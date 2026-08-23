# FinanceApp – Current State

**Last updated:** 23 August 2026 — bump when solution layout or shipped capabilities change (see also root [README.md](../README.md)).

What is **implemented today**. For deeper architecture history and migration narrative, see [FinanceApp-Architecture.md](./FinanceApp-Architecture.md) (note: early sections of that doc describe evolution toward API/mobile; those targets are now in place — see root [README.md](../README.md) for the operational picture).

---

## Where We Are Now

### Solution structure

Primary .NET solution (`FinanceApp.slnx` or equivalent) includes:

| Project | Role |
|---------|------|
| `FinanceApp.Domain` | Entities, enums |
| `FinanceApp.Application` | Services, interfaces, DTOs |
| `FinanceApp.Infrastructure` | EF Core, Identity, repositories, hosted jobs |
| `FinanceApp.Web` | ASP.NET Core MVC + Razor + cookie auth |
| `FinanceApp.API` | REST API + JWT + OpenAPI |
| `FinanceApp.Localization` | Shared `SharedResource` `.resx` (en + es, sw) |
| `FinanceApp.Tests` | Unit tests (xUnit, Moq) |
| `FinanceApp.API.Tests` | API integration tests (WebApplicationFactory, SQLite) |

**In-repo but not always in the same solution file:** `FinanceApp.Mobile` (Expo / React Native) — see `FinanceApp.Mobile/README.md`.

### Tech stack (summary)

| Area | Technology |
|------|------------|
| Runtime | .NET 10 |
| Web | ASP.NET Core MVC, Razor, Bootstrap 5 |
| API | ASP.NET Core Web API, JWT, refresh tokens, OpenAPI `/openapi/v1.json` |
| Data | SQL Server, EF Core (currency stored as ISO-4217 code) |
| Auth (web) | Identity + cookies + external OAuth (Google, Facebook, Twitter) |
| Auth (API/mobile) | JWT + refresh |
| Mobile | Expo, React Native, Expo Router, TanStack Query |
| Localization | `FinanceApp.Localization` + request culture; mobile: i18next |

### Implemented capabilities (high level)

| Area | Status |
|------|--------|
| Expenses, income, categories, budgets (global + per-category), accounts, transactions (incl. transfers) | ✅ |
| Supporting documents (receipts, etc.), dashboard, monthly report + HTML download + share link | ✅ |
| Notifications (web + API + mobile surfaces) | ✅ |
| Recurring templates + `RecurringTransactionJob` | ✅ |
| Profile (incl. country / phone where configured), subscription UI + API | ✅ |
| Admin area (users / roles) | ✅ |
| Dark mode (web), theme context (mobile) | ✅ |
| **i18n** — Web + API + Mobile baseline (**en**, **es**, **sw**) | ✅ |
| Excel export (expenses) | ✅ |
| **Multi-currency** — cross-currency budget/report totals, ISO-4217 storage | ✅ (see below) |
| **Self-service account deletion** (Web + API + mobile, store-compliance) | ✅ (see below) |
| **Mobile first-run onboarding carousel** | ✅ |

### Multi-currency

Amounts can be recorded in any of the supported currencies (`FinanceApp.Domain/Enums/Currency.cs`), and a budget's currency does not have to match the currency its spend was recorded in.

- **Storage:** `Currency` persists as its **ISO-4217 code** (`"TZS"`), not the enum's ordinal — `.HasConversion<string>()` in `FinanceDbContext`, migration `20260822114040_CurrencyAsIsoCode`. Adding a currency anywhere in the enum no longer affects existing rows.
- **Wire format:** the API serializes enums as strings (`"currency":"TZS"`) via `JsonStringEnumConverter`.
- **Comparison:** all "spend vs. budget" logic converts through `ICurrencyConversionService` (`SumInCurrency` / `SumCategoryInCurrency`), which now resolves rates through `IExchangeRateStore`.
- **Live rates:** `ExchangeRateRefreshJob` (Infrastructure) fetches from the free, no-key `open.er-api.com` endpoint on a configurable interval (`ExchangeRates:Provider` in `Shared/appsettings.shared.json`, default every 8h) and pushes them into `ExchangeRateStore`. Rate resolution is three-tier: **live-fetched > configured override (`ExchangeRates:{CODE}`) > hardcoded default** — a failed or not-yet-run fetch never blocks a conversion, it just falls through to the next tier.

- **Mobile display precision:** `formatAmount()` (`FinanceApp.Mobile/src/utils/currency.ts`) renders zero-decimal currencies (JPY) without decimal places instead of always assuming 2, applied across every mobile screen that shows a currency-tagged amount.

**Not yet done:** per-transaction historical rate capture, a user-level base currency.

### Account deletion

Self-service account deletion — required by Apple Guideline 5.1.1(v) and Google Play policy before store submission, and previously missing entirely (only an admin-only user-removal action existed, which itself silently orphaned every owned row since no business table has a real DB foreign key to `AspNetUsers`).

- `IAccountDeletionService` (`FinanceApp.Application.Interfaces.Services` / implemented in `FinanceApp.Infrastructure.Services.AccountDeletionService`) deletes every row owned by a user across all business tables in FK-safe order, inside a single DB transaction, then the Identity user, then the user's uploaded files (documents + profile photo) from disk.
- Re-authentication required before deletion: current password (`UserManager.CheckPasswordAsync`) for local accounts; typed email confirmation for social-login-only accounts with no password set.
- Surfaced on all three clients: Web (`ProfileController.DeleteAccount`, danger-zone modal on the Edit Profile page), API (`DELETE /api/Profile`, `GET /api/Profile/deletion-status`), and mobile (Profile → Account Management → Delete Account).
- The admin panel's user-delete action (`UserService.DeleteUserAsync`) now routes through the same service, fixing the pre-existing orphaning bug there too.

### Mobile onboarding

First-run carousel (`FinanceApp.Mobile/app/onboarding.tsx`) shown once per install before `(auth)/login`, gated by an `AsyncStorage` flag (`src/utils/onboarding.ts`) — only interposes for signed-out users, never reappears for an existing session. Three slides (Track / Budget / Thrive) using the existing icon system and theme tokens; no new dependency (plain `ScrollView` + `pagingEnabled`, reusing the splash screen's dot-indicator styling).

### Localization

- Shared strings: `FinanceApp.Localization/SharedResource*.resx`.
- Web: `IStringLocalizer<SharedResource>`, language switcher, user `PreferredLanguage`.
- API: culture from `Accept-Language` + profile.
- Mobile: i18next + `Accept-Language` on API calls.

---

## Where we are heading next

Production hardening, subscription enforcement polish, broader test coverage, and mobile store readiness — see [WHERE_WE_LEFT_OFF.md](../WHERE_WE_LEFT_OFF.md), [ROADMAP_KANBAN.md](../ROADMAP_KANBAN.md), and [DEPLOYMENT_READINESS.md](../DEPLOYMENT_READINESS.md).
