using Microsoft.EntityFrameworkCore;
using api;

var builder = WebApplication.CreateBuilder(args);



// Add services to the container.
builder.Services.AddOpenApi();
builder.Services.AddControllers();

// Load configuration from environment variables
var appSettings = new AppSettings
{
    PostgreSqlConnection = builder.Configuration["POSTGRESQL_CONNECTION"] ?? "DefaultConnectionString"
};

// Bind configuration to strongly-typed objects
builder.Configuration.Bind(appSettings);
builder.Services.AddSingleton(appSettings);

// Add postgresql connection string to the configuration
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(appSettings.PostgreSqlConnection));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// GET /config
app.MapGet("/config", () => {
    return Results.Ok(appSettings);
})
.WithName("Config");

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};
app.MapGet("/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

app.MapControllers();

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

public class AppSettings
{
    public string PostgreSqlConnection { get; set; }
}
