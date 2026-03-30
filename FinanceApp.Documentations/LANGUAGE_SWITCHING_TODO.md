# Language switching (i18n)

**Status:** Baseline **implemented** (English default; **Spanish** and **Swahili**). Optional follow-up: expand string coverage, add locales, or polish copy.

**Tracked in:** [ROADMAP_KANBAN.md](../ROADMAP_KANBAN.md) (Done). Root [README.md](../README.md) → **Localization (i18n)** under Features.

---

## Implemented

### Web (`FinanceApp.Web`)

- `FinanceApp.Localization` with `SharedResource.resx` + `es` / `sw` satellites; `IStringLocalizer<SharedResource>`.
- `AddLocalization` / `UseRequestLocalization`; culture from cookie, query string, `Accept-Language`, and signed-in user **`PreferredLanguage`** (profile).
- Major Razor surfaces localized (navigation, dashboard, expenses, income, budget, categories, accounts, transactions, notifications, monthly report, privacy, profile, language switcher, etc.).
- `NeutralResourcesLanguage` on the localization assembly for reliable neutral fallback.

### API (`FinanceApp.API`)

- Request culture aligned with **`Accept-Language`** and authenticated user preferred language where applicable (user-facing messages, validation).

### Mobile (`FinanceApp.Mobile`)

- **i18next** + locale JSON; language persisted (e.g. AsyncStorage); API requests send **`Accept-Language`**.

### Shared

- Supported locale codes: **`en`**, **`sw`**, **`es`** (`FinanceApp.Localization.SupportedLanguages`).
- User **preferred language** stored in the identity profile for consistency across Web/API.

---

## Optional follow-up (not blocking)

- Localize any remaining hardcoded strings (edge screens, validation messages, third-party copy).
- Add more locales or improve translations.
- Align `LANGUAGE_SWITCHING_TODO` historical notes with new UI copy as the app grows.

---

## Historical checklist (original plan)

The bullets below were the original backlog; they are **done** at baseline level — kept for traceability only.

<details>
<summary>Original Web / API / Mobile tasks (completed)</summary>

- .NET localization, `.resx`, `UseRequestLocalization`, `IStringLocalizer`, language switcher.
- API: culture from `Accept-Language` + user preference.
- Mobile: i18next, JSON per locale, persist choice, language in More/Profile.

</details>
