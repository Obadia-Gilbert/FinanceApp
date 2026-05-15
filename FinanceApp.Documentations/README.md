# FinanceApp Documentation

Project documentation for FinanceApp.

---

## Documents

| Document | Purpose |
|----------|---------|
| [FinanceApp-Architecture.md](./FinanceApp-Architecture.md) | Main architecture doc: current status, target state, domain model, API design, security |
| [Current-State.md](./Current-State.md) | What is implemented today (API, mobile, localization, tests) |
| [AI_IMPLEMENTATION_LIST.md](./AI_IMPLEMENTATION_LIST.md) | Prioritized AI features: insights, categorization, receipt parsing, NL entry, chat, forecasting; implementation notes and best practices |
| [LANGUAGE_SWITCHING_TODO.md](./LANGUAGE_SWITCHING_TODO.md) | i18n: baseline implemented (en / es / sw); optional follow-up tasks |
| [GOING_LIVE.md](./GOING_LIVE.md) | Production deployment: VPS vs Azure, DB strategy, HTTPS/CORS, secrets, jobs, mobile go-live |
| [MOBILE_AUTH.md](./MOBILE_AUTH.md) | Mobile auth setup + troubleshooting: API URL per target, Google/Facebook OAuth clients, API user secrets |
| [EMAIL_BREVO.md](./EMAIL_BREVO.md) | Brevo (ex-Sendinblue) transactional email setup: HTTP API vs SMTP relay, sender verification, user-secrets config |

---

## Quick Links

- **Current status (concise):** [Current-State.md](./Current-State.md)
- **Architecture (detailed + history):** [Architecture § Current Status & Direction](./FinanceApp-Architecture.md#current-status--direction)
- **Where we are heading:** [Architecture § Migration Path](./FinanceApp-Architecture.md#23-migration-path-where-we-are-heading)
- **AI features (planned):** [AI Implementation List](./AI_IMPLEMENTATION_LIST.md)
