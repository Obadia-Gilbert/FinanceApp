# AI Features — Implementation List

A prioritized, actionable list of AI capabilities to add to FinanceApp. Use this for planning and implementation; update status as items are completed.

---

## Overview

| Priority | Feature | Effort | Impact | Status |
|----------|---------|--------|--------|--------|
| P1 | Smart insights & summaries | Medium | High | Not started |
| P1 | Smart categorization | Low–Medium | High | Not started |
| P2 | Receipt / document understanding | Medium | High | Not started |
| P2 | Natural language expense entry | Medium | High | Not started |
| P3 | Conversational “ask your finances” | High | High | Not started |
| P3 | Predictions & forecasting (logic first) | Low then Medium | Medium | Not started |

---

## P1 — Quick wins

### 1. Smart insights & summaries

**Description**
- Weekly or monthly summary in plain language: e.g. “This month you spent $X on dining (up 15% vs last month), stayed under budget on groceries, and saved $Y.”
- Optional anomaly line: “Unusual $500 expense in Shopping on the 12th — was this expected?”

**Implementation**
- Backend: scheduled job (e.g. daily) that uses existing report/aggregation APIs, then calls an LLM with structured data to generate 2–3 sentences.
- Store summary (and optional anomaly) per user/month; expose via API (e.g. `GET /api/ai/insights/summary?month=...`).
- Mobile/Web: show in Dashboard or a dedicated “Insights” section; optional push or email.

**Dependencies**
- Existing: `IMonthlyReportService`, dashboard aggregation, category spend.
- New: LLM provider (e.g. OpenAI / Azure OpenAI), config for API key and model.

**Acceptance**
- [ ] Summary generated for current month (and optionally previous).
- [ ] Anomaly detection uses simple rules (e.g. >2σ or >X% vs average) then LLM for wording.
- [ ] Feature can be disabled per user or tier (cost control).

---

### 2. Smart categorization

**Description**
- When creating an expense/transaction, suggest category (and optionally account) from merchant name, amount, and date.
- One-tap accept: “Did you mean **Restaurants**?”

**Implementation**
- Backend: new endpoint e.g. `POST /api/ai/categorize` with `{ merchant, amount, date }`; returns `{ categoryId, confidence }`.
- Option A: prompt LLM with app category list + few-shot examples.
- Option B: small fine-tuned or embedding-based model; optionally learn from user corrections (store merchant → category, use as first lookup).
- Mobile/Web: on expense/transaction form, call suggest when merchant/description changes; show suggestion chip or dropdown.

**Dependencies**
- Category list (existing); optional: correction feedback storage for learning.

**Acceptance**
- [ ] Suggestion returned in &lt;1s for typical payload.
- [ ] User can accept or ignore; accepting pre-fills category (and optional account).
- [ ] Optional: user corrections stored for future improvement (with privacy note).

---

## P2 — High impact

### 3. Receipt / document understanding

**Description**
- User uploads a receipt or invoice; app extracts amount, date, merchant and pre-fills the expense/income form.
- Aligns with existing supporting-document flow for income.

**Implementation**
- Backend: endpoint e.g. `POST /api/ai/parse-receipt` (image file); call vision API (e.g. GPT-4V or document-specific model); return structured fields.
- App: pre-fill form; user confirms or edits before save; optional “attach receipt” kept as before.

**Dependencies**
- Vision-capable API; file upload size/type limits; existing document attachment flow.

**Acceptance**
- [ ] Image upload returns amount, date, merchant (and optional category hint).
- [ ] Form pre-filled; user must confirm before create/update.
- [ ] Clear privacy note: “Receipt is processed to fill the form; not stored for model training.”

---

### 4. Natural language expense entry

**Description**
- User types e.g. “$45 lunch at Chipotle yesterday” → app creates expense (and linked transaction if account chosen).

**Implementation**
- Backend: endpoint e.g. `POST /api/ai/parse-expense` with `{ text, userId }`; LLM extracts amount, category/merchant, date; service calls existing create-expense (and transaction) API.
- App: optional “Quick add” input; show parsed result for confirmation before save.

**Dependencies**
- Existing expense/transaction creation APIs; category list for disambiguation.

**Acceptance**
- [ ] Parsed result shown for confirmation; no auto-save without user action.
- [ ] Handles relative dates (“yesterday”, “last Tuesday”) and common currencies.
- [ ] Graceful fallback when parsing fails (e.g. “Couldn’t parse — use form instead”).

---

## P3 — Engagement and scale

### 5. Conversational “ask your finances”

**Description**
- User asks e.g. “How much did I spend on groceries last month?” or “What’s my total balance?”; answer in natural language using existing data.

**Implementation**
- Backend: endpoint e.g. `POST /api/ai/ask` with `{ message }`; optional LLM step to map intent to API calls (or fixed intents); call dashboard/expenses/accounts/reports APIs; LLM formats reply.
- Mobile/Web: chat-style UI; history optional (store last N exchanges or session-only).

**Dependencies**
- Auth; read-only access to dashboard, expenses, accounts, reports APIs; rate limiting and optional tier gating.

**Acceptance**
- [ ] Answers only from user’s own data; no cross-user or training on content.
- [ ] Rate limit per user (e.g. N requests/minute); optional premium-only or cap.
- [ ] Clear disclaimer: “Answers are based on your data and may be wrong; check key numbers in the app.”

---

### 6. Predictions & forecasting

**Description**
- “At this rate you’ll exceed your dining budget by $X by month end.”
- “Based on recurring items, estimated balance in 2 weeks: $Y.”
- Can start as pure logic; add LLM later only for natural-language phrasing.

**Implementation**
- Phase 1: backend logic using existing budget, recurring, and history; expose as structured data (e.g. `GET /api/insights/forecast`).
- Phase 2: optional LLM step to turn numbers into one short sentence for the UI.

**Dependencies**
- Existing budget and recurring services; transaction/balance logic.

**Acceptance**
- [ ] Forecast uses only user’s data and documented assumptions.
- [ ] Shown as “estimate” or “projection” with link to actuals.
- [ ] Optional: natural-language one-liner (Phase 2).

---

## Best practices (implementation)

### Where AI runs
- **Backend preferred** for all LLM and vision calls: keeps keys and prompts server-side; easier to cache, rate-limit, and audit.
- **Mobile/Web**: only call backend AI endpoints; no API keys in client.

### Cost and limits
- Put API keys in configuration (e.g. `appsettings`, env vars); never in repo.
- Rate limit AI endpoints per user (and optionally by subscription tier).
- Consider making advanced AI features premium or capped (e.g. N summaries/month on free tier).
- Cache where possible (e.g. same summary request within TTL).

### Privacy and compliance
- Prefer providers and options that **do not train on your data** (e.g. Azure OpenAI opt-out, OpenAI API policy); document in privacy policy.
- Make AI features **opt-in or clearly disclosed** where they process personal/financial data.
- Retention: do not store raw user messages or generated text longer than needed (e.g. last summary only, or session-only for chat).

### Security
- All AI endpoints require authentication and must scope data by `userId` (from token/session).
- Validate and sanitize LLM outputs before using in DB or UI (e.g. category IDs from allow-list only).

### Observability
- Log usage (e.g. endpoint, user, token count) for cost and abuse monitoring.
- Monitor latency and errors; consider fallbacks (e.g. “Insights temporarily unavailable”).

---

## References

- [FinanceApp-Architecture.md](./FinanceApp-Architecture.md) — API and security context.
- [MOBILE_READINESS.md](../MOBILE_READINESS.md) — API surface for mobile (and future AI clients).
- [DEPLOYMENT_READINESS.md](../DEPLOYMENT_READINESS.md) — Config and secrets handling.

---

*Last updated: March 2025. Update status and checkboxes as items are implemented.*
