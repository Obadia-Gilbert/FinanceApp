# Mobile app readiness – FinanceApp API

**Short answer:** The backend is **ready** for the in-repo mobile app: JWT auth, core finance flows, recurring, feedback, and baseline localization are exposed. Remaining work is mainly **production hardening** (secrets, CORS, file storage) — see [DEPLOYMENT_READINESS.md](./DEPLOYMENT_READINESS.md).

---

## What’s already in place (ready for mobile)

| Area | Status | Notes |
|------|--------|------|
| **Auth** | ✅ Ready | Register, login, JWT + refresh tokens, revoke. Mobile can use `POST /api/auth/register`, `login`, `refresh`, `revoke`. |
| **Dashboard** | ✅ Ready | `GET /api/dashboard` – totals, spend, budgets, notifications. |
| **Expenses** | ✅ Ready | CRUD, filters, receipt upload, Excel export. |
| **Income** | ✅ Ready | CRUD with optional account (sync to ledger). |
| **Categories** | ✅ Ready | List, create, update; expense/income/both types. |
| **Budgets** | ✅ Ready | Category budgets and overall budget. |
| **Accounts** | ✅ Ready | CRUD for user accounts. |
| **Transactions** | ✅ Ready | Ledger CRUD (income/expense, optional category). |
| **Profile** | ✅ Ready | Get/update profile (name, phone, country, etc.). |
| **Subscription** | ✅ Ready | Plan and upgrade endpoints. |
| **Supporting documents** | ✅ Ready | Upload, list, download, preview. |
| **Notifications** | ✅ Ready | List, unread count, mark read. |
| **Reports** | ✅ Ready | Monthly report (e.g. `GET /api/reports/monthly?year=&month=`). |
| **Recurring** | ✅ Ready | `RecurringTemplatesController` — `api/recurring` (JWT): list, get, create, update, deactivate, delete templates. |
| **Feedback** | ✅ Ready | `FeedbackController` — `api/feedback` (JWT): list “my” feedback, get by id, create. |
| **Localization** | ✅ Ready | Request culture from **`Accept-Language`** and the user’s preferred language (aligned with web profile). See root [README.md](./README.md) → Localization. |
| **Infrastructure** | ✅ Ready | JWT auth, CORS enabled, OpenAPI at `/openapi/v1.json`. |

So for a **first mobile version**, you can already build: login/register, dashboard, expenses, income, categories, budgets, accounts, transactions, recurring, feedback, profile, notifications, and reports — with localized API behavior where implemented.

---

## Before going to production (with or without mobile)

- **Secrets:** Move all secrets out of `appsettings.json` (see [DEPLOYMENT_READINESS.md](./DEPLOYMENT_READINESS.md)). Use env vars or a secret store for the API (e.g. `Jwt:Key`, DB connection, etc.).
- **CORS:** The API currently uses `AllowAnyOrigin()`. For production, restrict to your web and mobile origins (e.g. `WithOrigins("https://yourapp.com", "https://admin.yourapp.com")` and your mobile app’s scheme if needed).
- **File uploads:** Receipts/documents are under `wwwroot/uploads/`. For production, consider cloud storage (e.g. Azure Blob, S3) and serve via API or CDN so mobile and web use the same URLs.

---

## Summary

- **Ready for mobile now:** Auth, dashboard, expenses, income, categories, budgets, accounts, transactions, recurring, feedback, profile, subscription, documents, notifications, reports, and baseline **localization** (`Accept-Language` / preferred language). The in-repo **FinanceApp.Mobile** app targets this API (see **`FinanceApp.Mobile/README.md`**).
- **Before production:** Harden secrets, CORS, and upload storage as in [DEPLOYMENT_READINESS.md](./DEPLOYMENT_READINESS.md).
