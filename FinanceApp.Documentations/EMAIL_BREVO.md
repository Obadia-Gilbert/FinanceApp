# Email: Brevo (ex-Sendinblue)

FinanceApp uses **Brevo** as the default transactional email provider for the
forgot-password flow (and any future emails such as monthly reports, budget
alerts, etc.).

| Tier | Limit | Cost |
|------|-------|------|
| Free | 300 emails/day | $0 |
| Entry paid | 20,000 emails/month | ~$25/mo |

The free tier is enough for MVP / soft launch; upgrade only when you start
hitting the daily cap.

---

## 1. Choose an integration path

The codebase supports **two** Brevo integrations. Configure only one:

| Path | Service | When to use |
|------|---------|-------------|
| **HTTP API** (recommended) | `BrevoEmailService` | Production. Faster, returns a `messageId`, surfaces errors in the HTTP response. |
| **SMTP relay** (fallback)  | `EmailService`     | Legacy SMTP stacks, on-prem mail gateways, or when port 443 outbound is restricted. |

Selection is automatic and based on configuration (see [§4 Wiring priority](#4-wiring-priority)).

---

## 2. Create the Brevo account + get credentials

1. Sign up at <https://app.brevo.com> and log in.
2. **Verify your sender domain** (preferred) or at least your sender email
   under *Senders, Domains & Dedicated IPs → Senders*. Without verification
   Brevo will reject sends from that address. For best deliverability also
   add the **SPF**, **DKIM**, and **DMARC** DNS records Brevo lists for your
   domain.
3. Generate credentials, depending on the path you picked:

   - **HTTP API:** *SMTP & API → API Keys → Generate a new API key*. The key
     starts with `xkeysib-`. Save it; you cannot retrieve it later.
   - **SMTP relay:** *SMTP & API → SMTP*. You get a **login** (looks like
     `xxxxxx@smtp-brevo.com`) and an **SMTP key**. Host is
     `smtp-relay.brevo.com`, port `587` with STARTTLS.

---

## 3. Configure FinanceApp

The same configuration keys work for both **FinanceApp.API** and
**FinanceApp.Web**. Do **not** commit real keys; use user secrets locally and
environment variables / a secret manager in production (see
[GOING_LIVE.md § Secrets](./GOING_LIVE.md#secrets-never-production-only-in-repo)).

### 3a. HTTP API (recommended)

`appsettings.*.json` skeleton:

```json
{
  "Brevo": {
    "ApiKey": "xkeysib-...",
    "SenderName": "FinanceApp",
    "SenderEmail": "noreply@yourdomain.com",
    "ReplyToEmail": "support@yourdomain.com",
    "ReplyToName": "FinanceApp Support",
    "TimeoutMs": 10000
  }
}
```

Local dev with **user secrets** (from the project folder):

```bash
# FinanceApp.API
cd FinanceApp.API
dotnet user-secrets set "Brevo:ApiKey"       "xkeysib-..."
dotnet user-secrets set "Brevo:SenderEmail"  "noreply@yourdomain.com"
dotnet user-secrets set "Brevo:SenderName"   "FinanceApp"

# FinanceApp.Web — same keys
cd ../FinanceApp.Web
dotnet user-secrets set "Brevo:ApiKey"       "xkeysib-..."
dotnet user-secrets set "Brevo:SenderEmail"  "noreply@yourdomain.com"
dotnet user-secrets set "Brevo:SenderName"   "FinanceApp"
```

Production: set as environment variables, e.g.
`Brevo__ApiKey`, `Brevo__SenderEmail`, `Brevo__SenderName`.

### 3b. SMTP relay (fallback)

Leave `Brevo:ApiKey` **empty** and fill in the existing `EmailSettings` block:

```json
{
  "EmailSettings": {
    "SmtpServer": "smtp-relay.brevo.com",
    "Port": 587,
    "SenderName": "FinanceApp",
    "SenderEmail": "noreply@yourdomain.com",
    "Username": "xxxxxx@smtp-brevo.com",
    "Password": "your-brevo-smtp-key",
    "EnableSsl": true,
    "TimeoutMs": 10000
  }
}
```

---

## 4. Wiring priority

`Program.cs` in both API and Web picks the implementation in this order:

1. `Brevo:ApiKey` present → `BrevoEmailService` (HTTP API).
2. Else `EmailSettings:SmtpServer` present → `EmailService` (SMTP).
3. Else → `NoOpEmailService` — logs a warning and swallows the message. Used
   in `Testing` and any environment without email configured so flows like
   forgot-password don't 500.

This means existing SMTP deployments keep working unchanged; setting a Brevo
API key transparently upgrades them to the HTTP path.

---

## 5. Verifying it works

1. Start the API or Web project against a real `Brevo:ApiKey`.
2. POST to `/api/auth/forgot-password` (API) or use the *Forgot password*
   link on the Web login page.
3. Check the recipient inbox **and** the Brevo dashboard under
   *Transactional → Statistics → Email* — every sent message shows up with a
   `messageId` you can search.

If sending fails:

- **401 Unauthorized** → wrong/disabled API key.
- **400 Bad Request, "sender not allowed"** → sender email/domain not
  verified in Brevo.
- **Rate limiting** → you exceeded 300/day on free tier; upgrade or wait 24h.

The service logs error responses through `ILogger<BrevoEmailService>` at
`Error` level.

---

## 6. Related

- [GOING_LIVE.md](./GOING_LIVE.md) — secrets, HTTPS, hosting choices.
- [MOBILE_AUTH.md](./MOBILE_AUTH.md) — mobile-side forgot-password flow.
- Source:
  - `FinanceApp.Infrastructure/Services/BrevoEmailService.cs`
  - `FinanceApp.Infrastructure/Services/BrevoSettings.cs`
  - `FinanceApp.Infrastructure/Services/EmailService.cs` (SMTP fallback)
  - `FinanceApp.Infrastructure/Services/NoOpEmailService.cs` (no-config fallback)
