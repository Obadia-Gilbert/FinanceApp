# FinanceApp – Deployment Readiness

This document summarizes the current state of the project against the README and gives concrete steps to make it safe and ready for deployment.

---

## Critical (must fix before deploy)

### 1. Secrets in source control

**Issue:** `FinanceApp.Web/appsettings.json` currently contains:

- **EmailSettings.Password** (e.g. `90Barclaysnew!`)
- **EmailSettings.Username**, **SenderEmail**, **SmtpServer**, **Port**
- **ConnectionStrings** (and possibly credentials)

These must **not** be in the repository. If this file has ever been committed with real credentials, treat those credentials as compromised and rotate them (email password, DB password, etc.).

**What to do:**

- **Development:** Use [User Secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets):
  - Run: `dotnet user-secrets init --project FinanceApp.Web`
  - Store connection string and EmailSettings there, e.g.:
    - `dotnet user-secrets set "ConnectionStrings:DefaultConnection" "YourDevConnectionString" --project FinanceApp.Web`
    - `dotnet user-secrets set "EmailSettings:Password" "YourEmailPassword" --project FinanceApp.Web`
  - Remove all secrets from `appsettings.json` and `appsettings.Development.json` (or leave only non-sensitive defaults).
- **Production:** Use environment variables or a secret store (e.g. Azure Key Vault, AWS Secrets Manager). Never put production passwords in appsettings checked into git.
- Add a **template** file (e.g. `appsettings.Example.json`) that shows required keys with empty or placeholder values so others know what to configure.

### 2. Hardcoded admin credentials in RoleSeeder

**Issue:** `FinanceApp.Infrastructure/Identity/Roleseeder.cs` hardcodes:

- Admin email: `obadia@midata-tech.com`
- Admin password: `90Barclaysnew!`

That password is then visible to anyone with access to the repo and is the same as in appsettings.

**What to do:**

- Load admin email and (initial) password from configuration (e.g. `IConfiguration` or environment variables like `AdminSeed:Email`, `AdminSeed:Password`).
- Only run the “create admin user” logic when those settings are present (e.g. only in Development, or behind a one-time setup flag), and remove the hardcoded password from the codebase entirely.

---

## Important (strongly recommended before deploy)

### 3. Duplicate role seeding

**Issue:** In `Program.cs`, `RoleSeeder.SeedRolesAndAdminAsync` is called twice in a row. It’s redundant and can make startup slower.

**What to do:** Remove one of the two `using (var scope ...)` blocks that call the seeder.

### 4. Production configuration

- **AllowedHosts:** In `appsettings.json`, `AllowedHosts` is `"*"`. For production, set it to your real host(s), e.g. `"yourdomain.com;www.yourdomain.com"`.
- **appsettings.Production.json:** Add a file (or use environment variables) to set:
  - `ConnectionStrings:DefaultConnection` (from env/secrets)
  - `EmailSettings` (from env/secrets)
  - `AllowedHosts` to your production host(s)
  - Logging level (e.g. `Information` or `Warning` for production; avoid `Debug`/`Trace` in production unless needed temporarily).

### 5. README vs actual stack

- **README** says “.NET 8 (LTS)”.
- The solution targets **.NET 10** (`net10.0` in the `.csproj` files).

**What to do:** Update the README to state the actual target framework (e.g. .NET 10) and, if you intend to deploy on a specific runtime, note that (e.g. “.NET 10” or “latest LTS”).

### 6. File uploads in production

- Receipts and profile photos are stored under `wwwroot/uploads/` (e.g. `receipts/`, `profiles/`).
- On many hosts (e.g. Azure App Service, Docker, Kubernetes), the filesystem is ephemeral or not shared across instances, so uploads can be lost on restart or scale-out.

**What to do (before or soon after deploy):**

- Ensure the app creates the upload directories on startup if they don’t exist (or handle missing dirs gracefully).
- Plan for production: use external storage (e.g. Azure Blob, AWS S3) and store paths in the database; keep only minimal or no user files on the app server.

### 7. Docker / run instructions

- README mentions running SQL Server in Docker but there is no `Dockerfile` or `docker-compose` in the repo.

**What to do:** If you plan to deploy with Docker:

- Add a `Dockerfile` for the web app (multi-stage build, run as non-root, use production config).
- Optionally add `docker-compose.yml` for local dev (app + SQL Server).
- Document in the README how to run with Docker and how to set connection strings and secrets (env vars or mounts).

---

## Already in good shape

- **Error handling:** Non-Development pipeline uses `UseExceptionHandler("/Home/Error")` and HSTS is enabled.
- **Error page:** Hides development details when not in dev; user sees a generic “Something went wrong” message.
- **Authorization:** Controllers use `[Authorize]` and Admin area uses `[Authorize(Roles = "Admin")]`.
- **HTTPS:** `UseHttpsRedirection()` is in the pipeline.
- **.gitignore:** Covers `bin/`, `obj/`, `.env`, and other common exclusions (but ensure no `appsettings.*` with secrets are committed).
- **Architecture:** Clean separation (Domain, Application, Infrastructure, Web) is clear and maintainable.

---

## Checklist before first production deploy

- [ ] Remove all secrets from `appsettings.json` (and any committed appsettings); use User Secrets (dev) and env/vault (production).
- [ ] Rotate any credentials that were ever committed (email, DB, etc.).
- [ ] Move admin seed credentials out of RoleSeeder into configuration; remove hardcoded password from code.
- [ ] Remove duplicate RoleSeeder call in `Program.cs`.
- [ ] Set `AllowedHosts` for production to your real host(s).
- [ ] Add `appsettings.Production.json` or equivalent production config (no secrets in file; load from env).
- [ ] Update README to match actual .NET version and add deployment/run instructions (and Docker if used).
- [ ] Plan for uploads: either ensure persistent storage for `wwwroot/uploads` or move to cloud storage.
- [ ] Run the app in Production mode locally (e.g. `ASPNETCORE_ENVIRONMENT=Production`) and verify DB connection, login, and core flows.
- [ ] Ensure the production database has migrations applied (`dotnet ef database update` with production connection string).

---

## Summary

The application structure and runtime pipeline are in good shape for deployment. The main blockers are **secrets in configuration and hardcoded credentials**. Address those first, then apply the production config and deployment steps above so the project is both secure and ready to deploy.
