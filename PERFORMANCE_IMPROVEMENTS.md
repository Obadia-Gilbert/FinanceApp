# Performance improvements for scale (many users)

This document outlines changes that will help the system perform well as user count and data volume grow. Items are ordered by impact and effort.

---

## 1. Stop loading full expense/transaction sets (high impact, medium effort)

**Problem:** Several paths load all or very large subsets of user data into memory:

| Location | Issue |
|----------|--------|
| **Web `HomeController.Index`** | `GetPagedExpensesAsync(..., pageSize: int.MaxValue)` – loads every expense for the user. |
| **API `DashboardController.GetDashboard`** | `pageSize: 1000` – still heavy for power users; then all grouping/filtering in memory. |
| **`MonthlyReportService.GetMonthlyReportAsync`** | Fetches up to 5000 expenses and 5000 transactions for one month. |

**Why it hurts:** With many users and long histories, this causes large queries, high memory use, and slow responses.

**Recommendations:**

- **Dashboard (Web):**  
  - Do **not** load all expenses.  
  - Use **bounded queries** for what the dashboard needs, e.g.:
    - One query: sum of expenses per currency for “this month” and “last 30 days” (e.g. `UserId`, `ExpenseDate` range, `Currency` → `Sum(Amount)`).
    - Another: recent N expenses (e.g. `UserId`, order by `ExpenseDate` desc, `Take(20)`).
  - If you keep a “chart by day” or “category breakdown”, either aggregate in the DB (GROUP BY date/category) or limit to last 30/90 days and a max number of rows.

- **Dashboard (API):**  
  - Same idea: **aggregate in the database** (SUM, GROUP BY) for totals and chart data.  
  - Cap “recent items” (e.g. last 10–20).  
  - Remove the pattern “fetch 1000 expenses then filter in C#”.

- **Monthly report:**  
  - For a single month, **filter by month in the query** and only fetch that month’s expenses/transactions (no need for 5000 cap if the query is `UserId + Year + Month`).  
  - Consider **aggregated queries** for category totals and top N expenses instead of loading full lists.

**Implementation direction:** Add (or extend) application services to return **aggregates** (e.g. `GetExpenseTotalsAsync(userId, from, to)`, `GetRecentExpensesAsync(userId, count)`), and have dashboard/report use these instead of “get everything then filter in memory”.

---

## 2. Add database indexes for hot query patterns (high impact, low effort)

**Problem:** Queries often filter by `UserId` and by date (e.g. “this month”, “last 30 days”). Without the right indexes, the database scans many rows.

**Recommendations:**

- **Expenses:**  
  - Add composite index: `(UserId, ExpenseDate)` (or `(UserId, ExpenseDate DESC)` if supported) for dashboard and report “by user and date range” queries.

- **Transactions:**  
  - Add composite index: `(UserId, Date)` (or `(UserId, Date DESC)`) for ledger lists and monthly income/expense totals.  
  - Optionally: `(UserId, Type, Date)` if you often filter by type (e.g. income) and date.

- **Incomes:**  
  - Already has `UserId`. If you often filter by `IncomeDate`, add `(UserId, IncomeDate)`.

**Implementation:** In `FinanceDbContext`, add `entity.HasIndex(...)` for these columns and create a new migration. No application logic change, only schema.

---

## 3. Remove N+1 and per-item work in loops (high impact, low–medium effort)

**Problem:** In `DashboardController.GetDashboard`, for **each** category budget you call:

- `_categoryBudgetService.GetCategorySpendAsync(...)`  
- `_notificationService.CreateIfNotExistsAsync(...)`  

So N category budgets ⇒ N spend queries + N notification checks/inserts. Same pattern appears in `MonthlyReportService` (loop over category budgets calling `GetCategorySpendAsync`).

**Recommendations:**

- **Category spend:**  
  - Add a **batch** method, e.g. `GetCategorySpendForMonthAsync(userId, month, year)` returning a dictionary `CategoryId → amount` (one or two queries with GROUP BY), and use it in dashboard and monthly report instead of calling `GetCategorySpendAsync` in a loop.

- **Notifications:**  
  - In the dashboard, either:
    - Build the list of “alerts” in memory, then call a **single** method that creates missing notifications in one go (e.g. “ensure these topic keys exist for this user”), or  
    - Move “create budget notifications” to a **background job** that runs after budget/spend data is known, so the dashboard request only reads data and doesn’t do N inserts.

---

## 4. Cache hot, per-user data (medium impact, medium effort)

**Problem:** Dashboard and category list are hit often and change relatively slowly. Doing the same aggregates and list queries on every request adds load as users grow.

**Recommendations:**

- **In-memory cache (single server):**  
  - Cache “dashboard summary” per user with a short TTL (e.g. 1–2 minutes). Key e.g. `dashboard:{userId}`.  
  - Cache “category list” per user with a longer TTL (e.g. 5–10 minutes) or invalidate on create/update/delete category.

- **Distributed cache (multi-server / production):**  
  - Use **Redis** (or similar) for the same keys and TTLs so all app instances share the cache.  
  - Reduces repeated DB work and keeps response times stable under load.

- **What to cache:**  
  - Aggregated dashboard data (totals, chart points, “recent” list).  
  - Category list (and possibly budget list) per user.  
  - Do **not** cache full expense/transaction lists; keep those query-based and paginated.

---

## 5. Pagination and repository (medium impact, low effort)

**Current behavior:** Generic repository often does:

1. `CountAsync()` for total count  
2. `Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync()` for the page  

So every paged request = 2 round trips. For “next page” or infinite scroll, the client sometimes doesn’t need the total count.

**Recommendations:**

- **Optional total count:**  
  - Add an overload or parameter like `getTotalCount: bool`. When `false`, skip `CountAsync()` and only run the Skip/Take query (one round trip). Use for “next page” or mobile infinite scroll.

- **Default page size:**  
  - Enforce a **max page size** (e.g. 100) in API and Web so a single request never asks for 1000+ items.

---

## 6. API and infrastructure (medium impact, low effort)

- **Response compression:**  
  - Enable gzip/Brotli for API responses (e.g. `AddResponseCompression()` and `UseResponseCompression()`). Helps mobile and web when payloads are large (e.g. long expense list).

- **Connection pooling:**  
  - Keep default connection pooling enabled in the connection string. For production, set a sensible `Max Pool Size` if you have many concurrent users.

- **JWT validation:**  
  - Ensure you’re not doing heavy work per request (e.g. DB hit) to validate the token; use standard JWT validation with a cached key. If you look up the user on every request, consider short-lived cache per `userId`.

- **CORS:**  
  - In production, restrict CORS to your real origins instead of `AllowAnyOrigin()`; this doesn’t improve DB performance but reduces unnecessary preflight and keeps security in check.

---

## 7. Heavy operations offloaded to background (medium impact, medium effort)

**Current:** Recurring transaction job already runs in the background. Other good candidates:

- **Monthly report generation:**  
  - If you add “email me the report” or “generate PDF”, do it in a **background job** and notify when ready, instead of generating synchronously in the request.

- **Notification creation:**  
  - As above, moving “create budget notifications” to a small background step (after spend/budget data is updated) keeps dashboard requests read-only and fast.

- **Exports (Excel, etc.):**  
  - For large exports, queue a job, generate the file, store it (e.g. blob), and return a link or send email. Avoid long-running requests.

---

## 8. Database and hosting at scale (larger effort)

When the single database becomes the bottleneck:

- **Read replicas:**  
  - Use **read replica(s)** for reporting and dashboard reads; run writes (create/update/delete) on the primary. Configure read-only contexts in the app for query services that don’t need writes.

- **Connection string:**  
  - Point read-only operations to the replica (e.g. via connection string or a “read” DbContext). Keep writes and transactional reads on the primary.

- **Hosting:**  
  - Prefer **multiple app instances** behind a load balancer so you scale out the API and Web. With distributed cache (e.g. Redis), caching still works across instances.

---

## Summary checklist (priority order)

| Priority | Action | Impact | Effort |
|----------|--------|--------|--------|
| 1 | Replace “load all / 1000 / 5000” with aggregated and bounded queries (dashboard, report) | High | Medium |
| 2 | Add composite indexes: `(UserId, ExpenseDate)`, `(UserId, Date)` (and optionally type) | High | Low |
| 3 | Batch category-spend and notification creation; avoid N+1 in dashboard and report | High | Low–Medium |
| 4 | Enforce max page size (e.g. 100) on all paged endpoints | Medium | Low |
| 5 | Optional total count in paged repository/API for “next page” | Medium | Low |
| 6 | Per-user caching (dashboard, categories) with short TTL; Redis when multi-instance | Medium | Medium |
| 7 | Enable API response compression | Medium | Low |
| 8 | Background jobs for reports/exports and notification creation | Medium | Medium |
| 9 | Read replicas + read-only paths when DB becomes bottleneck | High at scale | Higher |

Implementing **1–4** will already make the system much more robust as user count and data volume grow. Then add caching and compression, and consider background jobs and read replicas as you approach production scale.
