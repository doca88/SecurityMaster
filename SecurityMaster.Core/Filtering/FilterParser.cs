using System.Linq.Expressions;

namespace SecurityMaster.Core.Filtering;

public class FilterParser<T>
{
    private readonly HashSet<string> _allowedFields;
    private List<Token> _tokens = new();
    private int _pos;
    private readonly ParameterExpression _param = Expression.Parameter(typeof(T), "x");

    public FilterParser(IEnumerable<string> allowedFields)
    {
        _allowedFields = new HashSet<string>(allowedFields, StringComparer.OrdinalIgnoreCase);
    }

    public Expression<Func<T, bool>> Parse(string input)
    {
        _tokens = new FilterTokenizer(input).Tokenize();
        _pos = 0;
        var body = ParseOr();
        Expect(TokenType.End);
        return Expression.Lambda<Func<T, bool>>(body, _param);
    }

    private Token Current => _tokens[_pos];

    private Expression ParseOr()
    {
        var left = ParseAnd();
        while (Current.Type == TokenType.Or)
        {
            _pos++;
            var right = ParseAnd();
            left = Expression.OrElse(left, right);
        }
        return left;
    }

    private Expression ParseAnd()
    {
        var left = ParsePrimary();
        while (Current.Type == TokenType.And)
        {
            _pos++;
            var right = ParsePrimary();
            left = Expression.AndAlso(left, right);
        }
        return left;
    }

    private Expression ParsePrimary()
    {
        if (Current.Type == TokenType.LParen)
        {
            _pos++;
            var expr = ParseOr();
            Expect(TokenType.RParen);
            return expr;
        }
        return ParseComparison();
    }

    private Expression ParseComparison()
    {
        var identToken = Expect(TokenType.Identifier);

        if (!_allowedFields.Contains(identToken.Text))
            throw new FormatException($"Field '{identToken.Text}' is not filterable");

        var opToken = Current;
        _pos++;

        var valueToken = Current;
        _pos++;

        var parts = identToken.Text.Split('.');
        return BuildComparison(_param, parts, 0, opToken, valueToken);
    }

    private Expression BuildComparison(Expression instance, string[] parts, int index, Token opToken, Token valueToken)
    {
        var property = Expression.Property(instance, parts[index]);
        var elementType = GetCollectionElementType(property.Type);

        if (elementType != null)
        {
            var remaining = parts.Skip(index + 1).ToArray();
            if (remaining.Length == 0)
                throw new FormatException($"Cannot filter directly on collection field '{string.Join(".", parts)}'");

            var innerParam = Expression.Parameter(elementType, "p" + index);
            var innerBody = BuildComparison(innerParam, remaining, 0, opToken, valueToken);

            var anyMethod = typeof(Enumerable).GetMethods()
                .First(m => m.Name == nameof(Enumerable.Any) && m.GetParameters().Length == 2)
                .MakeGenericMethod(elementType);

            var lambda = Expression.Lambda(innerBody, innerParam);
            return Expression.Call(anyMethod, property, lambda);
        }

        if (index < parts.Length - 1)
        {
            return BuildComparison(property, parts, index + 1, opToken, valueToken);
        }

        return BuildFinalComparison(property, opToken, valueToken);
    }

    private Expression BuildFinalComparison(Expression property, Token opToken, Token valueToken)
    {
        if (opToken.Type == TokenType.Contains)
        {
            var value = Expression.Constant(valueToken.Text);
            var containsMethod = typeof(string).GetMethod("Contains", new[] { typeof(string) })!;
            return Expression.Call(property, containsMethod, value);
        }

        var constant = ConvertValue(valueToken, property.Type);

        return opToken.Type switch
        {
            TokenType.Equal => Expression.Equal(property, constant),
            TokenType.NotEqual => Expression.NotEqual(property, constant),
            TokenType.GreaterThan => Expression.GreaterThan(property, constant),
            TokenType.LessThan => Expression.LessThan(property, constant),
            TokenType.GreaterOrEqual => Expression.GreaterThanOrEqual(property, constant),
            TokenType.LessOrEqual => Expression.LessThanOrEqual(property, constant),
            _ => throw new FormatException($"Unexpected operator '{opToken.Text}'")
        };
    }

    private static Type? GetCollectionElementType(Type type)
    {
        if (type == typeof(string)) return null;

        if (type.IsGenericType && typeof(System.Collections.IEnumerable).IsAssignableFrom(type))
        {
            return type.GetGenericArguments().FirstOrDefault();
        }

        var ienum = type.GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));

        return ienum?.GetGenericArguments().FirstOrDefault();
    }

    private Expression ConvertValue(Token token, Type targetType)
    {
        object value = token.Type == TokenType.NumberValue
            ? Convert.ChangeType(token.Text, Nullable.GetUnderlyingType(targetType) ?? targetType)
            : token.Text;
        return Expression.Constant(value, targetType);
    }

    private Token Expect(TokenType type)
    {
        if (Current.Type != type)
            throw new FormatException($"Expected {type} but got {Current.Type} ('{Current.Text}')");
        var t = Current;
        _pos++;
        return t;
    }
}