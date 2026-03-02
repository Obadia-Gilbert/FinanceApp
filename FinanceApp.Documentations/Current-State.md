# FinanceApp – Current State

**Last updated:** March 2025

What is **implemented today**. For the target architecture and roadmap, see [FinanceApp-Architecture.md](./FinanceApp-Architecture.md).

---

## Where We Are Now

### Solution Structure

```
FinanceApp.sln
├── FinanceApp.Domain
├── FinanceApp.Application
├── FinanceApp.Infrastructure
└── FinanceApp.Web
```

### Tech Stack

| Component | Technology |
|-----------|------------|
| Framework | .NET 10 / ASP.NET Core MVC |
| ORM | Entity Framework Core |
| Database | SQL Server |
| Auth | ASP.NET Core Identity |
| External auth | Google, Facebook, Twitter |
| Frontend | Bootstrap 5, jQuery, DataTables, Chart.js |

### Implemented Features

| Feature | Status |
|---------|--------|
| Expense tracking | ✅ |
| Receipt upload & preview | ✅ |
| Categories | ✅ |
| Monthly budget | ✅ |
| Category budgets | ✅ |
| Dashboard | ✅ |
| Admin panel | ✅ |
| User profile | ✅ |
| Subscription (Free/Pro/Premium) | ✅ |
| External login | ✅ |
| Dark mode | ✅ |

### Current Data Model

- **Expense:** Amount, Currency, ExpenseDate, CategoryId, UserId, Description, ReceiptPath
- **Category:** Name, Description, Icon, BadgeColor, UserId
- **Budget:** UserId, Month, Year, Amount, Currency
- **CategoryBudget:** UserId, CategoryId, Month, Year, Amount, Currency

---

## Where We Are Heading

See [FinanceApp-Architecture.md](./FinanceApp-Architecture.md) Section 2 for the target state and migration path.
