# Where We Left Off

**Last updated:** 29 March 2026 — aligned with [README.md](./README.md) and [Current-State.md](./FinanceApp.Documentations/Current-State.md): backend + web + **Expo mobile** (`FinanceApp.Mobile`), notifications, monthly report + share, recurring job, **localization (en / es / sw)**, tests.

> **When to edit this file:** Bump the date above and adjust sections when the stack, ports, or priorities in README / Current-State change materially (not every small commit).

---

## Current state (what’s done)

- **Web app (FinanceApp.Web):** Landing page, auth (login/register, forgot password, external login), dashboard, expenses, income, categories, budgets, accounts, transactions, recurring (web flows), profile (with phone/country), supporting documents, **notifications** (bell + dropdown, mark read, full list at `/Notification/Index`), **monthly report** (`Report/Index`: month picker, totals, by category, top expenses, download as HTML, shareable link). Layout: fixed sidebar/navbar, landing at `/` for unauthenticated users. **Localization:** `FinanceApp.Localization` + `IStringLocalizer<SharedResource>` across major views; language switcher; culture from cookie / query / `Accept-Language` / user **PreferredLanguage**.
- **API (FinanceApp.API):** Feature parity with mobile-oriented clients: Auth (register/login/refresh/revoke), Expenses (CRUD, filter, receipt stream, Excel export), Categories, Budgets, Accounts, Transactions, Income, Profile, Subscription, Supporting Documents, **Notifications**, **Reports** (`GET /api/Reports/monthly?year=&month=`), Recurring (`/api/recurring`), Feedback, Dashboard. JWT auth; **OpenAPI** at `/openapi/v1.json`. **Testing:** SQLite when `EnvironmentName == "Testing"`; `appsettings.Testing.json` for JWT.
- **Mobile app (`FinanceApp.Mobile`):** React Native **Expo** app in-repo (not part of `FinanceApp.slnx`). Uses **FinanceApp.API** with JWT + refresh (SecureStore). Covers auth, dashboard, expenses, income, budget, accounts, transactions, categories, recurring, reports, notifications, subscription, feedback, profile, theme, and more. **i18next** + persisted locale; API calls send **`Accept-Language`**. See **`FinanceApp.Mobile/README.md`** for run instructions (`EXPO_PUBLIC_API_URL`, API **`Mobile`** launch profile on port **5279**).
- **Tests:**
  - **FinanceApp.Tests:** Unit tests (ExpenseService, CategoryService, localization resource smoke checks, …) — xUnit, Moq. Run `dotnet test` for current count.
  - **FinanceApp.API.Tests:** Integration tests (Auth + Expenses, …) — WebApplicationFactory, SQLite test DB. All passing.
- **Domain/Application/Infrastructure:** Category types (Expense/Income/Both), supporting documents, accounts/transactions/refresh tokens, income, recurring templates + **RecurringTransactionJob**, user country/country code. **Notifications** (Notification entity, `INotificationService`). **Monthly report** (`IMonthlyReportService`, `MonthlyReportResult`; **SharedReport** + `ISharedReportService`). SQLite-specific fixes in `FinanceDbContext` for test runs (IdentityPasskeyData keyless, `DateTimeOffset` conversion).

---

## What’s next (order of work)

1. **Polish and ship mobile v1**  
   - Core flows are implemented; treat remaining work as **QA, UX polish, store readiness**, and any gaps you still want in v1 (see mobile README).  
   - Keep API URL and **Mobile** API profile documented when testing on a device.

2. **Optional i18n follow-up:** Expand translated string coverage, add locales, or polish copy — baseline **en / es / sw** is in place ([README.md](./README.md) → Localization). See [LANGUAGE_SWITCHING_TODO.md](./FinanceApp.Documentations/LANGUAGE_SWITCHING_TODO.md) for status and optional tasks.

3. **Production readiness**  
   - Run the checklist in [DEPLOYMENT_READINESS.md](./DEPLOYMENT_READINESS.md) before first production deploy.  
   - Critical: secrets out of repo (User Secrets / env / vault), RoleSeeder admin from config, no hardcoded credentials.  
   - Production config (`AllowedHosts`, env-based settings), uploads strategy (e.g. blob storage), tighten **CORS** for API, optional Docker.

4. **Deploy**  
   - Push Web + API to production.  
   - Ship mobile to app stores when ready (can be after backend is live).

---

## Key docs to use when continuing

| Doc | Purpose |
|-----|--------|
| [README.md](./README.md) | Setup, ports, structure, features, API overview, mobile section |
| [FinanceApp.Mobile/README.md](./FinanceApp.Mobile/README.md) | Expo run, `.env`, LAN API, troubleshooting |
| [DEPLOYMENT_READINESS.md](./DEPLOYMENT_READINESS.md) | Checklist **before** first production deploy |
| [ROADMAP_KANBAN.md](./ROADMAP_KANBAN.md) | Backlog / This Week / Done |
| [ROADMAP_2_WEEKS.md](./ROADMAP_2_WEEKS.md) | Day-by-day plan (security, stability, warnings, features) |

---

## Quick resume commands

```bash
# Run all tests
dotnet test

# Web (see FinanceApp.Web/Properties/launchSettings.json for port, e.g. 5279)
dotnet run --project FinanceApp.Web

# API — default profile (e.g. http://localhost:5022)
dotnet run --project FinanceApp.API

# API — listen on 0.0.0.0:5279 for phone / Expo on same Wi‑Fi
dotnet run --project FinanceApp.API --launch-profile Mobile

# Mobile (from repo root)
cd FinanceApp.Mobile && npm install && npx expo start
```

---

## Summary for next session

- **Where we ended:** Full stack: Web + API + in-repo **Expo** mobile; notifications and monthly report (web + API); shared architecture documented in **README.md**. Migrations such as `AddNotifications`, `AddSharedReports` — run `dotnet ef database update` when applying on a new database.
- **Where to continue:** **Harden and finish mobile v1** (testing on device, any missing polish), then **DEPLOYMENT_READINESS.md** and production planning; optional i18n expansion and other Kanban items as prioritized.
