# Workshop with .NET 9

## Create project with webapi
```
$dotnet new webapi
$dotnet run
```
Access to APIs
* http://localhost:5250/weatherforecast
* http://localhost:5250/openapi/v1.json


## Add dependencies to project
* [Npgsql - .NET Access to PostgreSQL](https://www.npgsql.org/)

```
dotnet add package Npgsql
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

## Config database's connection string in project
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
