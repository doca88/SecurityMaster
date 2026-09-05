using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace SecurityMaster.Core.Filtering;

public static class EntityHelper
{
    public static async Task<IResult?> RequireAsync<TEntity>(
        DbSet<TEntity> set, object key, string entityName, Action<TEntity> onFound)
        where TEntity : class
    {
        var entity = await set.FindAsync(key);
        if (entity == null) return Results.BadRequest($"{entityName} {key} not found");
        onFound(entity);
        return null;
    }

    public static async Task<IResult> CreateAsync<TEntity>(
        DbContext db, DbSet<TEntity> set, TEntity entity, Func<TEntity, object> idSelector, string route)
        where TEntity : class
    {
        set.Add(entity);
        await db.SaveChangesAsync();
        return Results.Created($"{route}/{idSelector(entity)}", entity);
    }
}