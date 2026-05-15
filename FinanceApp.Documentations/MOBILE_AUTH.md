# Mobile authentication: setup and troubleshooting

End-to-end setup for the Expo mobile app (`FinanceApp.Mobile`) talking to `FinanceApp.API`.

Covers email/password login plus **Google** and **Facebook** social sign-in across iOS Simulator, Android Emulator, physical devices, Expo Go, and dev builds.

---

## 1. API base URL by run target

`FinanceApp.Mobile/.env` controls the API endpoint via `EXPO_PUBLIC_API_URL`. The value is **inlined at bundle time** — restart Expo with `npx expo start -c` after edits.

| Run target | `EXPO_PUBLIC_API_URL` | API launch profile |
|------------|-----------------------|--------------------|
| iOS Simulator (Mac) | `http://127.0.0.1:5022` | `http` |
| Android Emulator | `http://10.0.2.2:5022` | `http` |
| Physical device (same Wi-Fi) | `http://<your-mac-LAN-IP>:5279` | **`Mobile`** (binds `0.0.0.0:5279`) |
| Production | `https://api.your-domain` | n/a (deployed) |

**Why these differ**

- `127.0.0.1` inside the Android emulator is the emulator itself, not your Mac. Use `10.0.2.2`.
- A physical device cannot reach `127.0.0.1` on your Mac at all. Use the Mac’s LAN IP and run the API on `0.0.0.0` (the **Mobile** profile in `Properties/launchSettings.json` does this).
- iOS allows `NSAllowsLocalNetworking` for `localhost` and RFC 1918 addresses in dev. For a physical device on production the API **must** be HTTPS.

**Do not** end the URL with `/api`; the client already prefixes paths with `/api/...`.

---

## 2. Avoid HTTPS redirect surprises in dev

`FinanceApp.API/Program.cs` enables `app.UseHttpsRedirection()` outside the `Testing` environment. If you launch with the **`https`** profile, plain HTTP requests get redirected to `https://localhost:7073` — a self-signed certificate that **mobile clients won’t trust**.

**Use the `http` or `Mobile` profile for mobile development**, not `https`.

---

## 3. Google Sign-In

### Required OAuth clients in Google Cloud

Open **Google Cloud Console → APIs & Services → Credentials** for the project that owns the OAuth consent screen. You need **three** OAuth client IDs:

| Type | Use | Bundle / package |
|------|-----|------------------|
| **Web application** | Token validation on the API; fallback for browser OAuth in Expo Go. | n/a |
| **iOS** | Native iOS Google Sign-In (dev build) and reversed URL scheme. | `com.financeapp.mobile` |
| **Android** | Native Android Google Sign-In. | Package `com.financeapp.mobile` + your debug + release **SHA-1** fingerprints |

Get the Android debug SHA-1 with:

```bash
keytool -list -v -keystore ~/.android/debug.keystore -alias androiddebugkey -storepass android -keypass android
```

### Mobile env (`FinanceApp.Mobile/.env`)

```env
EXPO_PUBLIC_GOOGLE_WEB_CLIENT_ID=<web-client-id>.apps.googleusercontent.com
EXPO_PUBLIC_GOOGLE_IOS_CLIENT_ID=<ios-client-id>.apps.googleusercontent.com
EXPO_PUBLIC_GOOGLE_ANDROID_CLIENT_ID=<android-client-id>.apps.googleusercontent.com
```

`app.config.js` derives the iOS reversed URL scheme **from the iOS client ID**. Without `EXPO_PUBLIC_GOOGLE_IOS_CLIENT_ID`, the native Google plugin is intentionally omitted (Expo Go browser flow can still work).

### API audience configuration (user secrets, not committed)

`FinanceApp.API` validates the Google **ID token** against an audience list:

```bash
cd FinanceApp.API
dotnet user-secrets set "Authentication:Google:ClientId" "<web-client-id>.apps.googleusercontent.com"
dotnet user-secrets set "Authentication:Google:ClientSecret" "<web-client-secret>"
dotnet user-secrets set "Authentication:Google:IdTokenAudiences" "<web-client-id>.apps.googleusercontent.com,<ios-client-id>.apps.googleusercontent.com,<android-client-id>.apps.googleusercontent.com"
```

The audience list **must include every client ID that the mobile app may use to obtain the ID token**. Otherwise `/api/auth/external` rejects with `Api_OAuthInvalidToken` or `Api_OAuthNotConfigured`.

### Run-target matrix

| Run target | Google flow used | Required env keys | Notes |
|------------|------------------|-------------------|-------|
| **Expo Go** (any sim/device) | Browser OAuth via `expo-auth-session` (`GoogleSignInExpoGo.tsx`). | At minimum `EXPO_PUBLIC_GOOGLE_WEB_CLIENT_ID`; ideally also iOS/Android client IDs. | Add Expo proxy URL to the **Web** OAuth client’s **Authorized redirect URIs** (Google Cloud) if Expo proxy is in use. |
| **Dev build (iOS)** | Native via `@react-native-google-signin/google-signin` (`GoogleSignInButtonImpl.tsx`). | Web + **iOS** client IDs. | Reversed URL scheme is auto-injected from the iOS client ID. Must be a full dev build, not Expo Go. |
| **Dev build (Android)** | Native. | Web + **Android** client IDs (Android needs SHA-1s in Cloud Console). | |

### Common Google failures

| Symptom | Likely cause | Fix |
|--------|--------------|------|
| `redirect_uri_mismatch` | Web OAuth client authorized redirect URIs don’t include the proxy / scheme being used. | Add Expo proxy URL or the native scheme `com.googleusercontent.apps.<ios-prefix>:/oauthredirect` to the matching client. |
| `Google did not return an ID token` | Mismatch between platform and configured client; Web client used for native flow. | Set the platform-specific client ID (iOS or Android) in `.env` and rebuild. |
| API returns `Api_OAuthNotConfigured` | API is missing `Authentication:Google:ClientId` / `Authentication:Google:IdTokenAudiences`. | Set the user secrets shown above. |
| API returns `Api_OAuthInvalidToken` | Token signed for a client ID not in the API’s audience list. | Add that client ID to `Authentication:Google:IdTokenAudiences`. |
| Native iOS button does nothing | Missing iOS client ID in `.env` (impl errors silently in some flows). | Set `EXPO_PUBLIC_GOOGLE_IOS_CLIENT_ID`, rebuild dev client. |

---

## 4. Facebook Login

### Mobile env

```env
EXPO_PUBLIC_FACEBOOK_APP_ID=<facebook-app-id>
```

`app.config.js` adds the `fb<APP_ID>` URL scheme automatically.

### API user secrets (server-side only)

```bash
cd FinanceApp.API
dotnet user-secrets set "Authentication:Facebook:AppId" "<facebook-app-id>"
dotnet user-secrets set "Authentication:Facebook:AppSecret" "<facebook-app-secret>"
```

If `AppSecret` is empty, `FacebookDebugTokenValidAsync` skips token validation — acceptable for early dev only. **Set both before production.**

### Facebook console settings

- **Valid OAuth Redirect URIs** must include `fb<APP_ID>://authorize`.
- App domains and platform settings must list the bundle ID `com.financeapp.mobile`.
- For Expo Go using the proxy, add the Expo redirect URL as well.

---

## 5. Forgot / reset password

The mobile app uses the same Identity password-reset tokens as the Web app —
the API issues them through `UserManager.GeneratePasswordResetTokenAsync` and
the reset email is sent by `IEmailService` (Brevo HTTP API in production,
SMTP fallback, or `NoOpEmailService` if nothing is configured — see
[EMAIL_BREVO.md](./EMAIL_BREVO.md)).

### Mobile screens

| Screen | Route | Purpose |
|--------|-------|---------|
| Forgot password | `app/(auth)/forgot-password.tsx` | Email input → POSTs `/api/auth/forgot-password`. Always shows a generic "If an account exists…" confirmation to avoid account enumeration. |
| Reset password  | `app/(auth)/reset-password.tsx`  | Code/email/new-password form → POSTs `/api/auth/reset-password`. Reads `email` + `code` from route params or deep link query string; the same screen handles both manual paste and deep-link entry. |

The Forgot Password success state exposes a **"I already have a reset code"**
link that navigates to `reset-password` with the email pre-filled. This is
the fallback for users who can't (or don't want to) tap the link in the
email on the same device.

### Where does the reset email link point?

`FinanceApp.API/appsettings.json` (or env / user secrets) controls this via:

```jsonc
"PasswordReset": {
  // Where the link in the reset email goes — defaults to the Web app.
  "WebAppBaseUrl": "https://localhost:7276",
  "ResetPath":     "/Identity/Account/ResetPassword"
}
```

`AuthController.ForgotPassword` builds the link as
`{WebAppBaseUrl}{ResetPath}?area=Identity&code=<base64url>&email=<email>` and
hands it to `IEmailService`. The Web `ResetPassword` Razor page decodes the
token in `OnGet` and submits the raw token to `UserManager.ResetPasswordAsync`
on `OnPost`. This works unchanged for mobile users — the link opens in the
device browser, the user picks a new password on the Web form, then signs
in on mobile with the new password. **This is the chosen default path**
because it doesn't require an installed mobile build, deep-link
registration, or any per-device universal-link configuration.

### Optional: deep-link straight into the mobile app

The mobile bundle is registered with the `financeapp` scheme
(`app.json` → `expo.scheme`), so `reset-password` can also be reached via
deep link:

```
financeapp:///(auth)/reset-password?email=user@example.com&code=<base64url>
```

To switch the email link to the deep-link target, override the two
`PasswordReset` keys (env var form):

```bash
PasswordReset__WebAppBaseUrl=financeapp:///
PasswordReset__ResetPath=(auth)/reset-password
```

Caveats:

- The mobile app must be installed on the device that opens the email; if
  not, the OS shows the standard "no app to open this link" message.
- Email clients differ in how aggressively they strip / rewrite custom
  schemes. For production, prefer **Apple Universal Links / Android App
  Links** (HTTPS URLs that open the app when installed, otherwise the Web
  fallback). That requires `apple-app-site-association` + Android
  `assetlinks.json` files hosted on the same HTTPS origin used in
  `WebAppBaseUrl`, plus an `expo-router` Linking config — out of scope
  for the initial launch.
- Until universal links are configured, leave the default Web-app target.
  Users who insist on completing the reset on mobile can still use the
  **"I already have a reset code"** link in the forgot-password screen
  and paste the `code` query-string value from the email.

### API endpoints

| Method | Path | Body | Response |
|--------|------|------|----------|
| POST | `/api/auth/forgot-password` | `{ "email": "user@example.com" }` | `204 No Content` (always, unless email is malformed → `400`) |
| POST | `/api/auth/reset-password`  | `{ "email": "...", "code": "<base64url>", "newPassword": "..." }` | `204 No Content` on success, `400 BadRequest` on invalid token / weak password |

The mobile client wraps these in `src/api/auth.ts`:
`forgotPassword(email)` and `resetPassword({ email, code, newPassword })`.

### Brevo / email configuration

The API resolves `IEmailService` in this order — same logic in the Web
project (see [EMAIL_BREVO.md](./EMAIL_BREVO.md) for full details):

1. `Brevo:ApiKey` set → `BrevoEmailService` (HTTP API, recommended).
2. Else `EmailSettings:SmtpServer` set → SMTP `EmailService`.
3. Else → `NoOpEmailService` (logs a warning, returns success — used for
   `Testing` and any dev environment without email configured so
   `/api/auth/forgot-password` doesn't 500).

For local dev, set Brevo via user secrets on the API project:

```bash
cd FinanceApp.API
dotnet user-secrets set "Brevo:ApiKey"       "xkeysib-..."
dotnet user-secrets set "Brevo:SenderEmail"  "noreply@yourdomain.com"
dotnet user-secrets set "Brevo:SenderName"   "FinanceApp"
```

---

## 6. JWT and refresh tokens

`FinanceApp.API` issues a JWT plus a refresh token. The mobile client stores them with `expo-secure-store`:

| Key | Purpose |
|-----|---------|
| `auth_token` | Bearer JWT for API calls. |
| `refresh_token` | Used by `apiFetch` on `401` to call `/api/auth/refresh`. |
| `auth_user` | Cached email / first / last name. |

`apiFetch` (in `src/api/client.ts`) automatically retries a request once after a successful refresh. If the refresh fails, secure storage is cleared and the user is logged out.

`Jwt:Key` in `appsettings.json` is a **placeholder**. Replace it via user secrets or environment for any non-trivial environment:

```bash
dotnet user-secrets set "Jwt:Key" "<generate-a-strong-32+-char-secret>"
```

`ClockSkew = TimeSpan.Zero` is strict. If a real device’s clock is significantly off vs. the API host, freshly issued tokens may appear expired — usually a non-issue, but worth checking if you see immediate 401s after login.

---

## 7. Pre-flight checklist for a new dev environment

- [ ] API runs under `http` (Simulator / Emulator) or `Mobile` (physical device) profile.
- [ ] `EXPO_PUBLIC_API_URL` matches your run target (see [§1](#1-api-base-url-by-run-target)).
- [ ] `EXPO_PUBLIC_GOOGLE_WEB_CLIENT_ID` (and platform IDs as applicable) set; Expo restarted with `-c`.
- [ ] API user secrets set: `Jwt:Key`, `Authentication:Google:ClientId`, `Authentication:Google:IdTokenAudiences`, `Authentication:Facebook:AppId`, `Authentication:Facebook:AppSecret`.
- [ ] For password reset: API user secrets set for Brevo (`Brevo:ApiKey`, `Brevo:SenderEmail`) — otherwise `/api/auth/forgot-password` quietly drops the email via `NoOpEmailService`.
- [ ] Google Cloud OAuth clients have the right redirect URIs / SHA-1s.
- [ ] Facebook console redirect URI `fb<APP_ID>://authorize` registered.
- [ ] On a physical device, the API URL is reachable from the device (`curl http://<mac-ip>:5279/openapi/v1.json` from another device on the same network).

---

## Related documentation

- [EMAIL_BREVO.md](./EMAIL_BREVO.md) — Brevo HTTP API / SMTP setup for transactional email (forgot password etc.).
- [GOING_LIVE.md](./GOING_LIVE.md) — production deployment, secrets, HTTPS/CORS, mobile go-live.
- [FinanceApp-Architecture.md](./FinanceApp-Architecture.md) — overall architecture and security context.
