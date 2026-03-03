# FinanceApp.API.Tests

Integration tests for the FinanceApp API using `WebApplicationFactory`.

## Running tests

```bash
dotnet test FinanceApp.API.Tests/FinanceApp.API.Tests.csproj
```

## Current behavior

- **SQLite in Testing**: When the environment is `Testing`, the API uses EF Core **SQLite** with a unique file per factory instance (`Testing:SqlitePath`). SQLite enforces unique constraints so Identity works correctly; all auth and expense tests run without skipping.
- **JWT**: Test JWT settings come from `appsettings.Testing.json` (and/or factory in-memory config) so token generation and validation match.
- **Schema**: The API calls `EnsureCreatedAsync()` on the test database before seeding roles. The Infrastructure `FinanceDbContext` applies SQLite-specific fixes: `IdentityPasskeyData` is marked keyless, and `DateTimeOffset` properties use a string conversion so `ORDER BY` works.

## Configuration

- `ApiWebApplicationFactory` sets `UseEnvironment("Testing")`, `Jwt:Key` (if not in appsettings.Testing.json), and `Testing:SqlitePath` (unique temp file per factory).
- The API's `Program.cs` uses SQLite when `EnvironmentName` is `Testing` and reads `Testing:SqlitePath` from configuration. HTTPS redirection is disabled in Testing so the test client’s `Authorization` header is not stripped.
