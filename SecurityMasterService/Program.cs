using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecurityMaster.Core.Filtering;
using SecurityMasterService.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddDbContext<SecurityMasterDbContext>(x =>
    x.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddSingleton(new AllowedFields<Order>(FilterFieldResolver.GetAllowedFields(typeof(Order))));
builder.Services.AddSingleton(new AllowedFields<Security>(FilterFieldResolver.GetAllowedFields(typeof(Security))));
builder.Services.AddSingleton(new AllowedFields<Allocation>(FilterFieldResolver.GetAllowedFields(typeof(Allocation))));
builder.Services.AddSingleton(new AllowedFields<Manager>(FilterFieldResolver.GetAllowedFields(typeof(Manager))));
builder.Services.AddSingleton(new AllowedFields<Strategy>(FilterFieldResolver.GetAllowedFields(typeof(Strategy))));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// get
app.MapFilterableGet<SecurityMasterDbContext, Order>("/orders", "GetOrders", db => db.Orders
    .AsNoTracking()
    .Include(o => o.Manager)
    .Include(o => o.Strategy)
    .Include(o => o.Security)
    .Include(o => o.Allocations));

app.MapFilterableGet<SecurityMasterDbContext, Security>("/securities", "GetSecurities", db => db.Securities.AsNoTracking());
app.MapFilterableGet<SecurityMasterDbContext, Allocation>("/allocations", "GetAllocations", db => db.Allocations.AsNoTracking());
app.MapFilterableGet<SecurityMasterDbContext, Manager>("/managers", "GetManagers", db => db.Managers.AsNoTracking());
app.MapFilterableGet<SecurityMasterDbContext, Strategy>("/strategies", "GetStrategies", db => db.Strategies.AsNoTracking());

// post
app.MapPost("/orders", async (SecurityMasterDbContext db, [FromBody] CreateOrderRequest request) =>
{
    Manager manager = null!;
    Strategy strategy = null!;
    Security security = null!;

    var error = await EntityHelper.RequireAsync(db.Managers, request.ManagerId, "Manager", m => manager = m)
             ?? await EntityHelper.RequireAsync(db.Strategies, request.StrategyId, "Strategy", s => strategy = s)
             ?? await EntityHelper.RequireAsync(db.Securities, request.SecurityId, "Security", s => security = s);
    if (error != null) return error;

    var order = new Order
    {
        Quantity = request.Quantity,
        TradeDate = request.TradeDate,
        ManagerId = request.ManagerId,
        StrategyId = request.StrategyId,
        SID = request.SecurityId,
        Manager = manager,
        Strategy = strategy,
        Security = security
    };

    return await EntityHelper.CreateAsync(db, db.Orders, order, o => o.OrderId, "/orders");
});

app.MapPost("/managers", async (SecurityMasterDbContext db, [FromBody] CreateManagerRequest request) =>
    await EntityHelper.CreateAsync(db, db.Managers, new Manager { Display = request.Display }, m => m.Id, "/managers"));

app.MapPost("/strategies", async (SecurityMasterDbContext db, [FromBody] CreateStrategyRequest request) =>
    await EntityHelper.CreateAsync(db, db.Strategies, new Strategy { Display = request.Display }, s => s.Id, "/strategies"));

app.MapPost("/securities", async (SecurityMasterDbContext db, [FromBody] CreateSecurityRequest request) =>
{
    var existing = await db.Securities.FindAsync(request.Sid);
    if (existing != null) return Results.Conflict($"Security with SID {request.Sid} already exists");

    var security = new Security { Sid = request.Sid, Description = request.Description };
    return await EntityHelper.CreateAsync(db, db.Securities, security, s => s.Sid, "/securities");
});

app.MapPost("/allocations", async (SecurityMasterDbContext db, [FromBody] CreateAllocationRequest request) =>
{
    Manager manager = null!;
    Strategy strategy = null!;
    Order order = null!;

    var error = await EntityHelper.RequireAsync(db.Managers, request.ManagerId, "Manager", m => manager = m)
             ?? await EntityHelper.RequireAsync(db.Strategies, request.StrategyId, "Strategy", s => strategy = s)
             ?? await EntityHelper.RequireAsync(db.Orders, request.OrderId, "Order", o => order = o);
    if (error != null) return error;

    var allocation = new Allocation
    {
        Quantity = request.Quantity,
        ManagerId = request.ManagerId,
        StrategyId = request.StrategyId,
        OrderId = request.OrderId,
        Manager = manager,
        Strategy = strategy,
        Order = order
    };

    return await EntityHelper.CreateAsync(db, db.Allocations, allocation, a => a.AllocationId, "/allocations");
});

app.Run();

// http://localhost:1111/orders?filter=orderId=11||orderId=12
// http://localhost:1111/orders?filter=Security.SID=1