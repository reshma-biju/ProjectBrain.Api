# Project Brain API: Task 3 – Project Skeleton, Configuration & Health Checks

**Author:** Reshma Biju
**Started:** 4 November 2026 · **Last updated:** 5 November 2026
**Status:** Completed (Task 3)
**Technology:** ASP.NET Core Web API (.NET 9), SQL Server, Docker, Visual Studio, MonsterASP.NET

## Summary

The API project now has a layered folder structure, secure configuration (user secrets locally, an environment variable on the host), an OpenAPI document with Swagger UI for development, and two health endpoints. `/health` checks that the API is running, and `/health/db` checks that the API can reach SQL Server and run a query. Both were tested locally in Docker, including two failure scenarios, and on MonsterASP.NET. The deployed API reaches the hosted database through the host's Local access connection, and it kept working after Remote access was switched off.

## Results at a glance

| Check | Result |
|---|---|
| Folder structure created | Done |
| Connection string kept out of Git (user secrets locally, environment variable on the host) | Done |
| OpenAPI document and Swagger UI (development only) | Done |
| `GET /health` locally and on the host | 200 OK |
| `GET /health/db` locally and on the host | 200 OK |
| Database stopped, `/health/db` locally | 503, with no details exposed |
| Wrong password, `/health/db` locally | 503, with no details exposed |
| Shorter connection timeout (`Connect Timeout=5`) locally and in production | Done |
| Production works with Remote access disabled | Confirmed |

## 1. Project structure

```text
ProjectBrain.Api
├── Controllers
├── Services
├── Repositories
├── Data
├── Entities
├── DTOs
├── Middleware
├── Properties
├── Program.cs
├── appsettings.json
└── ProjectBrain.Api.csproj
```

| Folder | Responsibility |
|---|---|
| Controllers | Receive HTTP requests and return responses. No business logic or SQL. |
| Services | Business rules. |
| Repositories | Data access. |
| Data | The database context and database configuration. |
| Entities | Classes that map to database tables. |
| DTOs | The request and response shapes used between the API and its clients. |
| Middleware | Custom components in the HTTP request pipeline. |

The request flow is Controller, then Service, then Repository. Separating the responsibilities keeps the application maintainable as it grows.

## 2. Configuration and secrets

### appsettings.json

The file holds only a placeholder. No credentials are stored in it.

```json
{
  "ConnectionStrings": {
    "Default": ""
  }
}
```

### User secrets (local development)

The project already had a `UserSecretsId` in its `.csproj`. The local connection string was stored with:

```bash
dotnet user-secrets set "ConnectionStrings:Default" "YOUR_CONNECTION_STRING"
dotnet user-secrets list
```

- User secrets are stored outside the project directory, and are linked to the project through its `UserSecretsId`.
- They are loaded automatically in the Development environment only.
- They are **not encrypted** by default. They are meant for local development, not for production.

### Configuration priority

With the default ASP.NET Core setup, configuration sources are applied from lowest to highest priority:

1. `appsettings.json`
2. `appsettings.{Environment}.json`
3. User secrets (Development only)
4. Environment variables
5. Command-line arguments

When the same key appears in more than one source, the higher source wins. The code reads the value the same way in every environment:

```csharp
_configuration.GetConnectionString("Default");   // reads ConnectionStrings:Default
```

### Connection timeout

Both the local User Secrets connection string and the production environment-variable connection string now include `Connect Timeout=5`.

- Connection timeout controls how long the application waits to **establish** a database connection before giving up. Here it is 5 seconds, so the health check fails fast when the database is unreachable.
- It does **not** control the total duration of a request or how long a SQL query may run.

### Environments

`launchSettings.json` sets `ASPNETCORE_ENVIRONMENT` to `Development` for local runs. It is for local development only and is not the production hosting configuration.

## 3. OpenAPI and Swagger UI

The project already used Microsoft's built-in OpenAPI support (the `Microsoft.AspNetCore.OpenApi` package, registered through `AddOpenApi()`). Only the Swagger UI component was added, not the full Swashbuckle package:

```bash
dotnet add package Swashbuckle.AspNetCore.SwaggerUI
```

```csharp
builder.Services.AddOpenApi();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "Project Brain API V1");
    });
}
```

`launchSettings.json` was updated so the browser opens Swagger on startup:

```json
"launchBrowser": true,
"launchUrl": "swagger"
```

Local addresses: `https://localhost:7056` and `http://localhost:5048`. These are two addresses for the **same** running application, not two instances.

Observations:
- The OpenAPI JSON is available at `/openapi/v1.json`. Swagger UI reads it and lets endpoints be tested in the browser.
- Both are restricted to the Development environment, so they do not appear on the public host.

## 4. SQL Server client

```bash
dotnet add package Microsoft.Data.SqlClient
```

`Microsoft.Data.SqlClient` lets the application connect to SQL Server directly: open connections, run queries, and read results. It was used for the database health check. EF Core, which sits on top of a similar client, comes in the next task.

## 5. Health endpoints

Implemented in `Controllers/HealthController.cs`.

### GET /health

- Purpose: checks that the API process is running.
- Does **not** touch the database.
- Response: `200 OK` with `{ "status": "ok" }`.

### GET /health/db

- Purpose: checks that the API can connect to SQL Server and run a query.
- Steps:
  1. Read the connection string through `IConfiguration`.
  2. Create a `SqlConnection` and open it asynchronously.
  3. Run `SELECT 1`.
  4. Return `200 OK` with `{ "status": "ok" }` if it succeeds.
  5. On failure, log the exception and return `503 Service Unavailable` with `{ "status": "unavailable" }`.
- Exception details are logged on the server and never returned to the caller.

`SELECT 1` returns the constant `1`. It does not read any table. It is a lightweight way to prove that SQL Server accepted the connection and executed a query.

### A. Cancellation handling

- ASP.NET Core automatically provides a `CancellationToken` associated with the current HTTP request.
- The token is passed to asynchronous database operations such as `OpenAsync()` and `ExecuteScalarAsync()`.
- If the request is cancelled (for example, the caller closes the connection), the database operation can observe the cancellation signal and stop unnecessary work.

### B. Connection disposal and pooling

- `await using` ensures that the SQL connection is disposed of when its scope ends, even if an error occurs.
- SQL connection pooling allows the underlying physical connection to be reused instead of establishing a completely new connection every time. Disposing a connection returns it to the pool rather than closing the physical connection.

## 6. Local testing (Docker SQL Server)

The local database runs in Docker. The connection string comes from user secrets, and the endpoints were tested through Swagger UI.

| Scenario | `/health` | `/health/db` | Notes |
|---|---|---|---|
| Normal | 200 | 200 | |
| SQL Server container stopped | 200 | 503 | The API stays up when its database is down. |
| Wrong password in User Secrets | 200 OK | 503 Service Unavailable | The response did not reveal credentials or exception details. The correct secret was restored. |

`/health` checks application availability independently of the database, while `/health/db` verifies database connectivity.

## 7. Template cleanup

- Deleted `WeatherForecastController.cs` and `WeatherForecast.cs`.
- Removed the old WeatherForecast request from the `.http` file, and added requests for `/health` and `/health/db`.
- The `.http` file is a manually maintained request file supported by Visual Studio. It lets requests run from the IDE, but it does not discover endpoints automatically. Swagger UI remains the main interface for exploring and testing the API.

## 8. Production setup on MonsterASP.NET

### Local access and Remote access

| Connection | Purpose |
|---|---|
| Local access | Used by the website hosted on MonsterASP.NET to reach its own database. |
| Remote access | Lets outside clients, such as SSMS on a laptop, connect to the database. |

Remote access was enabled earlier for the SSMS smoke test, and switched off again after the production check.

### Connection string

The host's Local access string has this shape. The real values, and the password, are kept private.

```text
Server=<host>;
Database=<database>;
User Id=<user>;
Password=<password>;
Encrypt=False;
MultipleActiveResultSets=True;
Connect Timeout=5;
```

`Encrypt=False` comes from the host's own Local access string. It is acceptable only for a connection between the website and its database inside the host's environment. Any connection that travels over the internet should be encrypted.

### Environment variable

User secrets are not loaded in Production, so the connection string was added as an environment variable in the website's configuration:

| Name | Value |
|---|---|
| `ConnectionStrings__Default` | the Local access connection string |

The double underscore stands for the colon in a hierarchical key, so `ConnectionStrings__Default` maps to `ConnectionStrings:Default`. Environment variables have higher priority than the appsettings files, so the same controller code works unchanged in both environments.

### Publishing

The Web Deploy publish profile downloaded from the panel was reused (it is kept out of Git). Settings: Release configuration, Target Runtime Portable, deployment method Web Deploy.

A Portable (framework-dependent) publish means the app relies on the .NET runtime installed on the host. Because the app started, the host has a .NET 9 runtime. The profile stores the deployment settings, so each publish updates the same site without re-entering them.

## 9. Production testing

Base address: `https://project-brain-api.runasp.net`

| Test | Result |
|---|---|
| `GET /health` | 200 OK, `{ "status": "ok" }` |
| `GET /health/db` | 200 OK, `{ "status": "ok" }`. The deployed API read the production connection string, connected to the hosted database, and ran `SELECT 1`. |
| Remote access disabled, then both endpoints tested again | Both still 200 OK. The hosted API does not depend on Remote access. |

## 10. What the health checks prove, and what they do not

- `/health/db` proves that the deployed app can **connect** to the hosted database and execute a query.
- It does **not** prove that the tables exist, because `SELECT 1` reads no table. The schema is created in Task 4.
- It does not measure performance under load.

## 11. Concepts learned

1. ASP.NET Core project structure and separation of responsibilities.
2. Configuration with `appsettings.json`, user secrets, and environment variables, and their priority.
3. Development and Production environments.
4. OpenAPI generation and Swagger UI.
5. Local HTTP and HTTPS addresses.
6. SQL Server connectivity with `Microsoft.Data.SqlClient`.
7. Dependency injection of `IConfiguration` and `ILogger`.
8. Asynchronous database calls with `OpenAsync()` and `ExecuteScalarAsync()`.
9. HTTP status codes 200 and 503, and the difference between liveness and readiness checks.
10. Failure handling that logs details and returns a generic message.
11. `.http` files for API testing.
12. Publish profiles and Web Deploy.
13. The difference between Local and Remote database access.
14. Request cancellation with `CancellationToken`.
15. Connection disposal with `await using`, connection pooling, and connection timeout.

## 12. Open items

1. **Public `/health/db`.** A public endpoint that opens a database connection could be abused. Keep it, protect it with a secret header, or remove it. Kept for a later production-hardening decision.
2. **Swagger on production.** No action needed now. Development-only Swagger is acceptable.
3. **Retarget to .NET 10.** To be completed before public deployment.
4. **Limited SQL login.** Create a limited SQL login for the API instead of using the host's database owner account. Kept for production hardening, subject to hosting-provider permissions.
5. **Database tables not created.** The hosted database has no tables yet. This is a note for Task 4, not a Task 3 blocker.

## 13. Conclusion

Task 3 established the backend foundation and verified connectivity in both environments. The API has a clear structure, keeps secrets out of Git, exposes health endpoints, and handles database failure without leaking details. The deployed API reaches the hosted SQL Server database through Local access, and kept working after Remote access was disabled.

**Status: completed. Next: Task 4, EF Core entities and the first migration.**
