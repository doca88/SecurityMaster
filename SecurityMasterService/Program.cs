using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.EntityFrameworkCore;
using SecurityMasterService.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddDbContext<SecurityMasterDbContext>(x=>x.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}


app.MapGet("", async (HttpContext http, SecurityMasterDbContext db) =>
{
    var q = http.Request.Query;

    var query = new ServiceQuery<Order>
    {
        Page = int.TryParse(q["page"], out var p) ? p : 1,
        PageSize = int.TryParse(q["pageSize"], out var ps) ? ps : 20,
        Search = q["search"],
        SortBy = q["sortBy"],
        SortDescending = bool.TryParse(q["sortDescending"], out var desc) && desc
    };

    // reserved parametri koji NE idu u Filters
    var reserved = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        { "page", "pageSize", "search", "sortBy", "sortDescending" };

    foreach (var key in q.Keys)
    {
        if (reserved.Contains(key)) continue;
        if (string.IsNullOrWhiteSpace(q[key])) continue;

        query.Filters.Add(new QueryFilter
        {
            Field = key,
            Operator = FilterOperator.Equals,
            Value = q[key]!
        });
    }

    var baseQuery = db.Orders
        .Include(o => o.Manager)
        .Include(o => o.Strategy)
        .Include(o => o.Security)
        .Include(o => o.Allocations)
        .AsQueryable();

    var result = await baseQuery.ExecuteAsync(
        query,
        searchableFields: new[] {"orderId" });

    return Results.Ok(result);
}).WithName("GetOrders");
app.Run();

//http://localhost:1111/?orderId=10