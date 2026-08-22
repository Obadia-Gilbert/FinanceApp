# CLAUDE.md

Guidance for working in this repository.

## What this is

A personal finance platform with **three client surfaces over one shared domain**: an ASP.NET Core MVC web app, a JWT REST API, and an Expo/React Native mobile app. Web and API both consume the same Application-layer services, so **business-logic changes usually need to land once in `FinanceApp.Application` — but presentation-layer logic is often duplicated between `FinanceApp.Web/Controllers` and `FinanceApp.API/Controllers`. Check both.**

## Layout

| Project | Role |
|---|---|
| `FinanceApp.Domain` | Entities, enums. No framework dependencies. Entity setters are `private set` — mutate via methods (`UpdateAmount(...)`). |
| `FinanceApp.Application` | Service interfaces + implementations, DTOs. Consumed by **both** Web and API. |
| `FinanceApp.Infrastructure` | EF Core, `FinanceDbContext`, Identity, repositories, email, subscription verifiers, hosted jobs. |
| `FinanceApp.Web` | MVC + Razor + cookie auth. Server-rendered. |
| `FinanceApp.API` | REST + JWT. Consumed by mobile. |
| `FinanceApp.Localization` | Shared `.resx` (`en`, `es`, `sw`) via `IStringLocalizer<SharedResource>`. |
| `FinanceApp.Tests` | Unit tests for Domain/Application (xUnit + Moq). |
| `FinanceApp.API.Tests` | Integration tests (`ApiWebApplicationFactory`, SQLite) **and** Infrastructure-layer unit tests. |
| `FinanceApp.Mobile` | Expo app. **Not in `FinanceApp.slnx`** — separate toolchain. |

## Running things

```bash
dotnet build FinanceApp.slnx
dotnet test  FinanceApp.slnx          # ~90 tests, all should pass

dotnet run --project FinanceApp.Web                          # http://localhost:5279
dotnet run --project FinanceApp.API                          # http://localhost:5022
dotnet run --project FinanceApp.API --launch-profile Mobile  # 0.0.0.0:5279 for LAN/devices
```

Mobile: `cd FinanceApp.Mobile && npx expo start`. Typecheck with `npx tsc --noEmit` — note there are 2 **pre-existing** TS errors (`sceneContainerStyle` in the tab layout, and a conditional style array in `notifications.tsx`) unrelated to most work; don't assume you broke something.

Database is SQL Server in Docker (container `sql_server`). `sqlcmd` is available on the host:

```bash
sqlcmd -S localhost,1433 -U sa -P '<password>' -C -Q "SELECT ... FROM FinanceAppDb.dbo.Expenses"
```

Real dev data lives in `FinanceAppDb`. Secrets are in **user secrets**, not appsettings — `dotnet user-secrets list --project FinanceApp.Web`.

## Sharp edges

**Razor views are compiled at startup.** Editing a `.cshtml` under plain `dotnet run` does nothing until you restart the process — no runtime compilation is configured. If a view change "isn't taking effect," restart before debugging anything else.

**Static assets need cache-busting.** Use `asp-append-version="true"` on `<img>`/`<link>`. Without it, a replaced image keeps serving the browser-cached old bytes under the same URL.

**Currency is multi-currency-aware everywhere. Never compare or sum raw amounts across currencies.**

A user can record expenses in one currency and set a budget in another. Use `ICurrencyConversionService`:

```csharp
// RIGHT — converts every currency bucket into the target
_currencyConversion.SumInCurrency(monthTotals, budgetCurrency);
_currencyConversion.SumCategoryInCurrency(categorySpend, categoryId, cb.Currency);

// WRONG — treats "no spend in exactly this currency" as zero, silently
monthTotals.GetValueOrDefault(budgetCurrency, 0);
```

This exact bug previously made budget alerts silently never fire, in ~10 duplicated places across the dashboard, notification service, and monthly report. If you add a new "is spend over budget" surface, route it through the shared helpers rather than writing the comparison again.

Related: when a budget's currency differs from the currency a report renders in, **convert the budget amount too** — don't print a TZS figure next to a "USD" label.

**Currency is stored as its ISO-4217 code (`"TZS"`), not the enum ordinal.** Enforced by `.HasConversion<string>()` in `FinanceDbContext` and `JsonStringEnumConverter` in `FinanceApp.API/Program.cs`. Enum member names *are* the ISO codes, so new currencies can be added anywhere in `Currency` without touching stored data. Don't reintroduce ordinal assumptions — no `(int)someCurrency` on the wire, and no index-based currency lists on the mobile side.

**Tests that deserialize API DTOs containing enums** need `new JsonSerializerOptions(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } }`. Omitting `JsonSerializerDefaults.Web` loses camelCase matching and every property silently deserializes to its default.

**Background jobs assume a single instance.** `RecurringTransactionJob` and `DailyActivityReminderJob` are `IHostedService`s with no distributed locking — scaling to 2+ instances duplicates recurring transactions. Flagged in `FinanceApp.Documentations/GOING_LIVE.md`.

## Database migrations

Migrations run against **real user data**. Before applying anything destructive:

1. `BACKUP DATABASE` first (`.db-backups/` is gitignored).
2. Restore into a scratch DB and apply there first.
3. Verify row counts and actual values, then test `Down()` too.

Scaffolded migrations are a starting point, not the answer. A generated `AlterColumn` int→string will cast `2` to `"2"`, not `"TZS"` — see `20260822114040_CurrencyAsIsoCode.cs` for the add-column / `CASE`-populate / drop / rename pattern that preserves meaning.

When reverting, target the **previous migration by name** (`dotnet ef database update <PreviousMigrationName>`). `update 0` reverts the entire history and will fail partway through on unrelated FK constraints.

## Conventions

- Services take dependencies via constructor injection; register in **both** `FinanceApp.Web/Program.cs` and `FinanceApp.API/Program.cs` when both surfaces need them. A service registered only in Web is invisible to mobile — this is how the API ended up with no currency conversion at all.
- Shared config (connection string, subscription billing) is in `Shared/appsettings.shared.json`, loaded first so user secrets and env vars override it.
- Repositories go through the generic `IRepository<T>`; aggregate/bounded queries live in `IExpenseQueryService` rather than being hand-rolled in controllers.
- Entities inherit `BaseEntity` (`Guid Id`, `CreatedAt`, `UpdatedAt`, `IsDeleted`). Soft delete is filtered in queries — respect `IsDeleted`.
