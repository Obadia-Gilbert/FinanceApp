# Where We Left Off

**Last updated:** After adding **Notifications** and **Monthly report (share)**. Use this file to resume work and keep roadmaps on track.

---

## Current state (what’s done)

- **Web app (FinanceApp.Web):** Landing page, auth (login/register, forgot password, external login), dashboard, expenses, categories, budgets, accounts, transactions, profile (with phone/country), supporting documents, **notifications** (bell + dropdown, mark read, full list at /Notification/Index), **monthly report** (Report/Index: month picker, totals, by category, top expenses, download as HTML, shareable link). Layout: fixed sidebar/navbar, landing at `/` for unauthenticated users.
- **API (FinanceApp.API):** Full feature parity with Web: Auth (register/login/refresh), Expenses (CRUD, filter by category, receipt stream, Excel export), Categories, Budgets, Accounts, Transactions, Profile, Subscription, Supporting Documents (upload/list/download/preview), **Notifications** (GET/POST list, unread count, mark read), **Reports** (GET /api/reports/monthly?year=&month=). JWT auth; OpenAPI at `/openapi/v1.json`. **Testing:** Uses SQLite when `EnvironmentName == "Testing"`; `appsettings.Testing.json` for JWT.
- **Tests:**
  - **FinanceApp.Tests:** 14 unit tests (ExpenseService, CategoryService) — xUnit, Moq.
  - **FinanceApp.API.Tests:** 7 integration tests (Auth + Expenses) — WebApplicationFactory, SQLite test DB. All passing.
- **Domain/Application/Infrastructure:** Category types (Expense/Income/Both), supporting documents, accounts/transactions/refresh tokens, user country/country code. **Notifications** (Domain: Notification entity, NotificationType enum; Application: INotificationService; created when dashboard loads and budget/category over). **Monthly report** (Application: IMonthlyReportService, MonthlyReportResult; **SharedReport** entity + ISharedReportService for shareable links). SQLite-specific fixes in `FinanceDbContext` for test runs (IdentityPasskeyData keyless, DateTimeOffset conversion).
- **Git:** `main` is up to date with all of the above. Branch `feature/tests` was merged into `main` and pushed.

---

## What’s next (order of work)

1. **Finish the mobile app** (before production)  
   - No mobile app exists in this repo yet.  
   - Decide: same repo (e.g. MAUI / React Native / Flutter) or separate repo.  
   - Use existing **FinanceApp.API** for auth and data.  
   - Scope to “finish”: login/register, expenses, categories, budgets, accounts/transactions, profile — then call it done for v1.

2. **Then: production readiness**  
   - Only after the mobile app is done, run the **deployment checklist** in [DEPLOYMENT_READINESS.md](./DEPLOYMENT_READINESS.md).  
   - Critical: secrets out of repo (User Secrets / env / vault), RoleSeeder admin from config, no hardcoded credentials.  
   - Then: production config (AllowedHosts, appsettings.Production or env), uploads strategy (e.g. blob storage), README/.NET version, optional Docker.

3. **Deploy**  
   - Push Web + API to production.  
   - Ship mobile to app stores when ready (can be after backend is live).

---

## Key docs to use when continuing

| Doc | Purpose |
|-----|--------|
| [DEPLOYMENT_READINESS.md](./DEPLOYMENT_READINESS.md) | Checklist and critical fixes **before** first production deploy. Run this **after** mobile app is finished. |
| [ROADMAP_KANBAN.md](./ROADMAP_KANBAN.md) | Backlog / This Week / Done. Update as you complete items. |
| [ROADMAP_2_WEEKS.md](./ROADMAP_2_WEEKS.md) | Day-by-day plan (security, stability, warnings, features). |
| [README.md](./README.md) | Setup, run, and project overview. |

---

## Quick resume commands

```bash
# Run all tests
dotnet test

# Web
cd FinanceApp.Web && dotnet run

# API (default http://localhost:5022)
cd FinanceApp.API && dotnet run
```

---

## Summary for next session

- **Where we ended:** Notifications (in-app bell, list, mark read, API endpoints) and monthly report (view by month, download HTML, shareable link; API GET /api/reports/monthly) are implemented. Two new migrations: `AddNotifications`, `AddSharedReports` — run `dotnet ef database update` when applying.
- **Where to continue:** Start or continue the **React Native app** (auth, expenses, categories, budgets, accounts/transactions, profile; use API notifications and reports). When mobile is done, run **DEPLOYMENT_READINESS.md** and then plan production deploy.
