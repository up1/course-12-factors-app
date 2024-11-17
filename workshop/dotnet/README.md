# Workshop with .NET 9 and Docker compose
* API
* Database

## Working with Docker Compose = `docker-compose.yml`
```
services:
  api:
    image: api:1.0
    build:
      context: ./api
      dockerfile: Dockerfile
    ports:
      - "8080:8080"
    environment:
      - POSTGRESQL_CONNECTION=Host=db;Database=mydb;Username=myuser;Password=mypassword
    depends_on:
      db:
        condition: service_healthy
  
  db:
    image: postgres:13
    ports:
      - "5432:5432"
    volumes:
      - ./db/data.sql:/docker-entrypoint-initdb.d/data.sql
    environment:
      POSTGRES_DB: mydb
      POSTGRES_USER: myuser
      POSTGRES_PASSWORD: mypassword
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U myuser"]
      interval: 10s
      timeout: 5s
      retries: 5
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

