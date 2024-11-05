# Workshop with 12-factor
* Docker
* Go
* .NET C#

## 1. Codebase: One Codebase, Tracked in Revision Control
* Principle: Maintain a single codebase in version control
* Initialize a Git repository for the Dockerized Go project.

## 2. Dependencies: Explicitly Declare and Isolate Dependencies
* Principle: Explicitly declare dependencies in the Docker image and isolate them.
* Example: Use a `Dockerfile` to define the environment and dependencies.

```
# Dockerfile
FROM golang:1.23.2-alpine

WORKDIR /app
COPY . .

RUN go mod download
RUN go build -o app .

CMD ["./app"]
```

## 3. Config: Store Config in the Environment
* Principle: Store configuration in environment variables.
* Example: Define environment variables in the `docker-compose.yml` file or pass them during runtime.
  * Use `.env` file

```
# docker-compose.yml
services:
  app:
    build: .
    environment:
      - DATABASE_URL=${DATABASE_URL}
    ports:
      - "5000:5000"

```

File `main.go`
```
package main

import (
    "fmt"
    "os"
)

func main() {
    dbURL := os.Getenv("DATABASE_URL")
    fmt.Printf("Database URL: %s\n", dbURL)
}
```

## 4. Backing Services: Treat Backing Services as Attached Resources
* Principle: Treat services (e.g., databases) as external resources.
* Example: Define a database as a separate service in `docker-compose.yml`.

```
services:
  app:
    build: .
    environment:
      - DATABASE_URL=postgres://user:password@db:5432/mydb
    depends_on:
      - db
    ports:
      - "5000:5000"
  db:
    image: postgres:13
    environment:
      - POSTGRES_USER=user
      - POSTGRES_PASSWORD=password
      - POSTGRES_DB=mydb
    volumes:
      - db_data:/var/lib/postgresql/data

volumes:
  db_data:

```

## 5. Build, Release, Run: Strictly Separate Build and Run Stages
* Principle: Separate build and release stages.
* Example: Use multi-stage builds in `Dockerfile` to separate dependencies from the runtime image.

```
# Dockerfile
FROM golang:1.23.2-alpine AS build
WORKDIR /app
COPY . .
RUN go mod download
RUN go build -o app .

# Release stage
FROM alpine:latest
WORKDIR /root/
COPY --from=build /app/app .
CMD ["./app"]
```

## 6. Processes: Execute the App as One or More Stateless Processes
* Principle: Design the app as stateless.
* Example: Use Redis as a backing service in `docker-compose.yml` to maintain shared data.

```
services:
  app:
    build: .
    environment:
      - REDIS_URL=redis:6379
    depends_on:
      - redis
    ports:
      - "5000:5000"

  redis:
    image: redis:alpine
```

## 7. Port Binding: Export Services via Port Binding
* Principle: Expose the application on a specified port.
* Example: Bind the Go service to a port in `docker-compose.yml`.

```
services:
  app:
    build: .
    ports:
      - "5000:5000"
```

File `main.go`
```
package main

import (
    "fmt"
    "net/http"
)

func handler(w http.ResponseWriter, r *http.Request) {
    fmt.Fprintln(w, "Hello, world!")
}

func main() {
    http.HandleFunc("/", handler)
    http.ListenAndServe(":5000", nil)
}
```

## 8. Concurrency: Scale Out via the Process Model
* Principle: Scale by running multiple processes.
* Example: Use Docker Compose to scale the app.
  * Docker in standalone mode
  * Docker in swarm mode

```
# Scale the app to 3 instances
docker-compose up --scale app=3
```

## 9. Disposability: Maximize Robustness with Fast Startup and Graceful Shutdown
* Principle: Handle fast startup and graceful shutdown.
* Example: Use signal handling in Go for shutdown events.


File `main.go`
```
package main

import (
    "fmt"
    "os"
    "os/signal"
    "syscall"
)

func main() {
    sigs := make(chan os.Signal, 1)
    signal.Notify(sigs, syscall.SIGINT, syscall.SIGTERM)

    go func() {
        sig := <-sigs
        fmt.Println("Received signal:", sig)
        fmt.Println("Shutting down gracefully")
        os.Exit(0)
    }()

    // Main application logic
}
```

## 10. Dev/Prod Parity: Keep Development, Staging, and Production as Similar as Possible
* Principle: Make environments consistent.
* Example: Use Docker Compose to simulate production locally.

```
$docker-compose build
$docker-compose up
$docker-compose ps
```

## 11. Logs: Treat Logs as Event Streams
* Principle: Output logs as streams.
* Example: Print logs to stdout and stderr for centralized logging.

```
package main

import (
    "log"
)

func main() {
    log.Println("Starting application...")
    // Other logs can follow
}
```

Run docker compose
```
$docker-compose logs -f
```

TODO keep log in Centralized log system
* Loki
* ELK stack

## 12. Admin Processes: Run Admin/Management Tasks as One-Off Processes
* Principle: Run one-off admin tasks.
* Example: Run tasks like migrations as one-time commands.
  * Create tables
  * Insert data

```
# Docker
$docker-compose run app go run migrate.go

# Go
$go run migrate.go

# .NET with EF Core
$dotnet ef database update
```
