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

## 5. Graceful Shutdown
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

