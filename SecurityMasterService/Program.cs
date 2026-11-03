using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SecurityMasterService.Data;
using SecurityMasterService.Hubs;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

//logging
var desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
var logPath = Path.Combine(desktopPath, "SecurityMasterService-log-.txt");
builder.Host.UseSerilog((context, config) =>
{
    config
        .MinimumLevel.Information()
        .WriteTo.Console()
        .WriteTo.File(logPath, rollingInterval: RollingInterval.Day);
});
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });


builder.Services.AddOpenApi();
var connectionString = builder.Configuration.GetConnectionString(Environment.MachineName);
builder.Services.AddDbContext<SecurityMasterDbContext>(x => x.UseSqlServer(connectionString));

builder.Services.AddSignalR();

// AUTH: JWT provera na svakom zahtevu
var jwt = builder.Configuration.GetSection("Jwt");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // šta se proverava na svakom zahtevu
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt["Issuer"],
            ValidateAudience = true,
            ValidAudience = jwt["Audience"],
            ValidateLifetime = true,                 // istekao token = 401
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]!)),
        };

        // SignalR: browser ne može da pošalje header uz WebSocket, pa token stiže u query stringu
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var token = context.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(token) && context.HttpContext.Request.Path.StartsWithSegments("/hubs"))
                    context.Token = token;
                return Task.CompletedTask;
            },
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

//TODO
if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.MapControllers();

app.MapHub<NotificationHub>("/hubs/notifications");
// get
//app.MapFilterableGet<SecurityMasterDbContext, Order>("/orders", "GetOrders", db => db.Orders
   // .AsNoTracking()
   // .Include(o => o.Manager)
   // .Include(o => o.Strategy)
   // .Include(o => o.Security)
   // .Include(o => o.Allocations));

//app.MapFilterableGet<SecurityMasterDbContext, Security>("/securities", "GetSecurities", db => db.Securities.AsNoTracking());
//app.MapFilterableGet<SecurityMasterDbContext, Allocation>("/allocations", "GetAllocations", db => db.Allocations.AsNoTracking());
//app.MapFilterableGet<SecurityMasterDbContext, Manager>("/managers", "GetManagers", db => db.Managers.AsNoTracking());
//app.MapFilterableGet<SecurityMasterDbContext, Strategy>("/strategies", "GetStrategies", db => db.Strategies.AsNoTracking());

// post
// app.MapPost("/orders", async (SecurityMasterDbContext db, [FromBody] CreateOrderRequest request) =>
// {
//     Manager manager = null!;
//     Strategy strategy = null!;
//     Security security = null!;

//     var error = await EntityHelper.RequireAsync(db.Managers, request.ManagerId, "Manager", m => manager = m)
//              ?? await EntityHelper.RequireAsync(db.Strategies, request.StrategyId, "Strategy", s => strategy = s)
//              ?? await EntityHelper.RequireAsync(db.Securities, request.Sid, "Security", s => security = s);
//     if (error != null) return error;

//     var order = new Order
//     {
//         Quantity = request.Quantity,
//         TradeDate = request.TradeDate,
//         ManagerId = request.ManagerId,
//         StrategyId = request.StrategyId,
//         Sid = request.Sid,
//         Manager = manager,
//         Strategy = strategy,
//         Security = security
//     };

//     return await EntityHelper.CreateAsync(db, db.Orders, order, o => o.OrderId, "/orders");
// });

//app.MapPost("/managers", async (SecurityMasterDbContext db, [FromBody] CreateManagerRequest request) =>
//    await EntityHelper.CreateAsync(db, db.Managers, new Manager { Display = request.Display }, m => m.Id, "/managers"));

//app.MapPost("/strategies", async (SecurityMasterDbContext db, [FromBody] CreateStrategyRequest request) =>
//    await EntityHelper.CreateAsync(db, db.Strategies, new Strategy { Display = request.Display }, s => s.Id, "/strategies"));

// app.MapPost("/securities", async (SecurityMasterDbContext db, [FromBody] CreateSecurityRequest request) =>
// {
//     var existing = await db.Securities.FindAsync(request.Sid);
//     if (existing != null) return Results.Conflict($"Security with SID {request.Sid} already exists");

//     var security = new Security { Sid = request.Sid, Description = request.Description };
//     return await EntityHelper.CreateAsync(db, db.Securities, security, s => s.Sid, "/securities");
// });

// app.MapPost("/allocations", async (SecurityMasterDbContext db, [FromBody] CreateAllocationRequest request) =>
// {
//     Manager manager = null!;
//     Strategy strategy = null!;
//     Order order = null!;

//     var error = await EntityHelper.RequireAsync(db.Managers, request.ManagerId, "Manager", m => manager = m)
//              ?? await EntityHelper.RequireAsync(db.Strategies, request.StrategyId, "Strategy", s => strategy = s)
//              ?? await EntityHelper.RequireAsync(db.Orders, request.OrderId, "Order", o => order = o);
//     if (error != null) return error;

//     var allocation = new Allocation
//     {
//         Quantity = request.Quantity,
//         ManagerId = request.ManagerId,
//         StrategyId = request.StrategyId,
//         OrderId = request.OrderId,
//         Manager = manager,
//         Strategy = strategy,
//         Order = order
//     };

//     return await EntityHelper.CreateAsync(db, db.Allocations, allocation, a => a.AllocationId, "/allocations");
// });

app.Run();

// http://localhost:1111/orders?filter=orderId=11||orderId=12
// http://localhost:1111/orders?filter=Security.SID=1
// http://localhost:1111/managers?sortBy=Display&sortDescending=true
// http://localhost:1111/orders?filter=Allocations.ManagerId=1&sortBy=TradeDate&sortDescending=true

// http://localhost:1111/orders/query
// {
//   "sortBy": "OrderId",
//   "sortDescending": false,
//   "filters": [
//     {
//       "field": "OrderId",
//       "operator": "Equals",
//       "value": "5"
//     }
//   ]
// }


//dotnet add "C:\SecurityMaster\SecurityMaster\SecurityMasterService\SecurityMasterService.csproj" package Microsoft.AspNetCore.Authentication.JwtBearer --version 9.*