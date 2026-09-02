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


app.MapGet("", async () =>
{
   
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<SecurityMasterDbContext>();
        return await db.Orders.Include(o=>o.Manager).
        Include(o=>o.Strategy).
        Include(o=>o.Security).
        Include(o=>o.Allocations).ToListAsync();
    }

}).WithName("GetTest");
app.Run();
