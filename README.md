# FinanceApp

> A full-stack personal finance management platform built with **.NET 10**, **Clean Architecture**, and a **dual-surface delivery model** — a rich MVC web application and a RESTful API ready for mobile clients.

![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)
![ASP.NET Core](https://img.shields.io/badge/ASP.NET_Core-MVC_%2B_API-blue?logo=microsoft)
![EF Core](https://img.shields.io/badge/EF_Core-10.0-orange)
![Bootstrap](https://img.shields.io/badge/Bootstrap-5.3-7952B3?logo=bootstrap)
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
10. [REST API](#rest-api)
11. [Database Migrations](#database-migrations)
12. [Roadmap](#roadmap)
13. [Author](#author)

---

## Overview

FinanceApp is a personal finance management system that gives individuals full visibility over their spending, accounts, budgets, and transactions — with a clean, responsive UI and a separate REST API for future mobile integration.

The platform is designed to evolve from a **personal-use tool** into a **multi-tenant SaaS product**, so every architectural decision from day one prioritises separation of concerns, testability, and scalability.

**Key design goals:**

- Per-user data isolation via ASP.NET Core Identity
- Clean Architecture enforcement — domain logic never leaks into infrastructure or presentation
- Both a browser-first MVC surface and a stateless API surface co-existing in the same solution
- Dark mode, responsive layout, and accessibility from the start

---

## Architecture

The solution follows **Clean Architecture** (Onion / Ports & Adapters), with dependency rules pointing strictly inward:

```
┌─────────────────────────────────────────────────────────┐
│                    Presentation Layer                   │
│                                                         │
│   FinanceApp.Web (MVC)     FinanceApp.API (REST)        │
│   ─ Controllers            ─ Controllers                │
│   ─ Razor Views            ─ DTOs                       │
│   ─ ViewModels             ─ JWT Auth                   │
└────────────────┬────────────────────┬───────────────────┘
                 │                    │
┌────────────────▼────────────────────▼───────────────────┐
│                  Infrastructure Layer                   │
│                                                         │
│   FinanceApp.Infrastructure                             │
│   ─ EF Core / DbContext    ─ Generic Repository         │
│   ─ ASP.NET Core Identity  ─ Email Service              │
│   ─ Refresh Token Service  ─ File Storage               │
└────────────────────────┬────────────────────────────────┘
                         │
┌────────────────────────▼────────────────────────────────┐
│                  Application Layer                      │
│                                                         │
│   FinanceApp.Application                                │
│   ─ Service Interfaces     ─ Service Implementations    │
│   ─ DTOs / PagedResult     ─ Currency Conversion        │
└────────────────────────┬────────────────────────────────┘
                         │
┌────────────────────────▼────────────────────────────────┐
│                    Domain Layer                         │
│                                                         │
│   FinanceApp.Domain                                     │
│   ─ Entities               ─ Enums                      │
│   ─ BaseEntity             ─ Domain rules               │
└─────────────────────────────────────────────────────────┘
```

**Dependency rule:** each layer only depends on the layer directly below it. The Domain has zero external dependencies.

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
| Social Login | Google / Facebook / Twitter OAuth2 | 10.0 |
| UI Framework | Bootstrap | 5.3 |
| Icons | Bootstrap Icons | 1.13 |
| Charts | Chart.js | CDN latest |
| Tables | DataTables.js | 1.13 |
| Excel Export | ClosedXML | 0.105 |
| Fonts | Inter (Google Fonts) | — |
| Email | SMTP via custom `IEmailService` | — |

---

## Features

### Expense Management
- Create, edit, and delete expenses with description, amount, currency, date, and category
- Full-text search and sortable DataTables with client-side pagination
- Multi-currency support — all totals normalised for cross-currency comparison

### Supporting Documents
- Attach multiple receipts, invoices, or PDFs to any expense or transaction
- In-place file preview (images rendered inline, PDFs in iframe)
- AJAX upload and delete — no full page reload
- Files stored at `wwwroot/uploads/documents/{UserId}/`

### Category Management
- Custom categories per user with name, icon, and colour
- Per-category budget limits with real-time alert thresholds
- Category spending breakdown on the dashboard doughnut chart

### Budget Tracking
- Monthly global budget with progress bar and over-budget alerts
- Per-category sub-budgets with configurable warning thresholds
- Dashboard badges indicate on-track / warning / exceeded status

### Account Management
- Multiple account types: Checking, Savings, Credit Card, Cash, Investment
- Per-account balance tracking
- Account balance summary on the dashboard

### Transaction Ledger
- Debit and credit transaction recording against accounts
- Supporting document attachments per transaction

### Dashboard Analytics
- 4 KPI cards: All-time spend, This Month (dynamic calendar badge), Avg Daily, Transaction count
- Month-over-month trend indicators (↑/↓ vs prior month)
- 30-day spending trend line chart (Chart.js)
- Current month category breakdown doughnut chart
- Recent expenses quick-view table
- Monthly budget progress bar + account balances panel

### UI/UX
- Fixed shell layout: navbar and sidebar stay anchored while main content scrolls
- Fully responsive — collapsible sidebar on mobile
- System-aware dark mode with a single-click toggle, persisted in `localStorage`
- AJAX offcanvas for create/edit workflows — no disruptive navigations
- Toast notifications for all async operations

### REST API
- Full CRUD endpoints for expenses, categories, accounts, transactions, budgets
- JWT authentication with refresh token rotation
- Dashboard analytics endpoint
- Supporting documents upload/delete
- Swagger / OpenAPI documentation

### Security & Identity
- ASP.NET Core Identity with email confirmation flow
- Role-based access control (`Admin`, `User`)
- Social login (Google, Facebook, Twitter)
- Anti-forgery tokens on all state-mutating requests
- Per-user data isolation enforced at the service layer

### Administration
- Admin area: user list, role management
- Subscription plan management (`Free`, `Pro`, `Enterprise`)

---

## Project Structure

```
FinanceApp/
├── FinanceApp.Domain/                  # Zero-dependency domain layer
│   ├── Common/
│   │   └── BaseEntity.cs              # Id, CreatedAt, UpdatedAt
│   ├── Entities/                      # Expense, Category, Account, Budget,
│   │   └── ...                        #   Transaction, SupportingDocument, etc.
│   └── Enums/                         # Currency, AccountType, TransactionType, etc.
│
├── FinanceApp.Application/             # Use cases / business logic
│   ├── Interfaces/                    # IExpenseService, ICategoryService, etc.
│   ├── Services/                      # Concrete implementations
│   ├── DTOs/                          # Internal data contracts
│   └── Common/                        # PagedResult<T>, CategoryDefaults
│
├── FinanceApp.Infrastructure/          # Framework & external concerns
│   ├── Persistence/
│   │   ├── FinanceDbContext.cs
│   │   └── Migrations/
│   ├── Identity/
│   │   ├── ApplicationUser.cs
│   │   └── RoleSeeder.cs
│   ├── Repositories/
│   │   └── Repository.cs              # Generic IRepository<T>
│   └── Services/                      # Email, UserService
│
├── FinanceApp.Web/                     # MVC web application
│   ├── Controllers/                   # Expense, Category, Account, Budget, etc.
│   ├── Views/                         # Razor views + shared partials + layouts
│   │   └── Shared/
│   │       ├── _Layout.cshtml         # Fixed-shell layout (navbar + sidebar + main)
│   │       ├── _Sidebar.cshtml
│   │       └── Components/            # UserProfile ViewComponent
│   ├── Models/                        # ViewModels
│   └── wwwroot/
│       ├── css/site.css               # Design tokens, layout, components
│       ├── css/dark-mode.css          # Dark theme overrides
│       ├── js/site.js                 # AJAX, offcanvas, supporting docs, toasts
│       └── uploads/                   # User-uploaded documents (gitignored)
│
├── FinanceApp.API/                     # REST API
│   ├── Controllers/                   # Expenses, Categories, Auth, Dashboard, etc.
│   ├── DTOs/                          # Request/Response contracts
│   └── Program.cs                     # JWT, Swagger, CORS config
│
└── FinanceApp.Documentations/          # Architecture notes and docs
```

---

## Prerequisites

| Tool | Minimum Version | Notes |
|---|---|---|
| [.NET SDK](https://dotnet.microsoft.com/download) | 10.0 | `dotnet --version` to verify |
| [SQL Server](https://www.microsoft.com/en-us/sql-server/sql-server-downloads) | 2019+ | LocalDB works for dev; Docker recommended |
| [Git](https://git-scm.com/) | any | — |
| [Docker](https://www.docker.com/) *(optional)* | 20+ | For SQL Server in a container |

---

## Getting Started

### 1. Clone the repository

```bash
git clone https://github.com/your-username/FinanceApp.git
cd FinanceApp
```

### 2. Restore dependencies

```bash
dotnet restore
```

### 3. Configure the database connection

Copy the example settings and fill in your connection string:

```bash
cp FinanceApp.Web/appsettings.json FinanceApp.Web/appsettings.Development.json
```

Edit `appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=FinanceAppDb;Trusted_Connection=True;MultipleActiveResultSets=true"
  }
}
```

> **Docker alternative:** spin up SQL Server in one command:
> ```bash
> docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=YourStrong@Passw0rd" \
>   -p 1433:1433 --name financeapp-sql -d mcr.microsoft.com/mssql/server:2022-latest
> ```
> Then use: `Server=localhost,1433;Database=FinanceAppDb;User=sa;Password=YourStrong@Passw0rd`

### 4. Apply database migrations

```bash
dotnet ef database update --project FinanceApp.Infrastructure --startup-project FinanceApp.Web
```

This creates all tables and seeds the default roles (`Admin`, `User`).

### 5. Run the web application

```bash
dotnet run --project FinanceApp.Web
```

Open [https://localhost:5001](https://localhost:5001) (or the port shown in the terminal).

### 6. Run the API *(optional)*

```bash
dotnet run --project FinanceApp.API
```

Swagger UI: [https://localhost:7001/swagger](https://localhost:7001/swagger)

---

## Configuration

### `appsettings.json` — Web

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "<sql-server-connection-string>"
  },
  "EmailSettings": {
    "SmtpHost": "smtp.example.com",
    "SmtpPort": 587,
    "SmtpUser": "no-reply@example.com",
    "SmtpPass": "<password>",
    "FromName": "FinanceApp"
  },
  "Authentication": {
    "Google": { "ClientId": "", "ClientSecret": "" },
    "Facebook": { "AppId": "", "AppSecret": "" },
    "Twitter": { "ConsumerKey": "", "ConsumerSecret": "" }
  }
}
```

### `appsettings.json` — API

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "<same-database>"
  },
  "JwtSettings": {
    "SecretKey": "<at-least-32-character-secret>",
    "Issuer": "FinanceApp.API",
    "Audience": "FinanceApp.Clients",
    "AccessTokenExpiryMinutes": 60,
    "RefreshTokenExpiryDays": 30
  }
}
```

> Store secrets in [.NET User Secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets) or environment variables in production — never commit credentials to source control.

---

## Running the Application

| Command | What it does |
|---|---|
| `dotnet run --project FinanceApp.Web` | Start the MVC web app |
| `dotnet run --project FinanceApp.API` | Start the REST API |
| `dotnet build` | Build all projects |
| `dotnet test` | Run tests (when test project is added) |
| `dotnet ef migrations add <Name> --project FinanceApp.Infrastructure --startup-project FinanceApp.Web` | Add a new EF migration |
| `dotnet ef database update --project FinanceApp.Infrastructure --startup-project FinanceApp.Web` | Apply pending migrations |

---

## REST API

The API uses **JWT Bearer authentication**. All protected endpoints require an `Authorization: Bearer <token>` header.

### Authentication

| Method | Endpoint | Description |
|---|---|---|
| `POST` | `/api/auth/register` | Register a new user |
| `POST` | `/api/auth/login` | Obtain access + refresh tokens |
| `POST` | `/api/auth/refresh` | Rotate refresh token |

### Core Resources

| Resource | Base Path |
|---|---|
| Expenses | `/api/expenses` |
| Categories | `/api/categories` |
| Accounts | `/api/accounts` |
| Transactions | `/api/transactions` |
| Budgets | `/api/budgets` |
| Supporting Documents | `/api/supportingdocuments` |
| Dashboard | `/api/dashboard` |

All collection endpoints support `page`, `pageSize`, and `search` query parameters.

Swagger UI (when running locally): `https://localhost:<port>/swagger`

---

## Database Migrations

EF Core migrations live in `FinanceApp.Infrastructure`. The startup project provides the connection string and DI container.

```bash
# Add a migration
dotnet ef migrations add YourMigrationName \
  --project FinanceApp.Infrastructure \
  --startup-project FinanceApp.Web

# Apply to database
dotnet ef database update \
  --project FinanceApp.Infrastructure \
  --startup-project FinanceApp.Web

# Rollback one migration
dotnet ef database update PreviousMigrationName \
  --project FinanceApp.Infrastructure \
  --startup-project FinanceApp.Web
```

---

## Roadmap

| Feature | Status |
|---|---|
| Expense tracking | ✅ Complete |
| Category management + budgets | ✅ Complete |
| Account management | ✅ Complete |
| Transaction ledger | ✅ Complete |
| Supporting documents (receipts / invoices) | ✅ Complete |
| Dashboard analytics + charts | ✅ Complete |
| REST API with JWT auth | ✅ Complete |
| Social login (Google, Facebook, Twitter) | ✅ Complete |
| Dark mode | ✅ Complete |
| Fixed-shell layout (sticky nav + sidebar) | ✅ Complete |
| Subscription plan management | 🔄 In progress |
| Income / earnings tracking | ⏳ Planned |
| Recurring transaction engine | ⏳ Planned |
| Export to Excel / CSV | ⏳ Planned |
| Azure Blob Storage for documents | ⏳ Planned |
| Currency exchange rate integration | ⏳ Planned |
| Stripe payment integration (SaaS) | ⏳ Planned |
| Mobile app (API consumer) | ⏳ Planned |
| Unit + integration test suite | ⏳ Planned |
| Docker Compose (full stack) | ⏳ Planned |

---

## Author

**Obadia Gilbert**

Built with a focus on clean code, real-world architecture patterns, and a genuinely usable UI.

---

*FinanceApp — take control of your finances.*
