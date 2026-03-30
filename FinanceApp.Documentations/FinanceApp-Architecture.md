# FinanceApp – API-First Clean Architecture

**Version:** 1.2  
**Last updated:** 29 March 2026 — refresh the “Where We Are Now” table when major capabilities shift; [Current-State.md](./Current-State.md) is the concise snapshot.

---

## Current Status & Direction

### Where We Are Now

FinanceApp is a **working multi-client personal finance platform**:

| Area | Status |
|------|--------|
| **Solution** | Domain, Application, Infrastructure, **Web**, **API**, **Localization**, tests; **Expo mobile** in-repo (`FinanceApp.Mobile`) |
| **Data model** | Expenses, Income, Categories, Budgets, CategoryBudgets, Accounts, Transactions, SupportingDocuments, Notifications, SharedReport, RecurringTemplate, … |
| **Auth** | Cookie + Identity (web); **JWT + refresh** (API / mobile); external OAuth (Google, Facebook, Twitter) |
| **Features** | Full web + API parity for core flows; dashboard; documents; monthly report + share; recurring job; subscriptions; **localization (en / es / sw)** via `FinanceApp.Localization` |
| **Storage** | Uploads under web/API `wwwroot` / configured paths; same database for web + API + mobile clients |
| **API** | **FinanceApp.API** — REST, OpenAPI at `/openapi/v1.json` |
| **Mobile** | **FinanceApp.Mobile** — Expo, consumes API with JWT; i18next + `Accept-Language` |

### Where We Are Heading

Ongoing work: **production hardening**, subscription enforcement polish, broader automated tests, mobile store readiness, and optional **i18n** string coverage expansion — see root [README.md](../README.md), [WHERE_WE_LEFT_OFF.md](../WHERE_WE_LEFT_OFF.md), [Current-State.md](./Current-State.md).

> **Historical note:** [Section 2](#2-current-vs-target-state) below describes the **migration** from web-only toward API + mobile. That migration is **largely complete** in the codebase; use Section 2 as narrative context, not as “not yet built.”

---

## Table of Contents

1. [Executive Summary](#1-executive-summary)
2. [Current vs Target State](#2-current-vs-target-state)
3. [Target Solution Architecture](#3-target-solution-architecture)
4. [Domain Model – Financial Ledger System](#4-domain-model--financial-ledger-system)
5. [Authentication Strategy](#5-authentication-strategy)
6. [API Endpoint Structure](#6-api-endpoint-structure)
7. [Financial Integrity Rules](#7-financial-integrity-rules)
8. [Security Requirements](#8-security-requirements)
9. [React Native Integration](#9-react-native-integration)
10. [Production Readiness](#10-production-readiness)
11. [Glossary](#11-glossary)

---

## 1. Executive Summary

FinanceApp is being refactored from a traditional ASP.NET Core MVC application into a scalable, API-first architecture while preserving:

- ASP.NET Core
- Razor Pages / MVC (Web Client)
- ASP.NET Core Identity
- Entity Framework Core
- SQL Server
- External OAuth Providers (Google, Facebook, Twitter)

The system will support:

- Web application (Razor Pages / MVC)
- React Native mobile application
- Shared centralized database
- JWT authentication for mobile clients
- Cookie authentication for web
- Role-based authorization
- Finance-grade security practices

---

## 2. Current vs Target State

> **As of 2026:** The codebase **includes** `FinanceApp.API`, `FinanceApp.Mobile`, `FinanceApp.Localization`, accounts/transactions/supporting documents, JWT, and mobile clients. The subsections **2.1–2.3** describe the **original** migration from web-only; treat them as **historical context**. For an accurate snapshot of what is deployed in code today, see [Current-State.md](./Current-State.md) and the root [README.md](../README.md).

### 2.1 Current State (Implemented)

*Historical snapshot (pre–API/mobile expansion):*

**Solution structure:**
```
FinanceApp.sln
├── FinanceApp.Domain      ✅
├── FinanceApp.Application ✅
├── FinanceApp.Infrastructure ✅
└── FinanceApp.Web        ✅
```

**Implemented features:**

| Feature | Status |
|---------|--------|
| Expense tracking | ✅ Create, edit, delete, list |
| Receipt upload | ✅ Optional; JPG/PNG/GIF/WebP/PDF; preview |
| Categories | ✅ User-defined; icon, color |
| Monthly budget | ✅ One per user per month; dashboard alerts |
| Category budgets | ✅ Per-category limits; 80% and 100% alerts |
| Dashboard | ✅ Totals, charts, budget vs spend |
| Admin panel | ✅ Manage users; assign admin role |
| User profile | ✅ First/Last name, photo; edit via navbar |
| Subscription | ✅ Free/Pro/Premium (UI) |
| External login | ✅ Google, Facebook, Twitter |
| Dark mode | ✅ Theme toggle |

**Current data model:** Expense, Category, Budget, CategoryBudget (all with UserId isolation).

### 2.2 Target State (Planned — largely achieved in repo)

*Original roadmap targets; most items are now implemented.*

| Area | Target |
|------|--------|
| **Solution** | Add FinanceApp.API for REST endpoints |
| **Data model** | Accounts, Transactions (double-entry), SupportingDocuments |
| **Auth** | Cookie (web) + JWT (API) + refresh tokens |
| **Storage** | SupportingDocuments table; cloud storage (optional) |
| **Clients** | Web + React Native mobile |

### 2.3 Migration Path (historical phases)

1. **Phase 1:** Add FinanceApp.API; expose existing Expense/Category/Budget via REST.
2. **Phase 2:** Introduce JWT; keep cookie auth for web.
3. **Phase 3:** Add Accounts and Transactions; migrate Expenses to Transactions.
4. **Phase 4:** Support SupportingDocuments; migrate to cloud storage.

---

## 3. Target Solution Architecture

### 3.1 Solution Structure

```
FinanceApp.sln  (see root README for full layout)
│
├── FinanceApp.Domain
├── FinanceApp.Application
├── FinanceApp.Infrastructure
├── FinanceApp.API
├── FinanceApp.Localization
├── FinanceApp.Web
├── FinanceApp.Tests
└── FinanceApp.API.Tests
```
*(Mobile: `FinanceApp.Mobile` in-repo, often listed outside the same .slnx file.)*

### 3.2 Layer Responsibilities

| Layer | Purpose | Contains |
|-------|---------|----------|
| **Domain** | Core business models and financial rules | Entities, Enums, Value Objects, domain logic. No dependencies on ASP.NET, EF Core, or Infrastructure. |
| **Application** | Business use cases and orchestration | Service interfaces, Commands/Queries, DTOs, validation, business rules. Used by API and Web. |
| **Infrastructure** | Technical implementation | EF Core DbContext, Identity, repositories, JWT token service, OAuth, file storage. |
| **API** | REST backend for mobile/SPA | JSON responses, JWT auth, thin controllers. |
| **Web** | Razor Pages / MVC | User interface, admin panel, cookie auth. |

---

## 4. Domain Model – Financial Ledger System

### Core Principles

- Multi-tenant isolation using UserId
- Balance is computed dynamically (never stored)
- Transfers use double-entry modeling
- Budgets overlay transaction ledger
- Supporting documents are optional

### 4.1 Users (ASP.NET Identity)

Fields: Id, Email, PasswordHash, CreatedAt, IsActive.

All financial tables include UserId.

### 4.2 Accounts (Target)

Represents financial containers.

| Field | Type |
|-------|------|
| Id | GUID |
| UserId | GUID |
| Name | string |
| Type | Checking, Savings, CreditCard, Cash |
| InitialBalance | decimal(18,2) |
| IsActive | bool |
| CreatedAt | datetime |

**Important:** CurrentBalance is NOT stored. Balance = InitialBalance + SUM(Transactions).

### 4.3 Transactions (Target)

Core ledger entity.

| Field | Type |
|-------|------|
| Id | GUID |
| UserId | GUID |
| AccountId | GUID |
| CategoryId | GUID (nullable for transfers) |
| Type | Income, Expense, Transfer |
| Amount | decimal (always positive) |
| Date | datetime |
| Note | string (optional) |
| TransferGroupId | GUID (nullable) |
| IsRecurring | bool |
| CreatedAt | datetime |

**Transfer modeling (double-entry):** Transfers create TWO records linked by TransferGroupId.

### 4.4 Categories

| Field | Type |
|-------|------|
| Id | GUID |
| UserId | GUID (nullable for system categories) |
| Name | string |
| Type | Income / Expense |
| Icon | string |
| Color | string |
| IsSystem | bool |

### 4.5 Budgets (Monthly)

| Field | Type |
|-------|------|
| Id | GUID |
| UserId | GUID |
| Month | int (1–12) |
| Year | int |
| Amount | decimal |
| Currency | enum |
| CreatedAt | datetime |

Unique constraint: (UserId, Month, Year).

### 4.6 BudgetCategories / CategoryBudgets (Target)

Per-category monthly allocation.

| Field | Type |
|-------|------|
| Id | GUID |
| UserId | GUID |
| CategoryId | GUID |
| Month | int |
| Year | int |
| Amount | decimal |
| Currency | enum |

Spent amount is computed from Transactions (or Expenses in current model).

### 4.7 SupportingDocuments (Target)

| Field | Type |
|-------|------|
| Id | GUID |
| TransactionId | GUID |
| UserId | GUID |
| FileName | string |
| FilePath | string |
| ContentType | string |
| FileSize | long |
| DocumentType | enum (Receipt, Invoice, Contract, etc.) |
| UploadedAt | datetime |

---

## 5. Authentication Strategy

### 5.1 Cookie Authentication (Web)

Used by FinanceApp.Web for Razor Pages and MVC.

### 5.2 JWT Authentication (API – Target)

Used by React Native and future SPA clients.

### 5.3 JWT Configuration Example

```csharp
builder.Services.AddAuthentication()
    .AddCookie("WebCookie")
    .AddJwtBearer("JwtBearer", options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = config["Jwt:Issuer"],
            ValidAudience = config["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(config["Jwt:Key"]))
        };
    });
```

### 5.4 Refresh Tokens (Target)

Table: Id, UserId, Token, ExpiryDate, IsRevoked.  
Endpoint: `POST /api/auth/refresh`. Rotate refresh tokens after each use.

### 5.5 External OAuth Providers

Keep: Google, Facebook, Twitter.  
Mobile flow: OAuth login → send provider token to API → API validates and returns FinanceApp JWT.

---

## 6. API Endpoint Structure

### Authentication

| Method | Endpoint |
|--------|----------|
| POST | /api/auth/register |
| POST | /api/auth/login |
| POST | /api/auth/refresh |
| POST | /api/auth/external |

### Profile

| Method | Endpoint |
|--------|----------|
| GET | /api/users/profile |

### Accounts

| Method | Endpoint |
|--------|----------|
| GET | /api/accounts |
| POST | /api/accounts |
| PUT | /api/accounts/{id} |
| DELETE | /api/accounts/{id} |

### Transactions

| Method | Endpoint |
|--------|----------|
| GET | /api/transactions |
| POST | /api/transactions |
| POST | /api/transactions/transfer |
| PUT | /api/transactions/{id} |
| DELETE | /api/transactions/{id} |

### Supporting Documents

| Method | Endpoint |
|--------|----------|
| POST | /api/transactions/{id}/documents |
| GET | /api/transactions/{id}/documents |
| GET | /api/documents/{id} |
| DELETE | /api/documents/{id} |

### Budgets

| Method | Endpoint |
|--------|----------|
| GET | /api/budgets/{year}/{month} |
| POST | /api/budgets/{year}/{month} |
| PUT | /api/budgets/{id}/categories |

### Dashboard

| Method | Endpoint |
|--------|----------|
| GET | /api/dashboard/summary |

---

## 7. Financial Integrity Rules

1. Amount must always be positive.
2. Transfers must be atomic (database transaction).
3. Always filter by UserId.
4. Soft delete accounts.
5. Budget must be unique per month.
6. Never store computed balances.

---

## 8. Security Requirements

- **Server-side validation:** Never trust client input.
- **Prevent over-posting:** Use DTOs instead of binding entities directly.
- **Role-based authorization:** `[Authorize(Roles = "Admin")]`
- **Token expiration:** Access 15–30 min; Refresh 7–30 days; rotate refresh tokens.
- **File security:** Validate MIME types; limit file size; store outside wwwroot; serve via authorized endpoints; verify UserId ownership.

---

## 9. React Native Integration

Authentication flow: Login → receive JWT + Refresh Token → store securely → send `Authorization: Bearer {token}`.

Secure storage: iOS Keychain; Android EncryptedSharedPreferences; Expo SecureStore. Never AsyncStorage for tokens.

---

## 10. Production Readiness

- HTTPS: Force HTTPS redirection.
- CORS: Restrict to trusted origins.
- Environment: appsettings.Development.json, appsettings.Production.json; secrets in env vars.
- Additional: 256-bit JWT secret; logging; health checks; SQL backups; rate limiting; security headers; file storage isolation; exception middleware (no stack traces in production).

---

## 11. Glossary

| Term | Definition |
|------|------------|
| **Double-entry** | Each transfer creates two records (debit + credit) for ledger consistency. |
| **TransferGroupId** | Links the two transaction records in a transfer. |
| **Multi-tenant** | Data isolated per user via UserId. |
| **Soft delete** | Records marked as deleted but retained in DB. |

---

## 12. Final Outcome

FinanceApp becomes:

- A financial ledger system
- With budgeting overlay
- With supporting document capability
- Web + Mobile ready
- Clean architecture based
- Scalable and secure
- AI-ready for future enhancements
