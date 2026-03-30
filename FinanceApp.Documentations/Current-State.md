# FinanceApp – Current State

**Last updated:** 29 March 2026 — bump when solution layout or shipped capabilities change (see also root [README.md](../README.md)).

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
| Data | SQL Server, EF Core |
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

### Localization

- Shared strings: `FinanceApp.Localization/SharedResource*.resx`.
- Web: `IStringLocalizer<SharedResource>`, language switcher, user `PreferredLanguage`.
- API: culture from `Accept-Language` + profile.
- Mobile: i18next + `Accept-Language` on API calls.

---

## Where we are heading next

Production hardening, subscription enforcement polish, broader test coverage, and mobile store readiness — see [WHERE_WE_LEFT_OFF.md](../WHERE_WE_LEFT_OFF.md), [ROADMAP_KANBAN.md](../ROADMAP_KANBAN.md), and [DEPLOYMENT_READINESS.md](../DEPLOYMENT_READINESS.md).
