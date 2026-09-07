
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace SecurityMaster.Core.Filtering;

public static class QueryHelper
{
    public static (ServiceQuery<T> query, IQueryable<T> filteredQuery, string? error) BuildServiceQuery<T>(
        IQueryCollection q, IQueryable<T> baseQuery, HashSet<string> allowedFilterFields)
    {
        var query = new ServiceQuery<T>
        {
            Page = int.TryParse(q["page"], out var p) && p > 0 ? p : 1,
            PageSize = int.TryParse(q["pageSize"], out var ps) ? Math.Clamp(ps, 1, 200) : 20,
            Search = q["search"].ToString() is { Length: > 0 } s ? s : null,
            SortBy = q["sortBy"].ToString() is { Length: > 0 } sb ? sb : null,
            SortDescending = bool.TryParse(q["sortDescending"], out var desc) && desc
        };

        var filterString = q["filter"].ToString();
        if (!string.IsNullOrWhiteSpace(filterString))
        {
            var parser = new FilterParser<T>(allowedFilterFields);
            try
            {
                var predicate = parser.Parse(filterString);
                baseQuery = baseQuery.Where(predicate);
            }
            catch (FormatException ex)
            {
                return (query, baseQuery, ex.Message);
            }
        }

        return (query, baseQuery, null);
    }

    public static async Task<IResult> HandleQuery<T>(
        IQueryCollection q, IQueryable<T> baseQuery, HashSet<string> allowedFilterFields)
    {
        var (query, filtered, error) = BuildServiceQuery(q, baseQuery, allowedFilterFields);
        if (error != null) return Results.BadRequest($"Invalid filter: {error}");

        var data = await filtered.ExecuteAsync(query);
        return Results.Ok(data);
    }
}

public class AllowedFields<T>
{
    public HashSet<string> Fields { get; }
    public AllowedFields(HashSet<string> fields) => Fields = fields;
}

public static class FilterableEndpointExtensions
{
    public static void MapFilterableGet<TContext, T>(
        this WebApplication app, string route, string endpointName,
        Func<TContext, IQueryable<T>> queryFactory)
        where TContext : DbContext
        where T : class
    {
        app.MapGet(route, async (HttpContext http, TContext db, AllowedFields<T> allowed) =>
        {
            var baseQuery = queryFactory(db);
            return await QueryHelper.HandleQuery(http.Request.Query, baseQuery, allowed.Fields);
        })
        .WithName(endpointName);
    }  
}
