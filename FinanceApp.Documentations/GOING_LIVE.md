# Going live: production deployment

This guide summarizes recommended hosting paths for **FinanceApp** (ASP.NET Core 10 Web, ASP.NET Core 10 API, SQL Server, Expo mobile), operational tradeoffs, and checklist items you must address before production traffic.

---

## Recommended options (pick one primary path)

### Option A: Lowest monthly bill, you accept operations work

**Single Linux VPS + reverse proxy + Docker** for **API** and **Web**.

| Piece | Approach |
|--------|-----------|
| Compute | One VPS (typical range: small tier on common providers). |
| Edge / TLS | **Caddy** or **nginx** as reverse proxy; terminate HTTPS here. |
| Apps | **Docker Compose** (or similar): containers for `FinanceApp.API` and `FinanceApp.Web`. |
| Database | See [Database strategy](#database-strategy-sql-server-in-docker-vs-managed-sql) below. |
| Uploads | API/Web write supporting documents under `wwwroot`; use **persistent volumes** or plan **object storage** so redeploys do not wipe files. |

**Best for:** Teams comfortable with SSH, backups, patching, and monitoring on their own timeline.

---

### Option B: Fewer surprises, keep SQL Server as-is (managed stack)

**Azure App Service + Azure SQL**, with **Azure Blob Storage** for uploads before you rely on local disk.

| Piece | Approach |
|--------|-----------|
| Web + API | Two App Service apps can share one **App Service Plan** (verify SKU fits CPU/RAM for both apps plus background work). |
| Database | **Azure SQL**—start with a small tier; watch **size**, **DTUs** or **vCores**, and growth. |
| Files | Move or mirror **supporting document** storage to **Blob**; avoid depending on ephemeral App Service local disk for durability. |
| Secrets | **Azure Key Vault** + App Service configuration / managed identity where possible. |

**Best for:** Predictable Microsoft-first hosting, backups and patching handled by the platform, at higher cost than a tiny VPS.

---

### Mobile: not another server tier

Distribution and build pipeline are separate from Web/API hosting:

| Topic | Notes |
|--------|--------|
| Builds | **EAS Build** (Expo) or your CI producing store binaries. |
| Accounts | **Apple Developer Program**, **Google Play** developer account. |
| Production wiring | Stable **API base URL** (HTTPS), valid **TLS** certificates, and correct **OAuth redirect URIs** / app links for Google, Facebook, and any other identity providers. |
| Hosting | End users install from the stores; you are not “hosting” the mobile app on a VM like the API. |

---

## Database strategy: SQL Server in Docker vs managed SQL

| Approach | Cost (rough mental model) | Pain / risk |
|----------|---------------------------|-------------|
| **SQL Server in Docker** on the same VPS | Lowest extra line item | You own backups, upgrades, disk, and **licensing** compliance. Operational burden is on you. |
| **Managed SQL** (e.g. Azure SQL) | Higher monthly cost | Backups, patching, and HA options are largely platform-managed; aligns with existing EF Core SQL Server provider without a database migration. |

Choosing **Docker SQL Server** is viable for early production only if you accept a clear **backup and restore** procedure, disk sizing, and understanding of **license** terms for your scenario. If in doubt, **managed SQL** reduces long-term risk.

---

## HTTPS and CORS for the API (including mobile)

### HTTPS

- Expose the API only over **HTTPS** in production.
- If the API sits behind a reverse proxy or load balancer, configure **forwarded headers** (`X-Forwarded-Proto`, etc.) so ASP.NET Core treats requests as HTTPS when appropriate.
- Use **HSTS** for browser-facing surfaces where it makes sense (marketing Web, account pages).

### CORS

- **Native iOS/Android** API calls from the Expo app **do not** go through browser CORS the same way a web page does; many mobile-only setups still work with default or restrictive CORS.
- CORS **does** matter for:
  - **Swagger / OpenAPI** in the browser,
  - Any **web** client or **Expo Web** calling the API,
  - Future SPAs or third-party browser integrations.

Configure CORS **explicitly**: allow only known origins in production (your Web origin, local dev origins only in Development). Avoid `AllowAnyOrigin` with credentials.

---

## Secrets: never production-only-in-repo

Do **not** commit production values for:

- **JWT** signing keys (or symmetric secrets used for bearer tokens),
- **OAuth** client secrets and related configuration,
- **Connection strings** (SQL Server, Redis if added later, etc.),
- Any third-party API keys.

**Patterns:**

| Environment | Where secrets live |
|-------------|-------------------|
| VPS + Docker | **Docker secrets**, **`.env` files on the host** (not in git), or a small **secret manager**; inject as environment variables. |
| Azure | **Key Vault** references on App Service, **managed identity**, **Deployment** slot settings. |
| CI/CD | **GitHub Actions secrets**, **Azure DevOps variable groups**, etc. |

Keep `appsettings.Production.json` free of real secrets, or omit sensitive keys and require environment configuration.

---

## Background jobs (`IHostedService`)

The API and Web projects use **hosted services** (for example recurring transactions and reminder jobs).

| Deployment shape | Guidance |
|------------------|----------|
| **Single instance** (one VM, or one App Service instance) | Safe starting point: one scheduler runs; behavior matches development assumptions. |
| **Multiple instances** (scale-out) | Risk of **duplicate** runs (e.g. same job firing on every node). You then need **one** of: a **single worker** role, **distributed locks**, **sticky sessions** (usually wrong for APIs), or a **job queue** / **dedicated worker** (Hangfire, Quartz.NET with clustering, Azure Functions timers, etc.). |

Plan for **one production instance** until you explicitly design for scale-out.

---

## .NET 10 on the host

Before you commit to a provider or base image:

- Confirm **runtime** or **container images** support **.NET 10** (or publish **self-contained** deployments so the host only needs a compatible OS/glibc).
- Align your **Dockerfile** `FROM` lines and CI build agents with that decision.

Upgrade or change the host if the platform lags behind your target framework.

---

## Pre-flight checklist (condensed)

- [ ] **DNS** points to the VPS or Azure endpoints; **TLS** certificates valid and auto-renewed.
- [ ] **Connection strings** and **JWT** configuration set via environment or Key Vault, not only in repo.
- [ ] **OAuth** redirect URIs and mobile **app links** match production URLs.
- [ ] **CORS** locked down for production origins; **HTTPS** enforced end-to-end.
- [ ] **Uploads**: persistent storage or Blob; tested backup/restore for DB **and** files.
- [ ] **Background jobs**: single instance **or** documented scale-out strategy.
- [ ] **Mobile**: production **API URL** in build/env; store listings and EAS profiles updated.
- [ ] **Monitoring** and **alerts** (uptime, 5xx rate, DB capacity, disk).
- [ ] **Transactional email** configured (e.g. Brevo HTTP API or SMTP relay) and sender domain verified — see [EMAIL_BREVO.md](./EMAIL_BREVO.md).

---

## Related documentation

- [FinanceApp-Architecture.md](./FinanceApp-Architecture.md) — architecture, API, security context.
- [Current-State.md](./Current-State.md) — what is implemented today.
- [EMAIL_BREVO.md](./EMAIL_BREVO.md) — transactional email (Brevo HTTP API / SMTP).
