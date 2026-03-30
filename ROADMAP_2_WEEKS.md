# FinanceApp 2-Week Practical Roadmap

Goal: improve production safety, architecture boundaries, UI consistency, and feature depth with clear day-by-day execution.

**Handoff:** Start from [WHERE_WE_LEFT_OFF.md](./WHERE_WE_LEFT_OFF.md) (ordered next steps). The **Expo** app is in-repo; this roadmap covers **security/stability** work and **production** prep alongside mobile polish. Daily execution: [ROADMAP_KANBAN.md](./ROADMAP_KANBAN.md).

---

## Week 1 - Stabilize and Secure

### Day 1 - Security and Configuration Hygiene
- [ ] Remove real credentials from tracked config files.
- [ ] Keep only placeholders in committed config.
- [ ] Rotate exposed SMTP/OAuth secrets and reconfigure local secrets.

**Files in scope**
- `FinanceApp.Web/appsettings.json`
- `FinanceApp.Web/appsettings.Example.json`
- `README.md`

**Commands**
```bash
dotnet user-secrets --project FinanceApp.Web/FinanceApp.Web.csproj list
```

**Done when**
- No secrets in tracked config.
- Local app still runs via User Secrets/env vars.

---

### Day 2 - Build/Runtime Stability
- [ ] Eliminate recurring corrupted build artifact scenarios.
- [ ] Document clean-recovery workflow in README.
- [ ] Verify `dotnet run` and `dotnet watch run` are both stable.

**Files in scope**
- `README.md`
- Optional: `scripts/dev-clean.sh`

**Commands**
```bash
dotnet clean FinanceApp.slnx
rm -rf FinanceApp.Web/bin FinanceApp.Web/obj
```

**Done when**
- Clean run works from a fresh terminal without manual hacks.

---

### Day 3 - Warning Debt (Priority)
- [ ] Resolve top nullable/runtime warnings in web and infra.
- [ ] Focus on warnings that can cause runtime errors first.

**Files in scope (likely)**
- `FinanceApp.Web/Controllers/CategoryController.cs`
- `FinanceApp.Web/Controllers/ExpenseController.cs`
- `FinanceApp.Web/Views/Shared/_Sidebar.cshtml`
- `FinanceApp.Web/Views/Expense/_ExpenseEditPartial.cshtml`
- `FinanceApp.Web/Areas/Identity/Pages/Account/Register.cshtml.cs`
- `FinanceApp.Infrastructure/Services/EmailService.cs`

**Commands**
```bash
dotnet build FinanceApp.Web/FinanceApp.Web.csproj
```

**Done when**
- Warnings in touched files are resolved or intentionally documented.

---

### Day 4 - Auth Flow Hardening (Google/Facebook/Twitter)
- [ ] Validate provider callback reliability.
- [ ] Confirm Identity pages consistently use `ApplicationUser`.
- [ ] Verify first-time external account creation flow.

**Files in scope**
- `FinanceApp.Web/Program.cs`
- `FinanceApp.Web/Areas/Identity/Pages/Account/ExternalLogin.cshtml.cs`
- `FinanceApp.Web/Areas/Identity/Pages/Account/Login.cshtml`
- `FinanceApp.Web/Areas/Identity/Pages/Account/Register.cshtml`

**Done when**
- Provider login works reliably (no correlation/DI mismatch).

---

### Day 5 - Testing Baseline
- [ ] Add/expand tests for critical flows.
- [ ] Start with service-level tests for budget/subscription/roles.
- [ ] Add at least one auth integration smoke test.

**Files in scope**
- `tests/FinanceApp.Application.Tests/*`
- `tests/FinanceApp.Web.Tests/*`
- Related service/controller files for testability seams

**Commands**
```bash
dotnet test FinanceApp.slnx
```

**Done when**
- Test command runs green for baseline critical flows.

---

## Week 2 - Architecture and UX Maturity

### Day 6 - Auth UI Final Pass
- [ ] Finalize login/register visual hierarchy.
- [ ] Reduce unnecessary vertical scroll on laptop viewports.
- [ ] Keep mobile behavior clean and readable.

**Files in scope**
- `FinanceApp.Web/Areas/Identity/Pages/Account/Login.cshtml`
- `FinanceApp.Web/Areas/Identity/Pages/Account/Register.cshtml`
- `FinanceApp.Web/wwwroot/css/site.css`
- `FinanceApp.Web/wwwroot/css/dark-mode.css`

**Done when**
- Login/Register are consistent and pass responsive checks.

---

### Day 7 - Shared UI Tokens and Consistency
- [ ] Introduce reusable spacing/typography/color tokens.
- [ ] Normalize card/table/form/button style system.

**Files in scope**
- `FinanceApp.Web/wwwroot/css/site.css`
- `FinanceApp.Web/wwwroot/css/dark-mode.css`
- `FinanceApp.Web/Views/Home/Index.cshtml`
- `FinanceApp.Web/Views/Expense/Index.cshtml`
- `FinanceApp.Web/Views/Category/Index.cshtml`
- `FinanceApp.Web/Areas/Admin/Views/Admin/Users.cshtml`

**Done when**
- Cross-page UI rhythm looks consistent.

---

### Day 8 - Architecture Cleanup (Web -> Application)
- [ ] Move remaining business rules out of controllers into services.
- [ ] Keep controllers as orchestration and transport logic.

**Files in scope (likely)**
- `FinanceApp.Web/Controllers/HomeController.cs`
- `FinanceApp.Web/Controllers/BudgetController.cs`
- `FinanceApp.Web/Controllers/SubscriptionController.cs`
- `FinanceApp.Application/Services/BudgetService.cs`
- `FinanceApp.Application/*` service interfaces

**Done when**
- Business rules are centralized in application/domain services.

---

### Day 9 - Subscription Feature Value
- [ ] Implement plan-based feature gating (not only plan labels).
- [ ] Add visible UX messaging for gated actions.

**Files in scope**
- `FinanceApp.Domain/Enums/SubscriptionPlan.cs`
- `FinanceApp.Infrastructure/Identity/ApplicationUser.cs`
- `FinanceApp.Web/Controllers/SubscriptionController.cs`
- Affected feature controllers/services/views

**Done when**
- Free/Pro/Premium meaningfully changes behavior.

---

### Day 10 - Budget Intelligence V2
- [ ] Add 80/90/100% threshold states.
- [ ] Improve dashboard budget messaging.
- [ ] Optional: add notification preference toggle.

**Files in scope**
- `FinanceApp.Application/Services/BudgetService.cs`
- `FinanceApp.Web/Controllers/HomeController.cs`
- `FinanceApp.Web/Views/Home/Index.cshtml`
- `FinanceApp.Web/Views/Budget/Index.cshtml`

**Done when**
- Users get proactive budget guidance, not just limit breach alerts.

---

### Day 11 - Admin UX and Safety
- [ ] Add user search/filter/sort improvements.
- [ ] Improve role action safety (self-demotion/deletion guardrails).
- [ ] Add clearer success/error feedback.

**Files in scope**
- `FinanceApp.Web/Areas/Admin/AdminController.cs`
- `FinanceApp.Web/Areas/Admin/Views/Admin/Users.cshtml`
- `FinanceApp.Infrastructure/Services/UserService.cs`

**Done when**
- Admin operations are safer and easier.

---

### Day 12 - Deployment Readiness Implementation
- [ ] Add health checks and runtime diagnostics.
- [ ] Explicit production security settings validation.
- [ ] Verify migration/deployment runbook.

**Files in scope**
- `FinanceApp.Web/Program.cs`
- `README.md`
- `DEPLOYMENT_READINESS.md`

**Done when**
- Clear, repeatable deployment checklist exists and works.

---

### Day 13 - Regression + Responsive QA Sweep
- [ ] Full smoke test of critical journeys:
  - register/login/logout/external auth
  - expense/category CRUD
  - budget create/alerts
  - subscription upgrades
  - admin role operations
- [ ] Mobile/tablet/desktop responsive pass.

**Files in scope**
- Fix only what fails during QA.

**Done when**
- No blocking regressions.

---

### Day 14 - Release Prep
- [ ] Code cleanup and dead code removal.
- [ ] Update docs/screenshots.
- [ ] Create release candidate tag.

**Files in scope**
- `README.md`
- Updated views/css/controllers from final QA fixes
- Optional: `UI_IMPROVEMENT_ANALYSIS.md`

**Done when**
- Branch is deployment-ready.

---

## Daily Execution Checklist (Use Every Day)

- [ ] Run build:
  ```bash
  dotnet build FinanceApp.Web/FinanceApp.Web.csproj
  ```
- [ ] Run tests (from Day 5 onward):
  ```bash
  dotnet test FinanceApp.slnx
  ```
- [ ] Manually test changed flows in browser.
- [ ] Verify `git status` does not include secrets/local files.
- [ ] Commit small and focused changes with clear messages.

---

## Suggested Commit Batching

1. `chore(config): remove secrets and document secure setup`
2. `chore(stability): clean build/watch reliability and docs`
3. `fix(auth): harden external login and identity consistency`
4. `style(auth): finalize login/register responsive UI`
5. `refactor(app): move business rules into services`
6. `feat(subscription): enforce plan-based capabilities`
7. `feat(budget): add threshold alerts and improved guidance`
8. `feat(admin): improve user management safety and usability`
9. `chore(release): docs, QA fixes, deployment checklist`

