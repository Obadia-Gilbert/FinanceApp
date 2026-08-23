# Where We Left Off

**Last updated:** 23 August 2026 — aligned with [README.md](./README.md) and [Current-State.md](./FinanceApp.Documentations/Current-State.md): backend + web + **Expo mobile** (`FinanceApp.Mobile`), notifications, monthly report + share, recurring job, **localization (en / es / sw)**, **multi-currency correctness + ISO-4217 storage + live forex**, **self-service account deletion (Web + API + mobile, store-compliance requirement)**, **mobile first-run onboarding carousel**, tests.

> **When to edit this file:** Bump the date above and adjust sections when the stack, ports, or priorities in README / Current-State change materially (not every small commit).

---

## Current state (what’s done)

- **Web app (FinanceApp.Web):** Landing page, auth (login/register, forgot password, external login), dashboard, expenses, income, categories, budgets, accounts, transactions, recurring (web flows), profile (with phone/country), supporting documents, **notifications** (bell + dropdown, mark read, full list at `/Notification/Index`), **monthly report** (`Report/Index`: month picker, totals, by category, top expenses, download as HTML, shareable link). Layout: fixed sidebar/navbar, landing at `/` for unauthenticated users. **Localization:** `FinanceApp.Localization` + `IStringLocalizer<SharedResource>` across major views; language switcher; culture from cookie / query / `Accept-Language` / user **PreferredLanguage**.
- **API (FinanceApp.API):** Feature parity with mobile-oriented clients: Auth (register/login/refresh/revoke), Expenses (CRUD, filter, receipt stream, Excel export), Categories, Budgets, Accounts, Transactions, Income, Profile, Subscription, Supporting Documents, **Notifications**, **Reports** (`GET /api/Reports/monthly?year=&month=`), Recurring (`/api/recurring`), Feedback, Dashboard. JWT auth; **OpenAPI** at `/openapi/v1.json`. **Testing:** SQLite when `EnvironmentName == "Testing"`; `appsettings.Testing.json` for JWT.
- **Mobile app (`FinanceApp.Mobile`):** React Native **Expo** app in-repo (not part of `FinanceApp.slnx`). Uses **FinanceApp.API** with JWT + refresh (SecureStore). Covers auth, dashboard, expenses, income, budget, accounts, transactions, categories, recurring, reports, notifications, subscription, feedback, profile, theme, and more. **i18next** + persisted locale; API calls send **`Accept-Language`**. **First-run onboarding carousel** (`app/onboarding.tsx`, 3 slides, shown once via AsyncStorage flag, gates only signed-out users before `/(auth)/login`). See **`FinanceApp.Mobile/README.md`** for run instructions (`EXPO_PUBLIC_API_URL`, API **`Mobile`** launch profile on port **5279**).
- **Account deletion (store-compliance requirement, done):** Self-service account deletion, required by Apple Guideline 5.1.1(v) and Google Play policy before store submission. `IAccountDeletionService` (`FinanceApp.Application`/`FinanceApp.Infrastructure`) purges every owned row across all business tables in FK-safe order inside one DB transaction, then the Identity user, then uploaded files — see `AccountDeletionService.cs`. Re-auth required before deletion: current password for local accounts, typed email confirmation for social-login-only accounts. Wired into `ProfileController` on both Web (`POST /Profile/DeleteAccount`, danger-zone modal on Edit Profile) and API (`DELETE /api/Profile`, `GET /api/Profile/deletion-status`), and into the mobile Profile screen (Account Management → Delete Account). Also fixed a pre-existing bug where the admin panel's "delete user" action only called `UserManager.DeleteAsync` and silently orphaned every owned row — it now goes through the same service.
- **Tests:**
  - **FinanceApp.Tests:** Unit tests (ExpenseService, CategoryService, localization resource smoke checks, …) — xUnit, Moq. Run `dotnet test` for current count.
  - **FinanceApp.API.Tests:** Integration tests (Auth + Expenses, …) — WebApplicationFactory, SQLite test DB. All passing.
- **Domain/Application/Infrastructure:** Category types (Expense/Income/Both), supporting documents, accounts/transactions/refresh tokens, income, recurring templates + **RecurringTransactionJob**, user country/country code. **Notifications** (Notification entity, `INotificationService`). **Monthly report** (`IMonthlyReportService`, `MonthlyReportResult`; **SharedReport** + `ISharedReportService`). SQLite-specific fixes in `FinanceDbContext` for test runs (IdentityPasskeyData keyless, `DateTimeOffset` conversion).

---

## Multi-currency work (in progress)

**Phase 0 — correctness (done).** Budget alerts and report totals silently ignored spend recorded in a currency other than the budget's own (a `GetValueOrDefault(currency, 0)` lookup returning 0). Fixed in ~10 duplicated sites across `BudgetNotificationService`, the Web dashboard, the API dashboard, and `MonthlyReportService`, and consolidated behind `ICurrencyConversionService.SumInCurrency` / `SumCategoryInCurrency`. Also fixed: `Budget.UpdateAmount` discarding the currency on update (a budget's currency could never be changed), a category-totals query summing raw amounts across currencies, and budget figures on the monthly report being labelled with the wrong currency.

**Phase 1 — currency identity (done).** `Currency` now persists as its **ISO-4217 code** rather than the enum's ordinal int, so inserting a new currency into the enum can no longer silently reassign the meaning of existing rows. Migration `20260822114040_CurrencyAsIsoCode` (hand-written, `CASE`-mapped, reversible). API emits string enums; mobile no longer sends ordinal indices.

**Phase 2 — live forex (done).** `ExchangeRateRefreshJob` fetches from `open.er-api.com` (free, no key) on a configurable interval and feeds `ExchangeRateStore`, which resolves each currency live-fetched > configured override > hardcoded default so a network hiccup never blocks a conversion. Registered in both Web and API, each keeping its own in-memory store. See `FinanceApp.Documentations/Current-State.md` → Multi-currency for the resolution chain.

**Mobile display precision (done).** `formatAmount()` in `FinanceApp.Mobile/src/utils/currency.ts` now formats zero-decimal currencies (JPY) with no decimal places instead of always showing 2 — applied across all mobile screens that render a currency-tagged amount (expenses, income, budgets, accounts, transactions, recurring, categories, reports, dashboard). Also removed a dead ordinal-index code path (`getCurrencyIndex` and a numeric branch in `formatCurrencyCode`) that assumed currency was sent as an enum index — nothing called it, but it was exactly the pattern `CLAUDE.md` warns against reintroducing.

**Phase 3+ — not started.** User-level base currency (+ currency selection at signup), per-transaction historical rate capture.

---

## What’s next (order of work)

1. **Polish and ship mobile v1**  
   - Core flows are implemented; treat remaining work as **QA, UX polish, store readiness**, and any gaps you still want in v1 (see mobile README).  
   - Keep API URL and **Mobile** API profile documented when testing on a device.
   - Account deletion and onboarding were verified via direct API testing (real FK-safe purge confirmed against dev DB across all 18 tables) and `tsc`/`dotnet test` passing — the mobile Delete Account modal and onboarding carousel have **not** been click-tested end-to-end in the simulator (tooling coordinate issues blocked it this session). Do one manual pass before shipping.

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
