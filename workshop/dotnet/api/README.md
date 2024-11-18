# Workshop with .NET 9

## 1. Create project with webapi
```
$dotnet new webapi
$dotnet run
```
Access to APIs
* http://localhost:5250/weatherforecast
* http://localhost:5250/openapi/v1.json


## 2. Add dependencies to project
* [Npgsql - .NET Access to PostgreSQL](https://www.npgsql.org/)
* Use EFCore

```
$dotnet add package Npgsql
$dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL
$dotnet add package Microsoft.EntityFrameworkCore.Tools
```

Added dependency in file `api.csproj`
```
<Project Sdk="Microsoft.NET.Sdk.Web">

  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.OpenApi" Version="9.0.0" />
    <PackageReference Include="Npgsql" Version="8.0.5" />
  </ItemGroup>

</Project>
```

## 3. Config database's connection string in project
* Production => appsettings.json
* Development => appsettings.Development.json

```
{
  "ConnectionStrings": {
    "PostgreSql": "Host=localhost;Database=mydb;Username=myuser;Password=mypassword"
  }
}
```

Problem !!!

### Config in environment variable
```
$export POSTGRESQL_CONNECTION="Host=localhost;Database=mydb;Username=myuser;Password=mypassword"
```

### Read data from environment variable
```
var appSettings = new AppSettings
{
    PostgreSqlConnection = builder.Configuration["POSTGRESQL_CONNECTION"] ?? "DefaultConnectionString"
};
```

class AppSettings
```
public class AppSettings
{
    public required string PostgreSqlConnection { get; set; }
}
```

Run and test
```
$dotnet run
```
Testing
* http://localhost:5250/config

### Inject AppSettings to Services

File `Program.cs`
```
// Load configuration from environment variables
var appSettings = new AppSettings
{
    PostgreSqlConnection = builder.Configuration["POSTGRESQL_CONNECTION"] ?? "DefaultConnectionString"
};

// Bind configuration to strongly-typed objects
builder.Configuration.Bind(appSettings);
builder.Services.AddSingleton(appSettings);
```

File `ConfigController.cs`
```
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace api
{
    [Route("api/config")]
    [ApiController]
    public class ConfigController : ControllerBase
    {
        private readonly AppSettings _appSettings;

        public ConfigController(AppSettings appSettings)
        {
            _appSettings = appSettings;
        }

        [HttpGet("db-connection")]
        public IActionResult GetDbConnection()
        {
            return Ok(new { ConnectionString = _appSettings.PostgreSqlConnection });
        }
    }
}
```

Run and test
```
$dotnet run
```
Testing
* http://localhost:5250/api/config/db-connection

## 4. Add Backing Services to project
* Database => PostgreSQL as an attached resource
* This approach treats PostgreSQL as an external dependency that can be swapped without requiring changes to the application code
* Use EFCore


### Create file `AppDbContext.cs`
```
using System;
using Microsoft.EntityFrameworkCore;

namespace api;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Product> Products { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Seed initial data
        modelBuilder.Entity<Product>().HasData(
            new Product { Id = 1, Name = "Laptop", Price = 1200.00m },
            new Product { Id = 2, Name = "Smartphone", Price = 800.00m },
            new Product { Id = 3, Name = "Tablet", Price = 400.00m }
        );
    }
}

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; }
    public decimal Price { get; set; }
}
```

### Config Database in file `Program.cs`
```
using Microsoft.EntityFrameworkCore;

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(appSettings.PostgreSqlConnection));
```

### Create Product Controller
```
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace api
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly AppDbContext _context;

    public ProductsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetProducts()
    {
        var products = await _context.Products.ToListAsync();
        return Ok(products);
    }

    [HttpPost]
    public async Task<IActionResult> AddProduct([FromBody] Product product)
    {
        _context.Products.Add(product);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetProducts), new { id = product.Id }, product);
    }
    }
}
```

### Run and test

Start database
```
$docker run -d --name my_postgres -e POSTGRES_USER=myuser -e POSTGRES_PASSWORD=mypassword -e POSTGRES_DB=mydb -p 5432:5432 -d postgres
$docker ps
CONTAINER ID   IMAGE      COMMAND                  CREATED         STATUS         PORTS                    NAMES
4c5516078dbe   postgres   "docker-entrypoint.s…"   8 seconds ago   Up 7 seconds   0.0.0.0:5432->5432/tcp   my_postgres
```

Start server
```
$dotnet run
```
Testing
* http://localhost:5250/api/Products
  * Got error !!

Run database migration tool
```
$dotnet tool update --global dotnet-ef
$dotnet ef migrations add InitialCreate
$dotnet ef migrations add SeedInitialData

$dotnet ef database update
```

Run and Test again !!
* http://localhost:5250/api/Products
```
[
  {
    "id": 1,
    "name": "Laptop",
    "price": 1200.0
  },
  {
    "id": 2,
    "name": "Smartphone",
    "price": 800.0
  },
  {
    "id": 3,
    "name": "Tablet",
    "price": 400.0
  }
]
```

## 4. Build, release and run
Basic processes
```
$dotnet restore
$dotnet build
$dotnet run
```

### 4.1 Build with Docker

Create `Dockerfile`
```

```

Build image
```
$docker image build -t api:1.0 .
```

Create build script
```
#!/bin/bash

set -e  # Exit on error

echo "Building the Docker image..."
docker image build -t api:1.0 .
echo "Build completed!"
```

### 4.2 Release
* Tagging and push image to regidtry server
```
#!/bin/bash

set -e  # Exit on error

echo "Tagging the Docker image for release..."
IMAGE_NAME="api:release-$(date +%Y%m%d%H%M)"
docker tag api:latest $IMAGE_NAME
echo "Image tagged as $IMAGE_NAME"

echo "Pushing the image to Docker Hub (or private registry)..."
docker push $IMAGE_NAME
echo "Release completed!"
```

### 4.3 Run
* Create a container

Workign with Docker CLI
```
$docker container run --rm -d -p 8080:8080 --name myapp-api -e POSTGRESQL_CONNECTION="Host=host.docker.internal;Database=mydb;Username=myuser;Password=mypassword" api:1.0
```

Testing
* http://localhost:8080/api/Products


Working with Docker Compose = `docker-compose.yml`

## 5. Disposability principle in .NET
* Graceful shutdown with cancellation tokens
* Fast startup


### 5.1 Graceful shutdown v1
```
var app = builder.Build();

// Handle graceful shutdown
var lifetime = app.Lifetime;
lifetime.ApplicationStopping.Register(() =>
{
    Console.WriteLine("Application is stopping...");
    // Additional cleanup logic (e.g., release resources, close connections)
});
```

### 5.2 Graceful shutdown in background service

File `SampleBackgroundService.cs`
```
namespace api;

public class SampleBackgroundService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Console.WriteLine("Background service started.");

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                Console.WriteLine("Background service is running...");
                await Task.Delay(1000, stoppingToken); // Simulate work
            }
        }
        catch (TaskCanceledException)
        {
            Console.WriteLine("Background service canceled.");
        }
        finally
        {
            Console.WriteLine("Background service stopping.");
            // Cleanup logic
        }
    }
}
```

Resgister to app
```
// Add background service
builder.Services.AddHostedService<SampleBackgroundService>();
```

Run
```
$dotnet run
Background service is running...
Background service is running...
Background service is running...
Background service is running...
Background service is running...
Background service is running...
^Cinfo: Microsoft.Hosting.Lifetime[0]

=== Ctrl + C to stop server ===
      Application is shutting down...
Application is stopping...
Background service canceled.
Background service stopping.
```

### 5.4 Improve startup time of app/service
* Optimized dependency injection
  * Register services with scoped, singleton, or transient lifetimes based on their usage.
* Lazy Database Initialization

Register services with scoped 
```
builder.Services.AddScoped<IMyService, MyService>(); // Scoped for per-request lifecycle
builder.Services.AddSingleton<IConfigService, ConfigService>(); // Singleton for long-lived dependencies
```

## 6. Logging and Telemetry
* [.NET observability with OpenTelemetry](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/observability-with-otel)
  * Log
  * Metric
  * Trace
* [Example](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/observability-prgrja-example)

### 6.1 Add depedencies
```
<ItemGroup>
    <PackageReference Include="OpenTelemetry.Exporter.Console" Version="1.9.0" />
    <PackageReference Include="OpenTelemetry.Exporter.OpenTelemetryProtocol" Version="1.9.0" />
    <PackageReference Include="OpenTelemetry.Exporter.Prometheus.AspNetCore" Version="1.9.0-beta.2" />
    <PackageReference Include="OpenTelemetry.Exporter.Zipkin" Version="1.9.0" />
    <PackageReference Include="OpenTelemetry.Extensions.Hosting" Version="1.9.0" />
    <PackageReference Include="OpenTelemetry.Instrumentation.AspNetCore" Version="1.9.0" />
    <PackageReference Include="OpenTelemetry.Instrumentation.Http" Version="1.9.0" />
</ItemGroup>
```

### 6.2 Configure OpenTelemetry
```
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


// Configure the Prometheus scraping endpoint
app.MapPrometheusScrapingEndpoint();
```

### Run and test
```
$dotnet run
```

List of URLs
* Hello API => http://localhost:5250/hi
* Metric => http://localhost:5250/metrics
  * hello_count_total

* Log and trace in console
```
// Log information
info: Program[0]
      Hello message logged!

// Trace information
Activity.TraceId:            3ca85ad0ee263c02f25179695f1e9309
Activity.SpanId:             63ef4cc7fde9ab01
Activity.TraceFlags:         Recorded
Activity.ParentSpanId:       081f1edaa3f033db
Activity.ActivitySourceName: DemoWithDotNet
Activity.DisplayName:        HelloActivity
Activity.Kind:               Internal
Activity.StartTime:          2567-11-18T02:18:24.0968010Z
Activity.Duration:           00:00:00.0000530
Activity.Tags:
    say: Hello World!
Resource associated with Activity:
    service.name: api
    service.instance.id: a6e4d18f-8b97-4fdf-826e-67c6deaa8962
    telemetry.sdk.name: opentelemetry
    telemetry.sdk.language: dotnet
    telemetry.sdk.version: 1.9.0

Activity.TraceId:            3ca85ad0ee263c02f25179695f1e9309
Activity.SpanId:             081f1edaa3f033db
Activity.TraceFlags:         Recorded
Activity.ActivitySourceName: Microsoft.AspNetCore
Activity.DisplayName:        GET /hi
Activity.Kind:               Server
Activity.StartTime:          2567-11-18T02:18:24.0965410Z
Activity.Duration:           00:00:00.0005170
Activity.Tags:
    server.address: localhost
    server.port: 5250
    http.request.method: GET
    url.scheme: http
    url.path: /hi
    network.protocol.version: 1.1
    user_agent.original: Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/128.0.0.0 Safari/537.36
    http.route: /hi
    http.response.status_code: 200
Resource associated with Activity:
    service.name: api
    service.instance.id: a6e4d18f-8b97-4fdf-826e-67c6deaa8962
    telemetry.sdk.name: opentelemetry
    telemetry.sdk.language: dotnet
    telemetry.sdk.version: 1.9.0
```
