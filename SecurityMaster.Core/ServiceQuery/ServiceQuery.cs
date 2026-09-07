using System.Globalization;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

public class QueryFilter
{
    public string Field { get; set; } = "";
    public FilterOperator Operator { get; set; } = FilterOperator.Equals;
    public string Value { get; set; } = "";
}

public enum FilterOperator
{
    Equals,
    GreaterThan,
    GreaterThanOrEqual,
    LessThan,
    LessThanOrEqual,
    Contains
}

public class ServiceQuery<T>
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? Search { get; set; }
    public string? SortBy { get; set; }
    public bool SortDescending { get; set; } = false;
    public List<QueryFilter> Filters { get; set; } = new();
}

public static class ServiceQueryExtensions
{
    public static async Task<List<T>> ExecuteAsync<T>(
        this IQueryable<T> query,
        ServiceQuery<T> serviceQuery,
        CancellationToken ct = default)
    {
        if (serviceQuery.Page < 1) serviceQuery.Page = 1;
        if (serviceQuery.PageSize < 1) serviceQuery.PageSize = 20;

        return await query
            .ApplyFilters(serviceQuery.Filters)
            .ApplySort(serviceQuery.SortBy, serviceQuery.SortDescending)
            .Skip((serviceQuery.Page - 1) * serviceQuery.PageSize)
            .Take(serviceQuery.PageSize)
            .ToListAsync(ct);
    }

    public static IQueryable<T> ApplyFilters<T>(this IQueryable<T> query, List<QueryFilter> filters)
    {
        foreach (var filter in filters)
            query = query.ApplyFilter(filter);
        return query;
    }

    private static IQueryable<T> ApplyFilter<T>(this IQueryable<T> query, QueryFilter filter)
    {
        if (string.IsNullOrWhiteSpace(filter.Field))
            return query;

        var parameter = Expression.Parameter(typeof(T), "x");
        var property = BuildPropertyExpression(parameter, filter.Field);
        var propertyType = property.Type;
        var underlyingType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;

        Expression body;

        if (filter.Operator == FilterOperator.Contains)
        {
            Expression stringProperty = propertyType == typeof(string)
                ? property
                : Expression.Call(property, "ToString", null);

            body = Expression.Call(stringProperty, "Contains", null, Expression.Constant(filter.Value));
        }
        else
        {
            var convertedValue = ConvertValue(filter.Value, underlyingType);
            var constant = Expression.Constant(convertedValue, propertyType);

            body = filter.Operator switch
            {
                FilterOperator.Equals => Expression.Equal(property, constant),
                FilterOperator.GreaterThan => Expression.GreaterThan(property, constant),
                FilterOperator.GreaterThanOrEqual => Expression.GreaterThanOrEqual(property, constant),
                FilterOperator.LessThan => Expression.LessThan(property, constant),
                FilterOperator.LessThanOrEqual => Expression.LessThanOrEqual(property, constant),
                _ => throw new NotSupportedException($"Operator {filter.Operator}")
            };
        }

        var lambda = Expression.Lambda<Func<T, bool>>(body, parameter);
        return query.Where(lambda);
    }

    public static IQueryable<T> ApplySort<T>(this IQueryable<T> query, string? sortBy, bool descending)
    {
        if (string.IsNullOrWhiteSpace(sortBy))
            return query;

        var parameter = Expression.Parameter(typeof(T), "x");
        var property = BuildPropertyExpression(parameter, sortBy);
        var lambda = Expression.Lambda(property, parameter);

        var methodName = descending ? "OrderByDescending" : "OrderBy";
        var method = typeof(Queryable).GetMethods()
            .First(m => m.Name == methodName && m.GetParameters().Length == 2)
            .MakeGenericMethod(typeof(T), property.Type);

        return (IQueryable<T>)method.Invoke(null, new object[] { query, lambda })!;
    }

    private static Expression BuildPropertyExpression(Expression parameter, string path)
    {
        Expression expr = parameter;
        foreach (var part in path.Split('.'))
            expr = Expression.Property(expr, part);
        return expr;
    }

    private static object? ConvertValue(string value, Type targetType)
    {
        if (targetType == typeof(string)) return value;
        if (targetType == typeof(Guid)) return Guid.Parse(value);
        if (targetType == typeof(DateTime)) return DateTime.Parse(value, CultureInfo.InvariantCulture);
        if (targetType.IsEnum) return Enum.Parse(targetType, value, true);
        return Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
    }
}