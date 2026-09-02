using Microsoft.AspNetCore.DataProtection.KeyManagement;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}


app.MapGet("", () =>
{
   return new List<object>()
   { 
        new Manager()
        {
            Display = "Juni"
        },
        new Strategy()
        {
            Display = "Mica"
        }
   };
}).WithName("GetTest");
app.Run();
