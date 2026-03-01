# FinanceApp

FinanceApp is a personal finance management system built using ASP.NET Core MVC and Clean Architecture principles.

The application is designed for personal use initially, with future plans to transform it into a scalable SaaS product.

---

## 🚀 Tech Stack

- .NET 8 (LTS)
- ASP.NET Core MVC
- Entity Framework Core
- SQL Server (Docker)
- Clean Architecture
- Bootstrap

---

## 🏗 Architecture

The solution follows Clean Architecture:

- FinanceApp.Domain
- FinanceApp.Application
- FinanceApp.Infrastructure
- FinanceApp.Web

This ensures separation of concerns, scalability, and maintainability.

---

## 📌 Features (Phase 1)

| Feature | Status |
|--------|--------|
| Expense tracking | ✅ Implemented |
| Receipt uploads | ✅ Implemented |
| **Budget management** | ✅ **Implemented** — set monthly budget; alert when expenses reach limit |
| Multi-user ready structure | ✅ Implemented (Identity, roles, per-user data) |
| Dashboard analytics | ✅ Implemented (totals, charts, budget vs spend) |
| Earnings tracking | ⏳ Not yet implemented |

---

## 🔮 Future Plans

- Investment tracking
- Recurring transaction engine
- SaaS subscription model
- Stripe integration
- Cloud storage (Azure Blob)
- REST API for mobile app

---

## 🐳 Running with Docker (SQL Server)

1. Pull SQL Server image
2. Run container
3. Apply EF Core migrations

---

## 👨‍💻 Author

Obadia Gilbert