using System.Globalization;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

public class QueryFilter
{
    public string Field { get; set; } = "";
    public FilterOperator Operator { get; set; } = FilterOperator.Equals;
    public string Value { get; set; } = "";
}

public enum FilterOperator { Equals, GreaterThan, GreaterThanOrEqual, LessThan, LessThanOrEqual, Contains }

public class ServiceQuery<T>
{
    public string? SortBy { get; set; }
    public bool SortDescending { get; set; }
    public List<QueryFilter> Filters { get; set; } = new();
}

public static class ServiceQueryExtensions
{
    public static Task<List<T>> ExecuteAsync<T>(this IQueryable<T> query, ServiceQuery<T> q, CancellationToken ct = default)
    {
        return query.ApplyFilters(q.Filters).ApplySort(q.SortBy, q.SortDescending).ToListAsync(ct);
    }   

    public static IQueryable<T> ApplyFilters<T>(this IQueryable<T> query, List<QueryFilter> filters)
    {
        var parameter = Expression.Parameter(typeof(T), "x");
        Expression? body = null;

        foreach (var f in filters.Where(f => !string.IsNullOrWhiteSpace(f.Field)))
        {
            var condition = BuildCondition(parameter, f);
            body = body == null ? condition : Expression.AndAlso(body, condition);
        }
        return body == null ? query : query.Where(Expression.Lambda<Func<T, bool>>(body, parameter));
    }

    private static Expression BuildCondition(ParameterExpression parameter, QueryFilter filter)
    {
        var property = filter.Field.Split('.').Aggregate((Expression)parameter, Expression.Property);
        var type = property.Type;
        var underlying = Nullable.GetUnderlyingType(type) ?? type;

        if (filter.Operator == FilterOperator.Contains)
        {
            var asString = type == typeof(string) ? property : Expression.Call(property, "ToString", null);
            return Expression.Call(asString, "Contains", null, Expression.Constant(filter.Value));
        }

        var value = Expression.Constant(ConvertValue(filter.Value, underlying), type);

        return filter.Operator switch
        {
            FilterOperator.Equals => Expression.Equal(property, value),
            FilterOperator.GreaterThan => Expression.GreaterThan(property, value),
            FilterOperator.GreaterThanOrEqual => Expression.GreaterThanOrEqual(property, value),
            FilterOperator.LessThan => Expression.LessThan(property, value),
            FilterOperator.LessThanOrEqual => Expression.LessThanOrEqual(property, value),
            _ => throw new NotSupportedException($"Operator {filter.Operator}")
        };
    }

    public static IQueryable<T> ApplySort<T>(this IQueryable<T> query, string? sortBy, bool descending)
    {
        if (string.IsNullOrWhiteSpace(sortBy)) return query;

        var parameter = Expression.Parameter(typeof(T), "x");
        var property = sortBy.Split('.').Aggregate((Expression)parameter, Expression.Property);
        var lambda = Expression.Lambda(property, parameter);

        var method = typeof(Queryable)
            .GetMethods()
            .First(m => m.Name == (descending ? "OrderByDescending" : "OrderBy") && m.GetParameters().Length == 2)
            .MakeGenericMethod(typeof(T), property.Type);

        return (IQueryable<T>)method.Invoke(null, new object[] { query, lambda })!;
    }

    private static object? ConvertValue(string value, Type type) =>
        type == typeof(string) ? value :
        type == typeof(Guid) ? Guid.Parse(value) :
        type == typeof(DateTime) ? DateTime.Parse(value, CultureInfo.InvariantCulture) :
        type.IsEnum ? Enum.Parse(type, value, true) :
        Convert.ChangeType(value, type, CultureInfo.InvariantCulture);
}