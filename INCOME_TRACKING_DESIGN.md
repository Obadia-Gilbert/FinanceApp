# Income Tracking as First-Class (No Recurring Transactions)

**Goal:** Treat income like expenses (dedicated entity, CRUD, categories, UI) and implement recurring transactions (templates + job to generate instances).

---

## Current State

| Area | Expenses | Income today |
|------|----------|--------------|
| **Entity** | `Expense` (first-class) | Only `Transaction` with `Type = Income` |
| **Service** | `IExpenseService` | `ITransactionService` (shared with expense/transfer) |
| **Web** | `/Expense` list, create, edit, receipts | Single `/Transaction` list with type dropdown; no dedicated Income page |
| **Categories** | Many defaults (CategoryDefaults.cs) | `CategoryType.Income` exists but no default income categories |
| **Dashboard** | Uses Expense for spend | Uses Transaction (Type=Income) for monthly income |
| **Reports** | Monthly report = expenses + budgets | No income or net cash flow in report |
| **Account balance** | Not from Expense table | From Transaction (Income adds, Expense type subtracts) |

**Recurring:** `Transaction.IsRecurring` exists; UI has “Recurring transaction” checkbox. No recurrence engine. We **remove** recurring entirely.

---

## Design Decisions

### 1. First-class income

- **Add `Income` entity** (mirroring `Expense`): `Amount`, `Currency`, `UserId`, `IncomeDate`, `CategoryId`, optional `Description`, optional `Source`/`Note`.
- **Link to ledger:** Each `Income` creates one `Transaction` (Type=Income) so account balance and existing dashboard logic stay correct. `Income.TransactionId` points to that transaction; create/update/delete Income keeps the Transaction in sync.
- **Dedicated surface:** Income list, create, edit, delete in Web and API (e.g. `GET/POST /api/income`), with categories filtered to `CategoryType.Income` (and `Both`).

### 2. No recurring transactions

- **Remove `IsRecurring`** from `Transaction` (domain, DB, DTOs, UI).
- **No recurring engine:** No schedules, no auto-generation of future transactions.

### 3. Categories

- **Default income categories:** Add a small set (e.g. Salary, Freelance, Investment, Gift, Refund, Other Income) in `CategoryDefaults` or seed, with `CategoryType.Income`.
- **Existing** `CategoryType.Income` and `CategoryType.Both` continue to drive dropdowns for income.

### 4. Dashboard & reports

- **Dashboard:** Can keep using Transaction (Type=Income) for “monthly income” total (same numbers once we sync Income → Transaction). Optionally add “Recent income” from Income table.
- **Monthly report:** Extend `MonthlyReportResult` with `TotalIncome` and optionally `NetCashFlow`; populate from Income table (or Transaction Type=Income) for the selected month.

### 5. Transactions page

- **/Transaction** remains the combined ledger (Income + Expense + Transfer). Creating an income via **Income** will create a Transaction, so it still appears here. We do **not** show “Recurring” badge or checkbox anymore.

---

## Data Model

### New: `Income` (Domain)

```csharp
public class Income : BaseEntity
{
    public string UserId { get; private set; }
    public Guid AccountId { get; private set; }
    public Guid CategoryId { get; private set; }
    public decimal Amount { get; private set; }
    public Currency Currency { get; private set; }
    public DateTimeOffset IncomeDate { get; private set; }
    public string? Description { get; private set; }
    public string? Source { get; private set; }  // e.g. "Employer", "Client X"
    public Guid? TransactionId { get; private set; }  // linked ledger entry

    public Account Account { get; private set; }
    public Category Category { get; private set; }
    public Transaction? Transaction { get; private set; }
}
```

- **TransactionId:** Set when we create the companion `Transaction` (Type=Income). On delete Income, delete that Transaction. On update (amount/date/account/category), update the Transaction.

### Change: `Transaction`

- Remove `IsRecurring` property and constructor parameter; remove from all DTOs and UI.

---

## Sync Rules (Income ↔ Transaction)

| Action | Income | Transaction |
|--------|--------|-------------|
| Create Income | Insert Income, then create Transaction (Type=Income), set Income.TransactionId | One new row |
| Update Income | Update Income; update linked Transaction (amount, date, account, category, note) | Update existing |
| Delete Income | Delete Income | Delete linked Transaction (by TransactionId) |

- If `TransactionId` is null (e.g. legacy or import), delete Income only (no orphan Transaction to delete).

---

## Implementation Order

1. **Domain & persistence**
   - Add `Income` entity.
   - Remove `IsRecurring` from `Transaction`.
   - Add `Income` to DbContext; migration (AddIncomeTable, RemoveIsRecurringFromTransaction).
   - Default income categories (seed or CategoryDefaults + type).

2. **Application**
   - `IIncomeService`: GetById, GetPaged, Create, Update, Delete. Create/Update/Delete keep Transaction in sync.
   - `IncomeService` implementation (uses IRepository<Income>, ITransactionService or direct repo for Transaction).
   - Ensure category service returns income categories for income flows.

3. **Web**
   - Income controller: Index, Create, Edit, Delete (like Expense).
   - Views: list, create/edit (account, category [income], amount, date, description, source).
   - Sidebar: add “Income” link (e.g. next to Expenses).
   - Transaction: remove “Recurring” checkbox and badge from create form and list.

4. **API**
   - `GET/POST /api/income`, `GET/PUT/DELETE /api/income/{id}` (and DTOs). Create/update income triggers sync to Transaction.
   - Transactions API: remove `IsRecurring` from request/response DTOs.

5. **Dashboard & report**
   - Dashboard: optional “Recent income” from Income table; keep monthly income from Transaction (already correct if we sync).
   - Monthly report: add `TotalIncome` (and optionally `NetCashFlow`) to `MonthlyReportResult` and `IMonthlyReportService`.

6. **Tests**
   - Unit tests for IncomeService (create/update/delete and Transaction sync).
   - Integration test: create income via API, assert Transaction exists and balance/amount correct.

---

## Backward Compatibility

- **Existing Transaction rows** with Type=Income remain; they are not linked to any Income row. Dashboard/report can keep aggregating income from Transaction so old data still counts. No need to backfill Income from existing Income-type transactions unless we want a one-time migration (out of scope for this design).
- **IsRecurring:** Migration drops the column. Existing `true` values are discarded (no recurring engine to preserve).

---

## Summary

- **Income:** New first-class entity and flows (Web + API), synced to Transaction for balance and totals; default income categories.
- **Recurring:** Removed from domain, DB, API, and UI; no recurring engine.

This keeps account balance and dashboard logic correct while giving income parity with expenses and simplifying the model by dropping recurring.
