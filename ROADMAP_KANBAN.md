# FinanceApp Roadmap Kanban

Use this board for daily execution tracking. Move items from **Backlog** to **This Week**, then to **Done**.

---

## Backlog

- [ ] Remove all secrets from tracked config and rotate exposed credentials.
- [ ] Resolve remaining nullable/runtime warnings in Web + Infrastructure.
- [ ] Add baseline tests for auth, budget alerts, and admin role operations.
- [ ] Refactor controller-level business rules into application services.
- [ ] Enforce subscription plan feature gating (Free vs Pro vs Premium).
- [ ] Add budget thresholds (80/90/100%) and better user guidance.
- [ ] Improve admin user management UX (search/filter/safe role actions).
- [ ] Add deployment health checks + startup diagnostics.
- [ ] Complete full responsive QA (mobile/tablet/desktop).

---

## This Week

### Security and Stability
- [ ] Remove secrets from `FinanceApp.Web/appsettings.json`.
- [ ] Keep placeholders only in `FinanceApp.Web/appsettings.Example.json`.
- [ ] Document secure setup in `README.md`.
- [ ] Validate clean dev startup (`dotnet run`, `dotnet watch run`) from fresh terminal.

### Auth Reliability and UI
- [ ] Validate Google external login flow end-to-end.
- [ ] Add Facebook/Twitter keys and validate callback flow.
- [ ] Ensure Identity page models consistently use `ApplicationUser`.
- [ ] Finalize login/register spacing and hierarchy for minimal scroll.

### Technical Debt
- [ ] Fix warnings in:
  - `FinanceApp.Web/Controllers/CategoryController.cs`
  - `FinanceApp.Web/Controllers/ExpenseController.cs`
  - `FinanceApp.Web/Areas/Identity/Pages/Account/Register.cshtml.cs`
  - `FinanceApp.Infrastructure/Services/EmailService.cs`

---

## Next Week

### Architecture
- [ ] Move budget/subscription decision logic fully into application services.
- [ ] Keep web controllers thin and transport-focused.
- [ ] Extract registration blocks from `Program.cs` into extension methods.

### Features
- [ ] Subscription capability limits in actual user workflows.
- [ ] Budget threshold alerts + UX messaging improvements.
- [ ] Admin table upgrades (search/filter/sort/audit-friendly actions).

### Release Readiness
- [ ] Add health endpoints and operational diagnostics.
- [ ] Run full regression suite and manual smoke checklist.
- [ ] Prepare release-candidate docs and deployment checklist.

---

## Done

- [x] Added Google/Facebook/Twitter provider wiring in `Program.cs`.
- [x] Added provider config placeholders to `appsettings.Example.json`.
- [x] Styled branded social provider buttons (light/dark mode).
- [x] Fixed `ExternalLogin` model to use `ApplicationUser`.
- [x] Fixed OAuth correlation issues for local development.
- [x] Improved login/register auth page layout iterations.
- [x] Added `ROADMAP_2_WEEKS.md` with detailed day-by-day plan.

---

## Daily Commands

```bash
dotnet build "/Users/obadiagilbert/PROJECTS/FinanceApp/FinanceApp.Web/FinanceApp.Web.csproj"
dotnet test "/Users/obadiagilbert/PROJECTS/FinanceApp/FinanceApp.slnx"
```

---

## Notes

- Never commit real secrets (`appsettings.json`, local uploads, credentials).
- Prefer User Secrets in development and env vars in production.
- Keep commits focused and small to simplify review and rollback.

