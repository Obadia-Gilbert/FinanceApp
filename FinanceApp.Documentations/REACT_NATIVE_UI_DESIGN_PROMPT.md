# React Native FinanceApp — UI/UX Design Brief for Specialized Agent

Use this prompt with a UI/UX–focused AI agent to design a top-notch, production-ready mobile experience for the FinanceApp React Native client. The agent should act as a senior product manager, UI/UX engineer, and visual designer.

---

## 1. Product context

**FinanceApp** is a personal finance management platform. The existing product is a .NET 10 web app (MVC + REST API) with Clean Architecture. You are designing the **mobile app (React Native)** that will consume the same REST API. The app must feel like a first-class native companion: fast, trustworthy, and a joy to use on the go.

**Target users:** Individuals who want to track spending, set budgets, and see where their money goes. They may use multiple currencies (e.g. TZS, USD). They value clarity, control, and minimal friction—not gamification or social features.

**Value proposition (from existing landing):** “Take control of your money — Track expenses, set budgets, and see where your money goes. One simple app for your personal finance—free to start.”

**Tone:** Professional, calm, trustworthy. Avoid playful or casual finance UX. Prioritize clarity and scannability over decoration.

---

## 2. Technical constraints and stack

- **Platform:** React Native (Expo or bare; specify recommendation). Target **iOS and Android**.
- **Backend:** Existing REST API at `api/` with JWT + refresh tokens. Endpoints: Auth, Dashboard, Expenses, Income, Categories, Accounts, Transactions, Recurring, Budgets, Reports, Subscription, Profile, Notifications, SupportingDocuments.
- **Auth:** Login/Register (email + password); optional social (Google, Facebook, Twitter) if the API supports it in mobile context.
- **State / data:** Consider React Query (TanStack Query) or SWR for server state; local state as needed. Offline-first or at least “graceful offline” (cached data, clear error states) is desirable for key flows (e.g. view dashboard, add expense).
- **Charts:** Use a React Native–friendly charting library (e.g. react-native-chart-kit, victory-native, or react-native-gifted-charts) for dashboard KPIs and trends.
- **Navigation:** Bottom tabs for primary sections; stack navigators for drill-down. Specify exact tab set and stack structure.
- **Accessibility:** Support Dynamic Type / font scaling, sufficient contrast (WCAG 2.1 AA), and screen-reader-friendly labels. Design for one-handed use and thumb-friendly primary actions.

---

## 3. Feature set (from existing web app)

Design screens and flows for these areas. Prioritize **mobile-first** patterns (e.g. FAB or bottom sheet for “Add expense”, not a full-page form as first step).

| Area | Purpose | Key actions |
|------|---------|-------------|
| **Dashboard** | At-a-glance KPIs, alerts, recent activity, cash flow | Total balance, monthly income/expense, active budgets, trend chart, recent transactions, budget alerts |
| **Expenses** | List and manage expenses | List (filter/sort), add, edit, delete, optional receipt attach |
| **Income** | List and manage income entries | Same pattern as expenses |
| **Categories** | Manage category list (name, icon, color) | CRUD categories; used by expenses/income/budgets |
| **Accounts** | Bank/checking/savings/credit/cash | CRUD accounts; balance display; link to transactions |
| **Transactions** | Ledger (debits/credits) | List, create (debit/credit/transfer), edit, delete |
| **Recurring** | Recurring expense/income rules | List, create, edit, delete |
| **Budget** | Monthly budget and per-category budgets | Set global and per-category limits; progress bars; alerts |
| **Monthly report** | Summary and export | View summary; optional export (if API supports mobile export) |
| **Subscription** | Plan (Free / Pro / Enterprise) | View current plan; upgrade path if applicable |
| **Profile** | User info and settings | Name, email, photo, theme (light/dark), currency preference, sign out |
| **Privacy** | Privacy policy | Read-only; link or in-app web view |
| **Notifications** | In-app notification list | List, mark read (API exists) |

**Admin-only (optional for first release):** Manage users, user feedback — can be hidden or gated by role.

---

## 4. Design system (align with existing web)

The web app uses a defined token set. Reuse these conceptually so the mobile app feels part of the same product.

**Light theme**

- **Brand:** `#0d6efd` (primary); hover/darker `#0b5ed7`.
- **Text:** Primary `#111827`, body `#374151`, muted `#6B7280`, subtle `#9CA3AF`.
- **Backgrounds:** Default `#ffffff`, alt `#F9FAFB`, hover `#F3F4F6`.
- **Borders:** `#E5E7EB`; focus `#93C5FD`.
- **Radii:** sm 6px, md 8px, lg 12px, xl 16px.
- **Shadows:** Subtle elevation (e.g. 1–2px blur, low opacity) for cards and modals.

**Dark theme**

- **Backgrounds:** `#111111` (alt), `#1a1a1a` (surface), hover `#2a2a2a`.
- **Text:** Primary `#F9FAFB`, body `#D1D5DB`, muted `#9CA3AF`, subtle `#6B7280`.
- **Borders:** `#2E2E2E`.
- **Accent:** Keep blue accent (e.g. `#3B82F6` / `#60A5FA`) for links and primary actions so the app stays recognizable.

**Typography**

- **Font:** Inter (web uses Inter from Google Fonts). Use Inter or a close system alternative (e.g. SF Pro on iOS, Inter or Roboto on Android) with clear hierarchy: one display/title style, one body, one caption/label. Support Dynamic Type / font scaling.
- **Sizes:** Define a small set (e.g. 12, 14, 16, 20, 24) and map to roles (caption, body, bodyLarge, title, headline).

**Components**

- **Cards:** Rounded (e.g. 12px), subtle shadow, padding 16–20. In dark mode, use surface background and subtle border.
- **Buttons:** Primary (filled brand color), secondary (outline), danger (e.g. red for delete). Minimum touch target 44pt.
- **Inputs:** Rounded, clear focus state, placeholder and label; error state for validation.
- **Lists:** Row-based with optional swipe actions (e.g. edit/delete). Use dividers or subtle borders; avoid visual noise.

**Icons**

- Web uses **Bootstrap Icons** (e.g. bi-cash, bi-wallet2, bi-pie-chart). Use a single icon set in React Native (e.g. Ionicons, MaterialCommunityIcons, or a custom set that matches) and document the mapping (e.g. Dashboard = speedometer, Expenses = cash, Income = cash-stack, Categories = tags, Accounts = bank, Transactions = swap-horizontal, Recurring = repeat, Budget = wallet, Report = document-text, Subscription = star, Profile = person-circle, Privacy = shield-lock).

---

## 5. UX principles and priorities

1. **Speed to log:** Adding an expense should be 1–2 taps from the home context (e.g. FAB or prominent “Add” on Expenses tab). Prefer bottom sheet or short form over full-screen wizard where possible.
2. **Glanceable dashboard:** Key numbers (balance, this month’s spend, budget status) visible without scrolling. Use cards and clear typography hierarchy. Alerts (over budget, category warning) should be visible but not alarming.
3. **Consistency with web:** Users who use both web and mobile should recognize the same concepts (categories, accounts, budget, “this month,” currency). Terminology and mental model must match the existing app.
4. **One-handed use:** Primary actions (add expense, main nav) reachable by thumb. Avoid critical actions at the top of long screens.
5. **Progressive disclosure:** Don’t overwhelm. Lists first; filters and advanced options in secondary screens or sheets.
6. **Empty and error states:** Every list and chart has an empty state (e.g. “No expenses yet — tap + to add”). Network errors and auth failures have clear messages and retry/sign-in actions.
7. **Theme:** Support light and dark mode. Respect system preference by default with an in-app override (e.g. in Profile). Persist preference (e.g. async storage) and apply on launch.

---

## 6. Deliverables to produce

Ask the agent to produce the following in a structured format (Figma, markdown, or both):

1. **Design tokens**  
   A single source of truth (JSON or code) for colors, spacing, radii, typography, and shadows for light and dark themes.

2. **Component library**  
   Core components: Button (primary, secondary, danger, ghost), Card, Input, Label, ListRow, Badge, Chip, BottomSheet, Modal, Toast/Snackbar, Skeleton loader. States: default, focus, disabled, error, loading.

3. **Navigation and app structure**  
   - Tab bar: which 4–5 tabs (e.g. Dashboard, Expenses, Budget, More or Reports, Profile).  
   - Stack structure per tab (e.g. Expenses → List → Detail → Edit).  
   - Where “Add expense” lives (tab bar, FAB, dashboard CTA).

4. **Screen-by-screen specs**  
   For each main screen: layout (wireframe or high-fidelity), key elements, copy for titles and empty states, and primary/secondary actions. At minimum:  
   - Onboarding / Welcome (optional), Login, Register  
   - Dashboard (KPIs, alerts, chart, recent activity)  
   - Expenses list and Add/Edit expense  
   - Categories list and Add/Edit category  
   - Budget overview and set/edit budget  
   - Profile (settings, theme, sign out)  
   - One “secondary” flow (e.g. Accounts or Transactions) to establish pattern

5. **Key user flows**  
   - First launch → Login or Register → Dashboard  
   - Add first expense from dashboard vs from Expenses tab  
   - View budget status and respond to over-budget alert  
   - Switch theme in Profile

6. **Accessibility and responsiveness**  
   - Minimum touch targets (44pt).  
   - Contrast ratios for text and controls.  
   - How charts and tables adapt to small screens and font scaling.  
   - Optional: short note on screen reader order and labels.

7. **Icon and asset list**  
   - List of required icons (with names/sources) and any illustrations (e.g. empty state art).

---

## 7. Out of scope for this design phase

- Backend or API changes.  
- Exact React Native component implementation (only design and specs).  
- Copy for full legal pages (Privacy, Terms); only placement and “View policy” entry points.  
- Localization and RTL (can be added later; design for LTR and one language first).

---

## 8. Success criteria

The resulting design should:

- Feel **native** and **premium**, not like a wrapped website.  
- Be **immediately usable** by a developer to implement in React Native without guessing layout or tokens.  
- **Match the existing product** in terminology, structure, and look (colors, typography, dark mode).  
- **Prioritize** the most frequent actions (log expense, check budget, view dashboard) and keep secondary features discoverable but not in the way.

---

## 9. One-sentence brief for the agent

**“Design a complete, production-ready UI/UX for a React Native mobile app that is the companion to an existing personal finance web app: define design tokens (light/dark), a component library, navigation structure, and detailed screen specs and flows for Dashboard, Expenses, Categories, Budget, Profile, and core flows, aligned with the existing web design system (Inter, brand blue #0d6efd, clear hierarchy) and optimized for fast expense logging, glanceable KPIs, and one-handed use.”**

You can paste the full document above into the agent for full context, or use the one-sentence brief plus sections 2–5 as needed.
