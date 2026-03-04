# Mobile app readiness – FinanceApp API

**Short answer:** The backend is **mostly ready** for a mobile app. You have a solid API with JWT auth and most features exposed. A few API gaps and config tweaks will make the move to mobile smooth.

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
| **Infrastructure** | ✅ Ready | JWT auth, CORS enabled, OpenAPI at `/openapi/v1.json`. |

So for a **first mobile version**, you can already build: login/register, dashboard, expenses, income, categories, budgets, accounts, transactions, profile, notifications, and reports.

---

## Gaps to fix for full parity (optional but recommended)

1. **Recurring templates**
   - **Web:** Recurring templates (create, list, deactivate) exist in the Web app.
   - **API:** `IRecurringTemplateService` is registered but there is **no** `RecurringController` (no endpoints).
   - **For mobile:** Add e.g. `GET/POST /api/recurring` (and optionally PUT/DELETE) so the app can list and create recurring templates.

2. **Feedback**
   - **Web:** Users can submit questions/suggestions/comments; admins see them in the Admin area.
   - **API:** No feedback endpoints and **no** `IFeedbackService` registration in the API project.
   - **For mobile:** Add `POST /api/feedback` (and optionally `GET /api/feedback` for “my feedback”) and register `IFeedbackService` in the API so the mobile app can submit feedback.

3. **API project config**
   - **Feedback:** In `FinanceApp.API/Program.cs`, add:
     - `builder.Services.AddScoped<IFeedbackService, FeedbackService>();`
   - Then add a `FeedbackController` (e.g. `POST` create, `GET` list for current user) if you want feedback from the mobile app.

---

## Before going to production (with or without mobile)

- **Secrets:** Move all secrets out of `appsettings.json` (see [DEPLOYMENT_READINESS.md](./DEPLOYMENT_READINESS.md)). Use env vars or a secret store for the API (e.g. `Jwt:Key`, DB connection, etc.).
- **CORS:** The API currently uses `AllowAnyOrigin()`. For production, restrict to your web and mobile origins (e.g. `WithOrigins("https://yourapp.com", "https://admin.yourapp.com")` and your mobile app’s scheme if needed).
- **File uploads:** Receipts/documents are under `wwwroot/uploads/`. For production, consider cloud storage (e.g. Azure Blob, S3) and serve via API or CDN so mobile and web use the same URLs.

---

## Summary

- **Ready for mobile now:** Auth, dashboard, expenses, income, categories, budgets, accounts, transactions, profile, subscription, documents, notifications, reports. You can start building the mobile app against the existing API.
- **Quick wins for parity:** Add Recurring and Feedback API endpoints (and register `IFeedbackService` in the API) so the mobile app can do everything the web app can.
- **Before production:** Harden secrets, CORS, and upload storage as in [DEPLOYMENT_READINESS.md](./DEPLOYMENT_READINESS.md).
