# Web app + API: Google sign-in configuration

`FinanceApp.Web` uses **ASP.NET Core Google authentication** (server-side OAuth with redirect back to `/signin-google`). `FinanceApp.API` does **not** host a browser OAuth redirect; it only **validates Google ID tokens** from the mobile app on `POST /api/auth/external`.

Use the **same Google Cloud project** and typically the **same Web application** OAuth client for both: Web app (`ClientId` + `ClientSecret`) and API token validation (`ClientId` + optional `IdTokenAudiences`).

---

## 1. Google Cloud Console (Web application client)

Credentials → create or edit **Web application**.

### Authorized JavaScript origins

Add every origin you use in development and production, for example:

| Environment | Origins |
|-------------|---------|
| Web dev (`http` profile) | `http://localhost:5279` |
| Web dev (`https` profile) | `https://localhost:7276` |
| Production | `https://your-production-domain` |

### Authorized redirect URIs

ASP.NET Core’s default Google callback path is **`/signin-google`**. Add a redirect URI for **each** scheme/host/port you use:

| Environment | Redirect URI |
|-------------|----------------|
| Web dev (`http` profile, default in `launchSettings.json`) | `http://localhost:5279/signin-google` |
| Web dev (`https` profile) | `https://localhost:7276/signin-google` |
| Production | `https://your-production-domain/signin-google` |

Do **not** put mobile-only URIs here unless you also use that same Web client for mobile browser OAuth (Expo). Mobile native / reversed-scheme URIs belong on the **iOS** or **Android** OAuth clients — see [MOBILE_AUTH.md](./MOBILE_AUTH.md).

---

## 2. FinanceApp.Web

Set `Authentication:Google:ClientId` and `Authentication:Google:ClientSecret` (same **Web application** client as in Google Cloud). Prefer **user secrets** locally:

```bash
cd FinanceApp.Web
dotnet user-secrets set "Authentication:Google:ClientId" "<web-client-id>.apps.googleusercontent.com"
dotnet user-secrets set "Authentication:Google:ClientSecret" "<web-client-secret>"
```

Restart the site. The login and register pages show **Continue with Google** when this provider is registered (`Program.cs` only calls `AddGoogle` when both values are non-empty).

---

## 3. FinanceApp.API

The API validates Google **ID tokens**; configure audiences so tokens issued to Web, iOS, or Android clients are accepted:

```bash
cd FinanceApp.API
dotnet user-secrets set "Authentication:Google:ClientId" "<web-client-id>.apps.googleusercontent.com"
dotnet user-secrets set "Authentication:Google:ClientSecret" "<web-client-secret>"
dotnet user-secrets set "Authentication:Google:IdTokenAudiences" "<web-client-id>.apps.googleusercontent.com,<ios-client-id>.apps.googleusercontent.com,<android-client-id>.apps.googleusercontent.com"
```

`IdTokenAudiences` must list every OAuth **client ID** that can mint an ID token your mobile app sends. The web MVC flow does not require extra redirect URIs on the API.

---

## 4. Quick verification

1. **Web:** Open `http://localhost:5279/Identity/Account/Login` (or your HTTPS URL), click **Continue with Google**, complete the flow, and confirm you land signed in.
2. **API:** From the mobile app or a REST client, call `POST /api/auth/external` with a Google `idToken` and confirm `200` (not `Api_OAuthInvalidToken` / `Api_OAuthNotConfigured`).

See also [MOBILE_AUTH.md §3](./MOBILE_AUTH.md#3-google-sign-in) for mobile-specific clients and troubleshooting.
