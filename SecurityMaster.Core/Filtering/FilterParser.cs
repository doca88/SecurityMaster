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

    var property = BuildPropertyChain(identToken.Text);

    var opToken = Current;
    _pos++;

    if (opToken.Type == TokenType.Contains)
    {
        var valueToken = Current; _pos++;
        var value = Expression.Constant(valueToken.Text);
        var containsMethod = typeof(string).GetMethod("Contains", new[] { typeof(string) })!;
        return Expression.Call(property, containsMethod, value);
    }

    var rawValueToken = Current; _pos++;
    var constant = ConvertValue(rawValueToken, property.Type);

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

private Expression BuildPropertyChain(string path)
{
    Expression current = _param;
    foreach (var part in path.Split('.'))
    {
        current = Expression.Property(current, part);
    }
    return current;
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