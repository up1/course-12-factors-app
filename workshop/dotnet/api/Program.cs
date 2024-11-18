using Microsoft.EntityFrameworkCore;
using api;
using OpenTelemetry.Resources;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using System.Diagnostics.Metrics;
using System.Diagnostics;
using Serilog;
using Serilog.Formatting.Compact;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(new CompactJsonFormatter())
    .CreateLogger();

builder.Services.AddSerilog(Log.Logger);


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

// Add background service
builder.Services.AddHostedService<SampleBackgroundService>();

// Custom metrics for the application
var helloMeter = new Meter("DemoWithDotNet", "1.0.0");
var countHello = helloMeter.CreateCounter<int>("hello.count", description: "Counts the number of greetings");

// Custom ActivitySource for the application
var greeterActivitySource = new ActivitySource("DemoWithDotNet");


// Add OpenTelemetry with Metrics and Tracing
var tracingOtlpEndpoint = builder.Configuration["OTLP_ENDPOINT_URL"];
var otel = builder.Services.AddOpenTelemetry();

// Configure OpenTelemetry Resources with the application name
otel.ConfigureResource(resource => resource
    .AddService(serviceName: builder.Environment.ApplicationName));

// Add Metrics for ASP.NET Core and our custom metrics and export to Prometheus
otel.WithMetrics(metrics => metrics
    .AddAspNetCoreInstrumentation()
    .AddMeter(helloMeter.Name)
    .AddMeter("Microsoft.AspNetCore.Hosting")
    .AddMeter("Microsoft.AspNetCore.Server.Kestrel")
    .AddPrometheusExporter());

// Add Tracing for ASP.NET Core and our custom ActivitySource and export to Jaeger
otel.WithTracing(tracing =>
{
    tracing.AddAspNetCoreInstrumentation();
    tracing.AddHttpClientInstrumentation();
    tracing.AddSource(greeterActivitySource.Name);
    if (tracingOtlpEndpoint != null)
    {
        tracing.AddOtlpExporter(otlpOptions =>
         {
             otlpOptions.Endpoint = new Uri(tracingOtlpEndpoint);
         });
    }
    else
    {
        tracing.AddConsoleExporter();
    }
});


// Add postgresql connection string to the configuration
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(appSettings.PostgreSqlConnection));

var app = builder.Build();

app.UseSerilogRequestLogging();

// Configure the Prometheus scraping endpoint
app.MapPrometheusScrapingEndpoint();

// Handle graceful shutdown
var lifetime = app.Lifetime;
lifetime.ApplicationStopping.Register(() =>
{
    Console.WriteLine("Application is stopping...");
    // Additional cleanup logic (e.g., release resources, close connections)
});

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

// Demo with metric
app.MapGet("/hi", (ILogger<Program> logger) =>
{
    // Create a new Activity scoped to the method
    using var activity = greeterActivitySource.StartActivity("HelloActivity");

    // Log a message
    logger.LogInformation("Hello message logged!");

    // Increment the custom counter
    countHello.Add(1);

    // Add a tag to the Activity
    activity?.SetTag("say", "Hello World!");

    return "Hello World!";
});

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

