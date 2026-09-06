using System.Globalization;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

public class QueryFilter
{
    public string Field { get; set; } = "";      // npr. "ManagerId", "TradeDate"
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

public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}

public static class ServiceQueryExtensions
{
    public static async Task<PagedResult<T>> ExecuteAsync<T>(
        this IQueryable<T> query,
        ServiceQuery<T> serviceQuery,
        Type type,
        CancellationToken ct = default)
    {
        var searchableFields = type.GetProperties()
        .Where(p => p.PropertyType == typeof(string))
        .Select(p => p.Name)
        .ToArray();
        query = query
            .ApplyFilters(serviceQuery.Filters)
            .ApplySort(serviceQuery.SortBy, serviceQuery.SortDescending);
        return await query.ToPagedResultAsync(serviceQuery.Page, serviceQuery.PageSize, ct);
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
    var property = BuildPropertyExpression(parameter, filter.Field); // podržava i "Manager.Name"
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
        object? convertedValue = ConvertValue(filter.Value, underlyingType);
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

    public static async Task<PagedResult<T>> ToPagedResultAsync<T>(
        this IQueryable<T> query, int page, int pageSize, CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;

    var totalCount = await EntityFrameworkQueryableExtensions.CountAsync(query, ct);
    var items = await EntityFrameworkQueryableExtensions.ToListAsync(
    query.Skip((page - 1) * pageSize).Take(pageSize), ct);

        return new PagedResult<T>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
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