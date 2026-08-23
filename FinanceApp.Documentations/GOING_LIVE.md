# Going live: production hosting

Concrete, low-budget hosting plan for **FinanceApp** (ASP.NET Core 10 Web + API, SQL Server, Expo mobile).

For store submission (Apple / Google), see **[STORE_SUBMISSION.md](./STORE_SUBMISSION.md)**.

> **Prices below were checked in August 2026 and will drift.** Treat them as a planning model, not a quote.

---

## TL;DR

**One always-on Linux VPS running everything, behind Caddy, with SQL Server Express in Docker.**

| | |
|---|---|
| Infrastructure | **~$4–6/month** |
| Year 1 all-in (incl. Apple $99/yr + Google $25 once) | **~$196** (~$16/mo average) |

This is not the "cheapest thing that runs" — it is the cheapest thing that runs **without degrading the user experience**. The reasoning is below, and it matters: the obvious free tiers actively break this app.

---

## Why this shape — the code decides, not preference

Three properties of the codebase eliminate most cheap hosting options before cost is even considered.

### 1. Always-on is mandatory — no scale-to-zero

Three `BackgroundService` loops run the product's core automation:

| Job | Interval | Registered in |
|---|---|---|
| `RecurringTransactionJob` | 1 hour | API **and** Web |
| `DailyActivityReminderJob` | 1 hour | API only |
| `ExchangeRateRefreshJob` | 8 hours (configurable) | API **and** Web |

All three are plain `while (!stoppingToken.IsCancellationRequested) { work; await Task.Delay(interval); }`. **They do not backfill.** If the process sleeps, the delay never elapses and the missed windows are simply lost — a user's recurring rent entry silently doesn't get created, and their daily reminder never arrives.

This rules out **Render free**, **Fly.io auto-stop**, **Google Cloud Run**, **Azure App Service F1**, and every other tier that idles your process. It is the single biggest way cheap hosting would visibly hurt users here.

> ⚠️ Note the two jobs registered in **both** projects. Running Web and API as separate processes against one database **duplicates recurring transactions**. Gate them with `Jobs:Enabled` (see [DEPLOYMENT_READINESS.md](../DEPLOYMENT_READINESS.md)) so exactly one process runs jobs.

### 2. SQL Server is structurally locked in

Moving to PostgreSQL is a **rewrite, not a config change**:

- `FinanceDbContext.OnModelCreating` carries a model-level `HasDefaultValueSql("GETUTCDATE()")` on `ApplicationUser.SubscriptionAssignedAt`, plus `HasColumnType("decimal(18,2)")` in seven places. These re-emit into *every future migration* regardless of provider.
- All 23 migrations are full of `nvarchar(max)`, `uniqueidentifier`, `bit`, `datetimeoffset`, `SqlServer:Identity` annotations, bracket-quoted raw SQL (`20260822114040_CurrencyAsIsoCode`), and bracket-quoted index filters.
- No `Npgsql` package is referenced anywhere.
- The SQLite test branch detects the provider by extension type name — a Postgres provider would fall through to the SQL Server path.

Realistic cost: regenerate all migrations from scratch, strip the provider-specific model config, and write a data migration. Weeks, not hours. **Not worth doing before launch.** It becomes worth doing at the scale triggers below.

### 3. A persistent disk is mandatory

Uploads are 100% local filesystem with **no storage abstraction anywhere** (no `IFileStorage`, no S3/Blob client):

| Path | Written by |
|---|---|
| `{WebRoot}/uploads/documents/{UserId}/` | `SupportingDocumentService` |
| `{WebRoot}/uploads/profiles/` | `ProfileController`, `Register.cshtml.cs`, `AccountDeletionService` |
| `wwwroot/uploads/receipts/` | `ExpenseController` |

Plus `SubscriptionBilling:Google:ServiceAccountJsonPath` reads a credentials file from disk.

Ephemeral-disk platforms lose every receipt on redeploy. And because Web and API **share this directory**, they must run on the same host — a document uploaded via the API is invisible to Web otherwise.

---

## Cost breakdown

| Component | Choice | Cost |
|---|---|---|
| VPS | Hetzner CX22 — 2 vCPU / 4 GB / 40 GB SSD | **€3.79/mo** (~$4.10) |
| TLS + reverse proxy | Caddy (automatic Let's Encrypt) | free |
| Database | SQL Server 2022 **Express** in Docker | free |
| DNS + CDN + DDoS | Cloudflare free tier | free |
| Transactional email | Brevo free tier (300/day, HTTP API) | free |
| Exchange rates | `open.er-api.com` | free |
| Off-box DB backup | nightly dump → Backblaze B2 | free (10 GB tier) |
| Domain | Cloudflare Registrar / Namecheap | ~$12/yr |
| **Apple Developer Program** | required to ship to App Store | **$99/yr** |
| **Google Play Console** | one-time | **$25** |

**4 GB RAM is the floor, not a preference.** SQL Server on Linux requires 2 GB minimum and Microsoft recommends more; add two .NET processes (~150–250 MB each) plus Caddy. A 2 GB box will OOM under load.

Comparable alternatives if Hetzner is unavailable in your region: **Contabo** (cheaper per GB, slower I/O), **Netcup**, **OVH**. Avoid DigitalOcean/Vultr/Linode at this tier — their 4 GB plans start around $24/mo for the same thing.

---

## Architecture

```
                    Cloudflare (DNS, CDN, DDoS — free)
                              │
                    ┌─────────▼─────────┐
                    │   Caddy :80/:443  │  automatic Let's Encrypt
                    └────┬─────────┬────┘
              app.domain │         │ api.domain
                    ┌────▼───┐ ┌───▼────┐
                    │  Web   │ │  API   │   Jobs:Enabled=true on ONE only
                    │ :8080  │ │ :8080  │
                    └────┬───┘ └───┬────┘
                         └────┬────┘
                   ┌──────────▼──────────┐    ┌──────────────────┐
                   │ SQL Server Express  │    │ /srv/uploads     │
                   │   (Docker volume)   │    │ (shared volume)  │
                   └─────────────────────┘    └──────────────────┘
                              │
                    nightly backup → Backblaze B2
```

---

## Runbook

### 1. Provision

```bash
# Ubuntu 24.04 LTS, Docker + Compose plugin
curl -fsSL https://get.docker.com | sh

# Non-root user, key-only SSH, firewall
adduser deploy && usermod -aG docker deploy
ufw allow OpenSSH && ufw allow 80 && ufw allow 443 && ufw enable
```

Harden SSH (`/etc/ssh/sshd_config`): `PasswordAuthentication no`, `PermitRootLogin no`.

### 2. DNS

Point `app.yourdomain.com` and `api.yourdomain.com` at the VPS. Proxy through Cloudflare (orange cloud) for the free CDN and DDoS protection. Set SSL/TLS mode to **Full (strict)** — Caddy holds a real certificate, so Flexible would be a downgrade.

### 3. Secrets

Create `/srv/financeapp/.env` on the host — **never in git**:

```bash
# Database
SA_PASSWORD=<strong-random>
ConnectionStrings__DefaultConnection=Server=db,1433;Database=FinanceApp;User Id=sa;Password=<same>;TrustServerCertificate=true;

# JWT — MUST be overridden; the repo default is public knowledge
Jwt__Key=<openssl rand -base64 48>
Jwt__Issuer=FinanceApp.API
Jwt__Audience=FinanceApp

# Only ONE process runs background jobs
Jobs__Enabled=true

# Email (Brevo HTTP API — avoids blocked SMTP ports)
Brevo__ApiKey=<key>
Brevo__SenderEmail=noreply@yourdomain.com

# Public URLs
PasswordReset__WebAppBaseUrl=https://app.yourdomain.com
EmailBranding__WebAppBaseUrl=https://app.yourdomain.com
Cors__AllowedOrigins__0=https://app.yourdomain.com

# OAuth, Stripe, store billing — see the full key list below
```

`chmod 600 .env`. Every key name is documented in [Configuration reference](#configuration-reference).

### 4. Database first — before the app ever starts

> 🚨 **Do this in the right order or you permanently break migrations.**
>
> `FinanceApp.API/Program.cs` currently calls `EnsureCreatedAsync()` on startup. Against a fresh empty database that creates the schema **with no `__EFMigrationsHistory` rows** — after which `dotnet ef database update` fails forever and no future migration can be applied. Apply migrations *before* first boot, and fix the startup call (tracked in [DEPLOYMENT_READINESS.md](../DEPLOYMENT_READINESS.md)).

```bash
docker compose up -d db          # SQL Server only
dotnet ef database update --project FinanceApp.Infrastructure \
                          --startup-project FinanceApp.API \
                          --connection "$ConnectionStrings__DefaultConnection"
docker compose up -d             # now the apps
```

### 5. Caddyfile

```caddy
app.yourdomain.com {
    reverse_proxy web:8080
    encode gzip zstd
}

api.yourdomain.com {
    reverse_proxy api:8080
    encode gzip zstd
}
```

That's the entire TLS setup — Caddy provisions and renews Let's Encrypt certificates automatically.

Because traffic arrives over HTTP inside the network, both apps need `UseForwardedHeaders` or `Request.Scheme` reads as `http` and `UseHttpsRedirection` can produce a redirect loop.

### 6. Backups

```bash
# /etc/cron.daily/financeapp-backup
docker exec financeapp-db /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa \
  -P "$SA_PASSWORD" -C -Q "BACKUP DATABASE FinanceApp TO DISK='/backups/db.bak' WITH INIT, COMPRESSION"
rclone copy /srv/financeapp/backups b2:financeapp-backups --max-age 24h
rclone copy /srv/uploads b2:financeapp-uploads
```

**Test the restore.** An untested backup is not a backup.

---

## Keeping the experience good on a cheap box

The budget constraint should be invisible to users. What makes that true here:

- **No cold starts.** Always-on is the main UX advantage over free tiers — every request hits a warm process.
- **Cloudflare CDN** in front. Bootstrap, Chart.js and the app's CSS/JS are self-hosted with `asp-append-version`, so they cache aggressively at the edge.
- **Fast redeploys.** `docker compose up -d` with a health-gated cutover is seconds of overlap, not an outage window.
- **Response caching + gzip/zstd** at Caddy.
- **Honest single-point-of-failure.** One VPS is not HA. If the box dies you restore from the nightly B2 backup onto a new one — realistically 30–60 minutes. That is an acceptable trade at this budget, but it should be a decision, not a surprise.

---

## Scale triggers — when this stops being enough

| Signal | Limit | What to do |
|---|---|---|
| Database size | **10 GB** (SQL Server Express hard cap) | Migrate to Azure SQL (~$15–30/mo) **or** do the Postgres migration |
| Transactional email | **300/day** (Brevo free) | Brevo paid ≈ $25/mo for 20k/mo |
| RAM pressure / slow queries | 4 GB | Resize the VPS (Hetzner resize is a reboot) |
| Need for zero-downtime or HA | single instance | Requires: distributed job locking, shared Data Protection keys, object storage for uploads — all currently absent |

The 10 GB cap is the one to watch. At ~2 KB/row for expenses it's a long way off, but uploaded receipts live on disk, not in the DB, so disk fills first — monitor both.

---

## Configuration reference

Required for startup:

| Key | Notes |
|---|---|
| `ConnectionStrings__DefaultConnection` | SQL Server |
| `Jwt__Key` | **API refuses to start without it.** The repo default is publicly known — always override |

Also set in production: `Jwt__Issuer`, `Jwt__Audience`, `Jwt__ExpirationMinutes`, `Jobs__Enabled`, `AllowedHosts`, `Cors__AllowedOrigins__*`.

| Area | Keys |
|---|---|
| OAuth | `Authentication__Google__ClientId` / `ClientSecret` / `IdTokenAudiences`, `Authentication__Facebook__AppId` / `AppSecret`, `Authentication__Twitter__ConsumerKey` / `ConsumerSecret` |
| Email | `Brevo__ApiKey`, `Brevo__SenderEmail`, `Brevo__SenderName`; or the `EmailSettings__*` SMTP block |
| Branding / links | `EmailBranding__WebAppBaseUrl`, `PasswordReset__WebAppBaseUrl` |
| Billing | `SubscriptionBilling__Stripe__SecretKey` / `WebhookSecret`, `SubscriptionBilling__Google__PackageName` / `ServiceAccountJsonPath`, `SubscriptionBilling__Webhooks__Apple__SharedSecret` / `Google__SharedSecret` |
| Admin seed | `AdminSeed__Email`, `AdminSeed__Password` |
| Exchange rates | `ExchangeRates__Provider__BaseUrl` / `RefreshIntervalHours` (free, no key) |

⚠️ **Two email gotchas.** `FinanceApp.Web` hardcodes the SMTP `EmailService` and does **not** use the API's Brevo-first fallback chain — configure the full `EmailSettings__*` block for Web even if Brevo is set for the API. And if neither is configured, `NoOpEmailService` silently swallows every message: password reset appears to work and no email ever arrives.

⚠️ **`Shared/appsettings.shared.json` is loaded via a `../Shared/` relative path** that won't exist in a container. It's `Optional = true`, so it silently no-ops — supply `SubscriptionBilling` and `ExchangeRates` defaults via environment variables instead.

---

## Pre-flight checklist

- [ ] Secrets **rotated** — the SMTP and admin passwords in git history are compromised ([DEPLOYMENT_READINESS.md](../DEPLOYMENT_READINESS.md))
- [ ] `Jwt__Key` overridden with a fresh random value
- [ ] Migrations applied **before** first app boot; `EnsureCreatedAsync` removed
- [ ] `Jobs__Enabled=true` on exactly one process
- [ ] Data Protection keys persisted (otherwise sessions drop on every redeploy)
- [ ] CORS locked to production origins
- [ ] `UseForwardedHeaders` configured
- [ ] `/health` responding; uptime monitor pointed at it
- [ ] Uploads on a persistent volume shared by Web and API
- [ ] Nightly DB + uploads backup running, **restore tested**
- [ ] OAuth redirect URIs updated to production hostnames
- [ ] Brevo sender domain verified ([EMAIL_BREVO.md](./EMAIL_BREVO.md))
- [ ] Mobile `EXPO_PUBLIC_API_URL` points at `https://api.yourdomain.com`

---

## Related documentation

- [STORE_SUBMISSION.md](./STORE_SUBMISSION.md) — Apple App Store + Google Play submission
- [DEPLOYMENT_READINESS.md](../DEPLOYMENT_READINESS.md) — security/config blockers
- [EMAIL_BREVO.md](./EMAIL_BREVO.md) — transactional email
- [SUBSCRIPTIONS_IAP.md](./SUBSCRIPTIONS_IAP.md) — IAP, webhooks, sandbox
- [FinanceApp-Architecture.md](./FinanceApp-Architecture.md) — architecture reference
