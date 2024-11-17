# Workshop with .NET 9 and Docker compose
* API
* Database

## Working with Docker Compose = `docker-compose.yml`
```
```

Build and Run
```
$docker compose build
$docker compose up -d

$docker compose ps
NAME           IMAGE         COMMAND                  SERVICE   CREATED          STATUS                    PORTS
dotnet-api-1   api:1.0       "dotnet api.dll"         api       30 seconds ago   Up 19 seconds             0.0.0.0:8080->8080/tcp
dotnet-db-1    postgres:13   "docker-entrypoint.s…"   db        30 seconds ago   Up 29 seconds (healthy)   5432/tcp
```

Testing
* http://localhost:8080/api/Products

