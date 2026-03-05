# Notifications – What to Expect & How to Test

In-app notifications are created **when you load the Dashboard**. The backend checks your budgets and spending, then creates notifications (at most one per scenario per month) so you can test different cases by setting budgets and expenses, then opening the Dashboard.

---

## Notification types

| Type | When it’s created | Example |
|------|-------------------|--------|
| **Budget exceeded** | Total monthly spend ≥ total monthly budget | "Monthly budget exceeded" – You've spent 60,000 TZS against a budget of 50,000 TZS. |
| **Category budget exceeded** | Spend in a category ≥ that category’s budget | "Category budget exceeded" – Transport: 12,000 TZS of 10,000 TZS (120%). |
| **Category budget warning** | Spend in a category ≥ 80% of that category’s budget (but not over) | "Category budget warning" – Food: 85,000 TZS of 100,000 TZS (85%). |

---

## How to test each scenario

### 1. Total budget exceeded

1. Set a **total monthly budget** (e.g. 50,000 TZS) for the current month (Budget tab → "+ Total").
2. Add **expenses** in the same currency that **total ≥ 50,000** (e.g. two expenses of 30,000 and 20,000).
3. Open the **Dashboard** tab (or pull to refresh on Dashboard).
4. Open **Notifications** – you should see: **"Monthly budget exceeded"** with the message about spend vs budget.

### 2. Category budget exceeded

1. Set a **category budget** (e.g. Transport: 10,000 TZS) for the current month (Budget tab → "+ By category" → pick Transport, amount 10000, Save).
2. Add **expenses** in the **Transport** category that **total ≥ 10,000** (e.g. one 10,000 or two 5,000).
3. Open the **Dashboard** tab.
4. Open **Notifications** – you should see: **"Category budget exceeded"** for Transport (e.g. "Transport: 10,000 TZS of 10,000 TZS (100%).").

### 3. Category budget warning (80%)

1. Set a **category budget** (e.g. Food: 100,000 TZS) for the current month.
2. Add **expenses** in the **Food** category that total **≥ 80,000 but &lt; 100,000** (e.g. 85,000).
3. Open the **Dashboard** tab.
4. Open **Notifications** – you should see: **"Category budget warning"** for Food (e.g. "Food: 85,000 TZS of 100,000 TZS (85%).").

---

## Quick test (sample notification)

If you want to see the Notifications screen with at least one item **without** setting budgets/expenses:

- Call **POST /api/notifications/sample** (authenticated).  
- This creates a single **Info** notification: *"Test notification" – "This is a sample notification for testing."*  
- Available only when the API is run in **Development** (e.g. `ASPNETCORE_ENVIRONMENT=Development`).

Then open the **Notifications** screen in the app; the sample notification will appear in the list.

---

## Tips

- Notifications are created **once per topic per month** (e.g. one “Transport exceeded” per month). To see the same type again in the same month, you’d need to use a different category or wait for next month (or use the sample endpoint).
- **Dashboard must be loaded** (or refreshed) for new budget-based notifications to be created.
- Use the **All** / **Unread** tabs and **Mark all as read** to verify read state and badges.
