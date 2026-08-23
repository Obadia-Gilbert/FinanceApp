# Store submission: Apple App Store + Google Play

Concrete checklist for publishing `FinanceApp.Mobile`. Cross-reference [GOING_LIVE.md](./GOING_LIVE.md) for the backend the app talks to — you cannot submit without a stable production `EXPO_PUBLIC_API_URL` first.

---

## Current state (as of this doc)

No `eas.json`, no EAS project ID, no CI for mobile, `expo-dev-client` not installed. Version pinned at `1.0.0` with **no `ios.buildNumber` or `android.versionCode`** anywhere. This is a from-scratch submission — budget real time for it, not just the store review wait.

---

## Blocking items — fix before you touch App Store Connect / Play Console

Ordered by how badly rejection would set you back.

### 1. No hosted privacy policy URL
Both stores require a **publicly reachable HTTPS URL**, not just an in-app screen. `app/(tabs)/privacy.tsx` exists but nothing serves it on the web. **Fix:** stand up `https://app.yourdomain.com/privacy` (a public MVC view, no auth) once [GOING_LIVE.md](./GOING_LIVE.md) is deployed, and use that URL in both consoles.

### 2. No Terms of Service document
`app/(auth)/register.tsx` renders a "Terms of Service" link that **routes to the privacy screen** — there's no actual ToS content anywhere in the repo. Write one (it can be short — an EULA covering acceptable use, subscription terms, and liability) and host it at `/terms` alongside privacy. Point the register screen link at it.

### 3. Dead "Contact Sales" button on the paywall
`app/(tabs)/subscription.tsx` renders the Premium tier's purchase control as a `TouchableOpacity` with **no `onPress` handler**. A non-functional control on a paywall is a guaranteed Guideline 2.1 rejection. Either wire it to the real Premium IAP flow (the SKUs already exist in `src/iap/config.ts`) or remove the tier from the paywall until it's ready.

### 4. Paywall missing required subscription disclosures
Apple and Google both require, adjacent to the purchase button: subscription length, price per period, auto-renewal terms, and links to Terms of Use (EULA) + Privacy Policy. Currently the footer only says "Secure checkout via App Store". Add the disclosure block once items 1–2 give you something to link to.

### 5. External-purchase steering in the FAQ
`subscription.faq.web.a` currently reads: *"Yes. Subscribe on the web (Stripe on the Subscription page) or in this app via App Store / Google Play."* Directing users to a web checkout from inside the app is Guideline 3.1.1 / Play Payments Policy territory outside the narrow reader-app exemptions this app doesn't qualify for. Remove or rephrase to avoid naming the web alternative inside the app.

### 6. Placeholder contact email
`privacy.tsx` lists `privacy@financeapp.local` — `.local` is not a routable TLD and Apple's reviewer will flag it. Use a real inbox on your production domain.

### 7. Unused permission strings
The generated `Info.plist` ships `NSFaceIDUsageDescription` and `NSMicrophoneUsageDescription` boilerplate from plugin defaults — the app uses neither capability. Unused permission strings are a common, avoidable rejection trigger. Remove via plugin config or an `infoPlist` override in `app.json`.

### 8. Dev-only network config shipping to production
`app.json` → `ios.infoPlist.NSAppTransportSecurity.NSAllowsLocalNetworking: true` is a simulator convenience. Combined with `.env.example` defaulting `EXPO_PUBLIC_API_URL` to `http://127.0.0.1:5022`, there is currently **no production HTTPS API URL configured anywhere**. Set the real `.env` for production builds and drop the local-networking exemption from the production profile.

---

## `eas.json` — create this

```json
{
  "cli": { "version": ">= 13.0.0", "appVersionSource": "remote" },
  "build": {
    "development": {
      "developmentClient": true,
      "distribution": "internal",
      "env": { "EXPO_PUBLIC_API_URL": "http://127.0.0.1:5022" }
    },
    "preview": {
      "distribution": "internal",
      "env": { "EXPO_PUBLIC_API_URL": "https://api.yourdomain.com" }
    },
    "production": {
      "autoIncrement": true,
      "env": { "EXPO_PUBLIC_API_URL": "https://api.yourdomain.com" }
    }
  },
  "submit": {
    "production": {
      "ios": { "appleId": "you@example.com", "ascAppId": "<from App Store Connect>", "appleTeamId": "<team id>" },
      "android": { "serviceAccountKeyPath": "./google-service-account.json", "track": "internal" }
    }
  }
}
```

`autoIncrement: true` on the production profile solves the missing `buildNumber`/`versionCode` problem — EAS bumps it on every build so re-uploads after a rejection don't need a manual version edit.

Also install the dev client so IAP is actually testable outside Expo Go (`react-native-iap` explicitly cannot load in Expo Go):

```bash
npx expo install expo-dev-client
eas build --profile development --platform ios
```

---

## Apple App Store

### Accounts & setup
1. Enroll in the **Apple Developer Program** — $99/yr, can take 24–48h for approval.
2. Create the app in **App Store Connect**: bundle ID `com.financeapp.mobile` (already set in `app.json`), SKU, primary category (Finance).
3. Create the two subscription products in **App Store Connect → Monetization → Subscriptions**, matching `src/iap/config.ts`: `com.financeapp.mobile.pro.monthly`, `com.financeapp.mobile.premium.monthly`. Each needs a localized display name, price tier, and a review screenshot of the paywall.

### App Privacy questionnaire
The app collects no analytics/tracking (`privacy.tsx` explicitly disclaims it, and there's no Sentry/Firebase/Amplitude/ATT in the codebase — this is accurate, not aspirational). Declare:
- **Contact Info** (email, name) — linked to user, used for app functionality
- **Financial Info** (transactions, purchase history) — linked to user, used for app functionality
- **User Content** (photos/receipts) — linked to user
- **Identifiers** (user ID) — linked to user
- Everything: **"Not used for tracking"**

### Assets needed
- Icon: `icon.png` is already **1024×1024, no alpha — compliant**.
- Screenshots: 6.7" and 6.5" iPhone (required), iPad if `supportsTablet` stays `true` (it does). None exist yet — capture from the simulator once the design sweep lands, since the current screens are what reviewers will see.
- `ITSAppUsesNonExemptEncryption`: set explicitly in `app.json` to skip the export-compliance question on every TestFlight build.

### Common rejection risks specific to this app
- **Guideline 5.1.1(v) account deletion** — already implemented (`app/(tabs)/profile.tsx`, backed by `IAccountDeletionService`). Verify it end-to-end on a real device before submitting; this is the one Apple actually tests by hand.
- **Guideline 2.1 (dead controls)** — item 3 above.
- **Guideline 3.1.1 (external purchase links)** — item 5 above.
- Sign-in options: the app offers Google/Facebook — Apple requires **Sign in with Apple** as an equivalent option once any third-party login is offered. Check whether it's implemented; if not, this is a near-certain rejection and needs its own scoped fix.

### TestFlight → Review
```bash
eas build --platform ios --profile production
eas submit --platform ios --profile production
```
Internal TestFlight testing is immediate; external TestFlight and the App Store review both require the Apple review pass (typically 24–48h in 2026, can be longer).

---

## Google Play

### Accounts & setup
1. **Google Play Console** — $25 one-time.
2. Create the app: package `com.financeapp.mobile` (already set), Finance category.
3. Create the subscription products under **Monetization → Subscriptions**: `pro_monthly`, `premium_monthly` (matching the Android defaults in `src/iap/config.ts`).
4. Generate a **service account** for server-side receipt verification (`SubscriptionBilling:Google:ServiceAccountJsonPath` on the API) and for EAS submit — grant it Play Console API access.

### Data safety section
Same substance as Apple's App Privacy: financial data collected and linked to the user, no data sold, no tracking. Google's account-deletion policy additionally requires a **web-reachable deletion path**, not just in-app — the public `/profile/delete-account` page from the blockers list above satisfies this; make sure it's linked from the Play Console Data Safety form.

### Assets needed
- `adaptive-icon.png` is present at the correct 1024×1024, but **verify the safe zone**: Android crops adaptive icons to a circle/squircle, leaving ~66dp of a 108dp canvas visible. A full-bleed logo will get its edges clipped — check this specifically, since `icon.png` and `adaptive-icon.png` are currently byte-different at the same dimensions (suggesting someone already tried to compensate, but it hasn't been visually verified).
- Feature graphic (1024×500), phone screenshots, short + full description. None exist yet.
- Content rating questionnaire (Finance apps are typically "Everyone" but still gated by IARC).

### Track strategy
Ship to **Internal testing** first (near-instant), then **Closed testing** with a small group for at least the length of Google's mandatory closed-testing period for new personal developer accounts, then **Production**. Do not skip straight to production on a first submission — new developer accounts are held to a longer testing requirement before Play allows a first production release.

```bash
eas build --platform android --profile production
eas submit --platform android --profile production   # track: internal, per eas.json
```

---

## Splash / icon fixes

- `splash` in `app.json` points at `./assets/logo.png`, which is **216×216** — visibly soft when scaled to a modern phone's full-bleed splash. `assets/splash-icon.png` (400×400) already exists in the repo but is **wired to nothing**. Point the splash config at it, or supply a properly sized asset (Expo's SDK 54 splash system wants roughly 1024×1024 for the centered-icon style this app uses).
- No dark-mode/tinted app icon variants (iOS 18+ supports these). Not required, worth doing once the design sweep produces a real icon.

---

## Post-launch

- Version bumps: with `autoIncrement: true`, only `version` in `app.json` needs a manual bump for a user-visible release (e.g. `1.0.0` → `1.1.0`); build numbers take care of themselves.
- Both stores' review times fluctuate — budget 2–5 business days end-to-end for a first submission, faster for updates.
- Set up `cd-mobile.yml` (see the CI/CD plan) once the first manual submission succeeds, so subsequent releases are `git tag mobile-v1.1.0 && git push --tags` instead of a local `eas submit`.

## Related documentation

- [GOING_LIVE.md](./GOING_LIVE.md) — backend hosting, must be live first
- [SUBSCRIPTIONS_IAP.md](./SUBSCRIPTIONS_IAP.md) — IAP product IDs, webhooks, sandbox testing
- [MOBILE_AUTH.md](./MOBILE_AUTH.md) — OAuth redirect URIs, API base URL per target
