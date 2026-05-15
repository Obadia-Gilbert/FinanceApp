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

---

## 7. Email branding (templating system)

Every outbound email goes through a single branded renderer; there is **no
inline HTML** anywhere in controllers / pages / services. This guarantees the
logo, colors, typography, and footer stay consistent and can be updated in one
place.

### 7.1 How the renderer works

Pipeline:

```
LocalizedEmailTemplates.BuildX(...)
     │   (subjects, headings, paragraphs pulled from SharedResource.{en,es,sw}.resx)
     ▼
EmailTemplate (sealed record: Subject / Preheader / Heading / Body / FooterNote)
     ▼
IEmailTemplateRenderer.RenderHtml(template)   →   branded HTML
                          RenderText(template) →   plaintext fallback
     ▼
IBrandedEmailSender.SendAsync(to, template, ct)
     ▼
IEmailService (BrevoEmailService | EmailService | NoOpEmailService)
```

Source files (`FinanceApp.Infrastructure/Email/`):

| File | Role |
|------|------|
| `EmailBrandingOptions.cs` | Bound from `EmailBranding:*` config — logo URL, brand color, support email, address, feature flags. |
| `EmailTemplate.cs` | Strongly-typed model. `EmailBlock` is a discriminated union (`Paragraph`, `Heading`, `CallToAction`, `InfoBox`, `KeyValueList`). |
| `IEmailTemplateRenderer.cs` / `EmailTemplateRenderer.cs` | Produces the branded HTML wrapper + plaintext. Single 600px card, bulletproof CTAs (Outlook VML + table fallback), `html lang` driven by `CurrentUICulture`, hidden preheader, dark-mode-aware `meta color-scheme`. |
| `LocalizedEmailTemplates.cs` | One `BuildX` factory per notification kind. Pulls every string from `IStringLocalizer<SharedResource>`. |
| `IBrandedEmailSender.cs` / `BrandedEmailSender.cs` | One-liner facade: `await _sender.SendAsync(to, template, ct);`. |

DI registration (already wired in `FinanceApp.API/Program.cs` and
`FinanceApp.Web/Program.cs`):

```csharp
builder.Services.Configure<EmailBrandingOptions>(builder.Configuration.GetSection(EmailBrandingOptions.SectionName));
builder.Services.AddSingleton<IEmailTemplateRenderer, EmailTemplateRenderer>();
builder.Services.AddScoped<LocalizedEmailTemplates>();
builder.Services.AddScoped<IBrandedEmailSender, BrandedEmailSender>();
```

### 7.2 Brand tokens

Bind via `IOptions<EmailBrandingOptions>` from the `EmailBranding` section in
each app's `appsettings.json`:

```json
"EmailBranding": {
  "WebAppBaseUrl": "",              // e.g. https://app.financeapp.io
  "LogoUrl": "",                    // defaults to {WebAppBaseUrl}/branding/email-logo.png
  "BrandName": "FinanceApp",
  "PrimaryColor": "#0d6efd",        // must be hex — bogus values fall back to #0d6efd
  "SupportEmail": "support@financeapp.io",
  "CompanyAddress": "",             // shown in footer for CAN-SPAM/GDPR
  "UnsubscribeUrl": "",             // optional
  "SendWelcomeEmail": true          // feature flag — toggle the welcome email
}
```

Neutrals + typography are hard-coded inline in the renderer (single source of
truth, no theming surface beyond `PrimaryColor`):

- Text: `#1f2937` · Muted: `#6b7280` · Background: `#f3f4f6` · Card: `#ffffff`
- Font stack: `-apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, "Helvetica Neue", Arial, sans-serif`

### 7.3 Logo asset

The renderer references `{LogoUrl}` (or `{WebAppBaseUrl}/branding/email-logo.png`
when blank). The Web app serves the file from
`FinanceApp.Web/wwwroot/branding/email-logo.png` (with `email-logo@2x.png` for
Retina). These files were generated from the existing
`FinanceApp.Web/wwwroot/financeapp-logo.png` via macOS `sips` resampling —
replace them with a true print-quality export from the design source whenever
that becomes available.

### 7.4 Adding a new notification kind

The 4-step recipe:

1. **Add resource keys** to `FinanceApp.Localization/SharedResource.resx`
   (and the `.es` / `.sw` siblings). Naming: `Email_<Kind>_Subject`,
   `Email_<Kind>_Preheader`, `Email_<Kind>_Heading`, `Email_<Kind>_BodyXxx`,
   `Email_<Kind>_CtaLabel`.
2. **Add a `BuildX` factory** to `LocalizedEmailTemplates` returning a fully
   populated `EmailTemplate`.
3. **Call it at the call site**:
   ```csharp
   var template = _emailTemplates.BuildMyNotification(user.FirstName, url);
   await _brandedEmailSender.SendAsync(user.Email, template, ct);
   ```
4. **Add a factory test** to `FinanceApp.API.Tests/Unit/LocalizedEmailTemplatesTests.cs`
   asserting the expected blocks land in `template.Body`.

### 7.5 Localized copy

All subjects, headings, preheaders, body paragraphs, and CTA labels live in
`SharedResource.resx` (`en`, `es`, `sw`). Each method uses the current
`CultureInfo.CurrentUICulture` so the request-localization middleware in both
apps picks the right language automatically. The renderer also sets
`<html lang="…">` from the same culture so screen readers + clients render
correctly.

### 7.6 Local preview harness

In `Development` the Web app exposes a tiny preview endpoint:

- **Index:** `https://localhost:7276/dev/email-preview` — links to every template in every culture.
- **Individual:** `https://localhost:7276/dev/email-preview/{key}?culture={en|es|sw}`

Available keys: `reset-password`, `welcome`, `confirm-email`, `budget-alert`,
`daily-reminder`, `feedback-ack`, `generic`.

Examples:

- `https://localhost:7276/dev/email-preview/reset-password?culture=en`
- `https://localhost:7276/dev/email-preview/budget-alert?culture=es`
- `https://localhost:7276/dev/email-preview/welcome?culture=sw`

The endpoint is automatically disabled outside `Development`.

### 7.7 Tests

- `FinanceApp.API.Tests/Unit/EmailTemplateRendererTests.cs` — branded
  wrapper invariants (logo URL, primary color, preheader, bulletproof CTA,
  `html lang`, info-box tones, key-value list, plaintext flattening).
- `FinanceApp.API.Tests/Unit/LocalizedEmailTemplatesTests.cs` — one assertion
  per `BuildX` factory + a Theory verifying subjects swap between `en` / `es`
  / `sw`.
- `FinanceApp.API.Tests/Unit/BrevoEmailServiceTests.cs` — adds
  `BrandedEmailSender_PostsRenderedHtmlBranding_ThroughBrevo` to verify the
  rendered HTML reaches Brevo with the expected body shape.
- `FinanceApp.API.Tests/Integration/AuthApiTests.cs` — extended with
  `Register_SendsBrandedWelcomeEmail`.
