# FinanceApp

> A full-stack personal finance management platform built with **.NET 10**, **Clean Architecture**, and a **triple-surface delivery model** — an ASP.NET Core MVC web app, a JWT REST API, and a **React Native (Expo)** mobile client that share the same domain and services.

![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)
![ASP.NET Core](https://img.shields.io/badge/ASP.NET_Core-MVC_%2B_API-blue?logo=microsoft)
![EF Core](https://img.shields.io/badge/EF_Core-10.0-orange)
![Bootstrap](https://img.shields.io/badge/Bootstrap-5.3-7952B3?logo=bootstrap)
![Expo](https://img.shields.io/badge/Expo-RN-000020?logo=expo)
![License](https://img.shields.io/badge/license-MIT-green)

---

## Table of Contents

1. [Overview](#overview)
2. [Architecture](#architecture)
3. [Tech Stack](#tech-stack)
4. [Features](#features)
5. [Project Structure](#project-structure)
6. [Prerequisites](#prerequisites)
7. [Getting Started](#getting-started)
8. [Configuration](#configuration)
9. [Running the Application](#running-the-application)
10. [Mobile app (Expo)](#mobile-app-expo)
11. [REST API](#rest-api)
12. [Testing](#testing)
13. [Database Migrations](#database-migrations)
14. [Roadmap & docs](#roadmap--docs)
15. [Author](#author)

---

## Overview

FinanceApp is a personal finance management system that gives individuals visibility over spending, income, accounts, budgets, transactions, and recurring items — with a responsive web UI, a stateless API for programmatic and mobile access, and an **Expo** mobile app that consumes the same API.

The platform is designed to evolve from a **personal-use tool** into a **multi-tenant SaaS product**, so architectural decisions prioritise separation of concerns, testability, and scalability.

**Key design goals:**

- Per-user data isolation via ASP.NET Core Identity
- Clean Architecture — domain logic stays in Application/Domain; infrastructure is swappable
- **Web (cookies + Identity UI)**, **API (JWT + refresh tokens)**, and **Mobile** as clients over shared services
- Dark mode (web), theme context (mobile), responsive layout

---

## Architecture

The solution follows **Clean Architecture** (Onion / Ports & Adapters), with dependency rules pointing strictly inward:

```
┌─────────────────────────────────────────────────────────────────────────┐
│                         Presentation / Clients                           │
│                                                                          │
│   FinanceApp.Web (MVC)     FinanceApp.API (REST)    FinanceApp.Mobile  │
│   ─ Razor + Identity       ─ JWT + OpenAPI          ─ Expo Router        │
│   ─ Cookie auth            ─ Refresh tokens         ─ TanStack Query     │
└────────────────┬───────────────────────┬──────────────────┬──────────────┘
                 │                       │                  │
┌────────────────▼───────────────────────▼──────────────────▼──────────────┐
│                        Infrastructure Layer                                │
│                                                                            │
│   FinanceApp.Infrastructure                                                │
│   ─ EF Core / FinanceDbContext    ─ Generic IRepository<T>                 │
│   ─ ASP.NET Core Identity         ─ Email, file-backed uploads             │
│   ─ Refresh tokens                ─ RecurringTransactionJob (hosted svc)   │
└────────────────────────────────┬───────────────────────────────────────────┘
                                 │
┌────────────────────────────────▼───────────────────────────────────────────┐
│                         Application Layer                                    │
│                                                                              │
│   FinanceApp.Application                                                     │
│   ─ Services (expenses, categories, budgets, accounts, transactions,       │
│      income, recurring, notifications, reports, feedback, documents)      │
│   ─ Interfaces + DTOs / shared result types                                  │
└────────────────────────────────┬─────────────────────────────────────────────┘
                                 │
┌────────────────────────────────▼─────────────────────────────────────────────┐
│                           Domain Layer                                         │
│                                                                                │
│   FinanceApp.Domain                                                            │
│   ─ Entities (Expense, Income, Budget, Account, Transaction, …)               │
│   ─ Enums (Currency, SubscriptionPlan, NotificationType, …)                │
└────────────────────────────────────────────────────────────────────────────────┘
```

**Dependency rule:** each layer only depends on the layer directly below it. The Domain project has no references to infrastructure or UI frameworks.

---

## Tech Stack

| Layer | Technology | Version |
|---|---|---|
| Runtime | .NET | 10.0 |
| Web UI | ASP.NET Core MVC | 10.0 |
| REST API | ASP.NET Core Web API | 10.0 |
| ORM | Entity Framework Core | 10.0 |
| Database | SQL Server (LocalDB / Docker) | — |
| Auth (Web) | ASP.NET Core Identity + Cookie | 10.0 |
| Auth (API) | JWT Bearer + Refresh Tokens | 10.0 |
| Social login | Google / Facebook / Twitter OAuth2 | 10.0 |
| UI (Web) | Bootstrap | 5.3 |
| Icons | Bootstrap Icons | 1.13 |
| Charts (Web) | Chart.js | CDN |
| Tables | DataTables.js | 1.13 |
| Excel export | ClosedXML | 0.105 |
| Fonts | Inter (Google Fonts) | — |
| Email | SMTP via `IEmailService` | — |
| Mobile | Expo, React Native, Expo Router | see `FinanceApp.Mobile/package.json` |
| Mobile state | TanStack Query | 5.x |
| Localization | `FinanceApp.Localization` — RESX (`SharedResource`), `IStringLocalizer<SharedResource>` | en + es, sw |

---

## Features

### Expense & income

- **Expenses:** CRUD with description, amount, currency, date, category; search, sort, pagination; **Excel export** (Web + API).
- **Income:** CRUD with category and optional account; aligned with dashboard and reports.

### Supporting documents

- Attach receipts, invoices, or PDFs to expenses or transactions.
- Preview in browser (images inline, PDFs in iframe); AJAX upload/delete.
- Files under `wwwroot/uploads/documents/{UserId}/` (web); API uses the same path pattern via `IWebHostEnvironment`.

### Categories & budgets

- User-defined categories (name, icon, colour) with **category type** (expense / income / both).
- **Global monthly budget** and **per-category budgets** with progress and alerts.
- Dashboard doughnut chart and budget status indicators.

### Accounts & transactions

- Account types: Checking, Savings, Credit Card, Cash, Investment (and related domain enums).
- **Transactions:** income/expense lines and **transfers** between accounts; optional supporting documents.

### Recurring transactions

- **Recurring templates** (frequency, next run); **background job** (`RecurringTransactionJob`) processes due items when the API host runs.

### Dashboard & analytics (Web)

- KPI cards, month-over-month hints, 30-day trend chart, category breakdown, recent expenses, budget progress, account balances.

### Reports & sharing (Web + API)

- **Monthly report** by month: totals, by category, top expenses; **download as HTML** (web).
- **Shareable report links** via `SharedReport` (web flows).

### Notifications

- In-app **notifications** (e.g. budget/category alerts); list, unread count, mark read (Web + API + Mobile).

### Profile & identity

- Profile with extended fields (e.g. phone, country); JWT + refresh for API/mobile.
- **Landing** experience for unauthenticated users; authenticated app uses fixed shell (navbar + sidebar).
- **Social login** (Google, Facebook, Twitter) when configured in `appsettings`.

### Subscription

- Plans: **Free**, **Pro**, **Premium** (`SubscriptionPlan` enum); subscription UI and API surface.

### Feedback

- User feedback submission and status (web + API).

### REST API

- JWT auth, refresh rotation, **OpenAPI** document at `/openapi/v1.json` (built-in .NET 10; optional Swagger UI package can be added later).
- CORS enabled for development-style clients (tighten for production).

### Administration (Web)

- Admin area: user list, role management (`Admin`, `User`).

### Mobile app (`FinanceApp.Mobile`)

- **Auth:** login, register, forgot-password flow, JWT + refresh in **SecureStore**.
- **Tabs:** Dashboard, Expenses, Budget, More (profile, income, accounts, transactions, categories, monthly report, notifications, subscription, privacy, feedback, sign out).
- **Screens:** expense/income/account/category detail & create, transactions (transfer/create), recurring templates, reports.
- **Theme:** light/dark via `ThemeContext`.

### Localization (i18n)

- **Shared library:** `FinanceApp.Localization` holds `SharedResource.resx` (default English) plus `SharedResource.es.resx` / `SharedResource.sw.resx` for Spanish and Swahili. Razor and API code use `IStringLocalizer<SharedResource>` (marker type `SharedResource.cs`). The assembly sets neutral fallback via `NeutralResourcesLanguage` so strings resolve reliably.
- **Web (`FinanceApp.Web`):** `AddLocalization` / `UseRequestLocalization`; culture from cookie, query string (`culture` / `ui-culture`), `Accept-Language`, and signed-in users’ **`PreferredLanguage`** (profile). Major UI surfaces use localized strings: layout, sidebar, dashboard, expenses, income, budget, categories, accounts (including localized account type labels), transactions, notifications, **monthly report**, **privacy policy**, profile, language switcher, and common controls.
- **API (`FinanceApp.API`):** Request culture aligned with `Accept-Language` and the authenticated user’s preferred language where applicable (validation messages and user-facing strings).
- **Mobile:** **i18next** + translation JSON; selected language persisted (e.g. AsyncStorage); API calls send **`Accept-Language`** so responses can follow the app locale.

---

## Project Structure

```
FinanceApp/
├── FinanceApp.Domain/                  # Zero-dependency domain layer
│   ├── Common/                         # BaseEntity (timestamps)
│   ├── Entities/                       # Expense, Income, Category, Budget,
│   │                                   # CategoryBudget, Account, Transaction,
│   │                                   # SupportingDocument, Notification,
│   │                                   # SharedReport, RecurringTemplate,
│   │                                   # UserFeedback, RefreshToken, …
│   └── Enums/                          # Currency, AccountType, TransactionType,
│                                       # SubscriptionPlan, NotificationType, …
│
├── FinanceApp.Application/             # Use cases / business logic
│   ├── Interfaces/                   # IExpenseService, … + Services/
│   ├── Services/                     # ExpenseService, MonthlyReportService,
│   │                                 # NotificationService, RecurringTemplateService, …
│   ├── DTOs/                         # Internal contracts
│   └── Common/                       # PagedResult<T>, helpers
│
├── FinanceApp.Localization/          # Shared UI strings (RESX)
│   └── SharedResource*.resx          # en (default) + es, sw satellites
│
├── FinanceApp.Infrastructure/        # EF, Identity, adapters
│   ├── Persistence/
│   │   ├── FinanceDbContext.cs
│   │   └── Migrations/
│   ├── Identity/                     # ApplicationUser, RoleSeeder
│   ├── Repositories/                 # Generic Repository<T>
│   └── Services/                     # EmailService, UserService, ExpenseQueryService,
│                                     # RecurringTransactionJob, …
│
├── FinanceApp.Web/                     # MVC + Razor + Identity cookies
│   ├── Controllers/                  # Landing, Home, Expense, Income, Category,
│   │                               # Budget, Account, Transaction, Profile,
│   │                               # Report, Notification, Subscription,
│   │                               # SupportingDocument, Feedback, Recurring, …
│   ├── Areas/                        # Admin, Identity UI
│   ├── Views/
│   ├── Models/                       # ViewModels
│   └── wwwroot/                      # css, js, uploads (gitignored)
│
├── FinanceApp.API/                   # REST + JWT + OpenAPI
│   ├── Controllers/                  # Auth, Expenses, Categories, Budgets,
│   │                               # Accounts, Transactions, Income,
│   │                               # Dashboard, Profile, Subscription,
│   │                               # SupportingDocuments, Notifications,
│   │                               # Reports, RecurringTemplates, Feedback
│   ├── DTOs/
│   └── Program.cs                    # JWT, CORS, OpenAPI, EnsureCreated (Testing: SQLite)
│
├── FinanceApp.Tests/                 # Unit tests (xUnit, Moq)
├── FinanceApp.API.Tests/             # Integration tests (WebApplicationFactory, SQLite)
│
├── FinanceApp.Mobile/                # Expo React Native app (not in .slnx)
│   ├── app/                          # Expo Router routes
│   ├── src/api/                      # apiFetch, feature modules
│   ├── src/context/                  # Auth, theme
│   └── README.md                     # Run on device, env, troubleshooting
│
├── FinanceApp.Documentations/        # Architecture notes, i18n status, prompts
├── DEPLOYMENT_READINESS.md           # Pre-production checklist
├── WHERE_WE_LEFT_OFF.md              # Handoff / resume notes
├── ROADMAP_KANBAN.md                 # Backlog board
└── README.md                         # This file
```

---

## Prerequisites

| Tool | Minimum Version | Notes |
|---|---|---|
| [.NET SDK](https://dotnet.microsoft.com/download) | 10.0 | `dotnet --version` |
| [SQL Server](https://www.microsoft.com/en-us/sql-server/sql-server-downloads) | 2019+ | LocalDB (Windows); Docker on macOS/Linux |
| [Node.js](https://nodejs.org/) | 18+ | For `FinanceApp.Mobile` |
| [Git](https://git-scm.com/) | any | — |
| [Docker](https://www.docker.com/) *(optional)* | 20+ | SQL Server container |

---

## Getting Started

### 1. Clone the repository

```bash
git clone https://github.com/your-username/FinanceApp.git
cd FinanceApp
```

### 2. Restore .NET dependencies

```bash
dotnet restore
```

### 3. Configure the database connection

Copy or merge example settings and set your connection string (prefer **User Secrets** or environment variables for secrets — do not commit real credentials).

Edit `FinanceApp.Web/appsettings.Development.json` (or User Secrets) so `ConnectionStrings:DefaultConnection` points at your SQL Server instance. Use the **same database** for Web and API when running both.

> **Docker (example):** run SQL Server and point the connection string at `localhost,1433` with `sa` / password as configured.

### 4. Apply database migrations

```bash
dotnet ef database update --project FinanceApp.Infrastructure --startup-project FinanceApp.Web
```

This creates tables and seeds default roles (`Admin`, `User`).

### 5. Run the web application

```bash
dotnet run --project FinanceApp.Web
```

Open the URL shown in the terminal (see `FinanceApp.Web/Properties/launchSettings.json` — e.g. `http://localhost:5279` for the default `http` profile).

### 6. Run the API

```bash
dotnet run --project FinanceApp.API
```

Default profile listens on **http://localhost:5022**. For **physical devices** or Expo, use the **Mobile** launch profile so the API binds to `0.0.0.0:5279`:

```bash
dotnet run --project FinanceApp.API --launch-profile Mobile
```

### 7. Mobile app

See [Mobile app (Expo)](#mobile-app-expo) and `FinanceApp.Mobile/README.md`.

---

## Configuration

### Web — `appsettings` / User Secrets

- `ConnectionStrings:DefaultConnection`
- `EmailSettings` — SMTP for `IEmailService` / Identity emails
- `Authentication:Google|Facebook|Twitter` — OAuth client IDs/secrets when using social login
- `AdminSeed` / role seeding — see `RoleSeeder` and `DEPLOYMENT_READINESS.md` for production

### API — `FinanceApp.API/appsettings.json`

- `ConnectionStrings:DefaultConnection` — same database as the web app for shared data
- `Jwt` — at minimum **`Jwt:Key`** (long, random secret; ≥ 32 characters), plus `Issuer`, `Audience`, `ExpirationMinutes` as configured in `Program.cs`

Example shape:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "<sql-server-connection-string>"
  },
  "Jwt": {
    "Key": "<at-least-32-character-secret>",
    "Issuer": "FinanceApp.API",
    "Audience": "FinanceApp",
    "ExpirationMinutes": 60
  }
}
```

> Store production secrets in environment variables or a vault — never commit real keys.

---

## Running the Application

| Command | What it does |
|---|---|
| `dotnet run --project FinanceApp.Web` | Start the MVC web app |
| `dotnet run --project FinanceApp.API` | Start the REST API (default port **5022**) |
| `dotnet run --project FinanceApp.API --launch-profile Mobile` | API on **0.0.0.0:5279** for LAN / Expo |
| `dotnet build` | Build all solution projects |
| `dotnet test` | Run `FinanceApp.Tests` + `FinanceApp.API.Tests` |
| `dotnet ef migrations add <Name> --project FinanceApp.Infrastructure --startup-project FinanceApp.Web` | Add EF migration |
| `dotnet ef database update --project FinanceApp.Infrastructure --startup-project FinanceApp.Web` | Apply migrations |

---

## Mobile app (Expo)

1. Install dependencies: `cd FinanceApp.Mobile && npm install`
2. Copy `.env.example` to `.env` and set `EXPO_PUBLIC_API_URL` to your machine’s LAN IP and API port (e.g. `http://192.168.1.10:5279` when using the API **Mobile** profile).
3. Start the API with `--launch-profile Mobile` so the phone can reach it.
4. Run `npx expo start` and open in Expo Go or a simulator.

Full steps, troubleshooting, and feature list: **`FinanceApp.Mobile/README.md`**.

---

## REST API

Authentication uses **JWT Bearer**; protected endpoints expect `Authorization: Bearer <token>`. Refresh via `POST /api/auth/refresh`.

### Authentication

| Method | Endpoint | Description |
|---|---|---|
| `POST` | `/api/auth/register` | Register |
| `POST` | `/api/auth/login` | Access + refresh tokens |
| `POST` | `/api/auth/refresh` | Rotate refresh token |

### Resources (all under `/api/...`, most require JWT)

| Area | Typical base path |
|---|---|
| Expenses | `/api/Expenses` |
| Categories | `/api/Categories` |
| Budgets | `/api/Budgets` |
| Accounts | `/api/Accounts` |
| Transactions | `/api/Transactions` |
| Income | `/api/Income` |
| Dashboard | `/api/Dashboard` |
| Profile | `/api/Profile` |
| Subscription | `/api/Subscription` |
| Supporting documents | `/api/SupportingDocuments` |
| Notifications | `/api/Notifications` |
| Reports | `/api/Reports` (e.g. `GET .../monthly?year=&month=`) |
| Recurring templates | `/api/recurring` |
| Feedback | `/api/Feedback` |

**OpenAPI:** with the API running, the document is at **`/openapi/v1.json`**. Use Swagger UI, Scalar, or Postman by importing that URL if you add a UI package or external tool.

---

## Testing

- **`FinanceApp.Tests`** — unit tests (e.g. expense/category services) with **xUnit** and **Moq**.
- **`FinanceApp.API.Tests`** — **integration tests** with **WebApplicationFactory** and **SQLite** when `EnvironmentName` is `Testing`.

```bash
dotnet test
```

---

## Database Migrations

EF Core migrations live in `FinanceApp.Infrastructure`. Use the web project as startup for design-time and updates:

```bash
dotnet ef migrations add YourMigrationName \
  --project FinanceApp.Infrastructure \
  --startup-project FinanceApp.Web

dotnet ef database update \
  --project FinanceApp.Infrastructure \
  --startup-project FinanceApp.Web
```

---

## Roadmap & docs

| Doc | Purpose |
|---|---|
| [WHERE_WE_LEFT_OFF.md](./WHERE_WE_LEFT_OFF.md) | Current milestone notes and suggested next steps |
| [ROADMAP_KANBAN.md](./ROADMAP_KANBAN.md) | Backlog / This Week / Done |
| [DEPLOYMENT_READINESS.md](./DEPLOYMENT_READINESS.md) | Pre-production checklist |
| [FinanceApp.Documentations/LANGUAGE_SWITCHING_TODO.md](./FinanceApp.Documentations/LANGUAGE_SWITCHING_TODO.md) | i18n baseline (en / es / sw) + optional follow-up (see **Localization** under [Features](#features)) |

**High-level status**

| Area | Status |
|---|---|
| Web: expenses, income, budgets, accounts, transactions, documents, dashboard | ✅ |
| Web: reports, notifications, profile, landing, admin | ✅ |
| API: JWT + feature parity for mobile | ✅ |
| Excel export (expenses) | ✅ |
| Recurring templates + background job | ✅ |
| Mobile (Expo) — core flows | ✅ (evolving; see mobile README) |
| Unit + API integration tests | 🔄 (baseline present; expand coverage) |
| Subscription enforcement / plan gating | 🔄 (see roadmap) |
| i18n (Web + API + Mobile) — **en**, **es**, **sw** via `FinanceApp.Localization` | ✅ baseline (expand string coverage / polish as needed) |
| Production hardening (secrets, CORS, blob storage, health checks) | ⏳ See deployment doc |

---

## Author

**Obadia Gilbert**

Built with a focus on clean code, real-world architecture patterns, and a genuinely usable UI.

---

*FinanceApp — take control of your finances.*
