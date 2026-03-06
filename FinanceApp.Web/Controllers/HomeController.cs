using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using FinanceApp.Web.Models;
using FinanceApp.Infrastructure.Identity;
using FinanceApp.Application.Interfaces.Services;
using System.Text.Json;
using FinanceApp.Domain.Enums;

namespace FinanceApp.Web.Controllers;

[Authorize]
public class HomeController : Controller
{
    private readonly IExpenseService _expenseService;
    private readonly IExpenseQueryService _expenseQueryService;
    private readonly ICategoryService _categoryService;
    private readonly IBudgetService _budgetService;
    private readonly ICategoryBudgetService _categoryBudgetService;
    private readonly IAccountService _accountService;
    private readonly ITransactionService _transactionService;
    private readonly ICurrencyConversionService _currencyConversion;
    private readonly IBudgetNotificationService _budgetNotificationService;
    private readonly UserManager<ApplicationUser> _userManager;

    public HomeController(
        IExpenseService expenseService,
        IExpenseQueryService expenseQueryService,
        ICategoryService categoryService,
        IBudgetService budgetService,
        ICategoryBudgetService categoryBudgetService,
        IAccountService accountService,
        ITransactionService transactionService,
        ICurrencyConversionService currencyConversion,
        IBudgetNotificationService budgetNotificationService,
        UserManager<ApplicationUser> userManager)
    {
        _expenseService = expenseService;
        _expenseQueryService = expenseQueryService;
        _categoryService = categoryService;
        _budgetService = budgetService;
        _categoryBudgetService = categoryBudgetService;
        _accountService = accountService;
        _transactionService = transactionService;
        _currencyConversion = currencyConversion;
        _budgetNotificationService = budgetNotificationService;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var userId = _userManager.GetUserId(User);
        if (userId == null) return Unauthorized();

        var now          = DateTime.Now;
        var currentMonth = now.Month;
        var currentYear  = now.Year;
        var lastMonth    = currentMonth == 1 ? 12 : currentMonth - 1;
        var lastMonthYear = currentMonth == 1 ? currentYear - 1 : currentYear;
        var localOffset   = TimeZoneInfo.Local.GetUtcOffset(now);
        var last30Start   = new DateTimeOffset(now.Date.AddDays(-29), localOffset);
        var last30End     = new DateTimeOffset(now.Date.AddDays(1), localOffset);

        // Aggregated and bounded queries (sequential: DbContext is not thread-safe)
        var totalsByCurrency = await _expenseQueryService.GetTotalsByCurrencyAsync(userId);
        var thisMonthTotals  = await _expenseQueryService.GetMonthTotalsByCurrencyAsync(userId, currentMonth, currentYear);
        var lastMonthTotals  = await _expenseQueryService.GetMonthTotalsByCurrencyAsync(userId, lastMonth, lastMonthYear);
        var trendSums        = await _expenseQueryService.GetSumsByDayAsync(userId, last30Start, last30End, null);
        var categoryTotals   = await _expenseQueryService.GetCategoryTotalsForMonthAsync(userId, currentMonth, currentYear, null);
        var recentExpenses   = await _expenseQueryService.GetRecentExpensesAsync(userId, 8);
        var expenseCount     = await _expenseQueryService.GetTotalCountAsync(userId);

        // Display currency = currency with highest total spend
        var displayCurrency = totalsByCurrency.Count > 0
            ? totalsByCurrency.OrderByDescending(kv => kv.Value).Select(kv => kv.Key).First()
            : Currency.TZS;
        var displayCurrencyStr = displayCurrency.ToString();

        var totalSpend = totalsByCurrency.GetValueOrDefault(displayCurrency, 0);
        var thisMonthSpend = thisMonthTotals.GetValueOrDefault(displayCurrency, 0);
        var lastMonthSpend = lastMonthTotals.GetValueOrDefault(displayCurrency, 0);
        var thisMonthExpenseCount = await _expenseQueryService.GetMonthExpenseCountAsync(userId, currentMonth, currentYear);
        var lastMonthCount = await _expenseQueryService.GetMonthExpenseCountAsync(userId, lastMonth, lastMonthYear);

        decimal? momSpendChange = lastMonthSpend > 0 ? Math.Round((thisMonthSpend - lastMonthSpend) / lastMonthSpend * 100, 1) : (decimal?)null;
        decimal? momCountChange = lastMonthCount > 0 ? Math.Round(((decimal)(thisMonthExpenseCount - lastMonthCount)) / lastMonthCount * 100, 1) : (decimal?)null;

        var daysPassed = Math.Max(1, now.Day);
        var avgDailySpend = daysPassed > 0 ? Math.Round(thisMonthSpend / daysPassed, 0) : 0m;
        var lastMonthDays = DateTime.DaysInMonth(lastMonthYear, lastMonth);
        var avgDailyLastMonth = lastMonthDays > 0 ? Math.Round(lastMonthSpend / lastMonthDays, 0) : 0m;
        decimal? momAvgChange = avgDailyLastMonth > 0 ? Math.Round((avgDailySpend - avgDailyLastMonth) / avgDailyLastMonth * 100, 1) : (decimal?)null;

        // Category breakdown top 6 (by display currency if available in categoryTotals; we got all currencies)
        var categoryTotalsInDisplay = await _expenseQueryService.GetCategoryTotalsForMonthAsync(userId, currentMonth, currentYear, displayCurrency);
        var categoryBreakdown = categoryTotalsInDisplay.Take(6).Select(x => new { Category = x.CategoryName ?? "Other", Amount = x.Sum }).ToList();

        // Trend chart: 30 days (fill missing days with 0)
        var trendByDate = trendSums.ToDictionary(s => s.Date, s => s.Sum);
        var trendData = Enumerable.Range(0, 30)
            .Select(i => now.Date.AddDays(-29 + i))
            .Select(d => new { Date = d.ToString("MMM dd"), Amount = trendByDate.GetValueOrDefault(d, 0m) })
            .ToList();

        // Budget
        var currentMonthBudget = await _budgetService.GetBudgetForMonthAsync(userId, currentMonth, currentYear);
        decimal? budgetAmount = currentMonthBudget?.Amount;
        var budgetCurrency = currentMonthBudget?.Currency ?? displayCurrency;
        var thisMonthSpendInBudgetCurrency = thisMonthTotals.GetValueOrDefault(budgetCurrency, 0);
        var isOverBudget = budgetAmount.HasValue && budgetAmount.Value > 0 && thisMonthSpendInBudgetCurrency >= budgetAmount.Value;
        decimal budgetPct = budgetAmount.HasValue && budgetAmount.Value > 0 ? Math.Min(100, Math.Round(thisMonthSpendInBudgetCurrency / budgetAmount.Value * 100, 1)) : 0m;

        // Category budget alerts (batch spend lookup, then loop only for notifications)
        var categorySpendForMonth = await _categoryBudgetService.GetCategorySpendForMonthAsync(userId, currentMonth, currentYear);
        var categoryBudgets = await _categoryBudgetService.GetForMonthAsync(userId, currentMonth, currentYear);
        var categoryBudgetAlerts = new List<CategoryBudgetAlertViewModel>();
        foreach (var cb in categoryBudgets)
        {
            var spent = categorySpendForMonth.GetValueOrDefault((cb.CategoryId, cb.Currency), 0);
            var catName = cb.Category?.Name ?? "Unknown";
            if (spent >= cb.Amount)
                categoryBudgetAlerts.Add(new CategoryBudgetAlertViewModel { CategoryName = catName, Spent = spent, Budget = cb.Amount, Currency = cb.Currency.ToString(), IsOver = true });
            else if (cb.Amount > 0 && spent >= cb.Amount * 0.8m)
                categoryBudgetAlerts.Add(new CategoryBudgetAlertViewModel { CategoryName = catName, Spent = spent, Budget = cb.Amount, Currency = cb.Currency.ToString(), IsOver = false });
        }

        await _budgetNotificationService.EvaluateAndCreateNotificationsAsync(userId, currentMonth, currentYear);

        // ── Account balances & total balance (in display currency only; no conversion) ─
        var accounts = (await _accountService.GetAllAsync(userId)).Where(a => a.IsActive).ToList();
        var accountCards = new List<(string Name, string Type, decimal Balance, string Currency)>();
        decimal totalBalanceInDisplayCurrency = 0;
        foreach (var acc in accounts)
        {
            var bal = await _accountService.GetBalanceAsync(acc.Id, userId);
            if (acc.Currency == displayCurrency)
                totalBalanceInDisplayCurrency += bal;
            if (accountCards.Count < 4)
                accountCards.Add((acc.Name, acc.Type.ToString(), bal, acc.Currency.ToString()));
        }

        // ── Monthly income (transactions Type == Income) ───────────────
        var transactionsThisMonth = await _transactionService.GetPagedAsync(userId, 1, 500,
            t => t.Date.Month == currentMonth && t.Date.Year == currentYear);
        var monthlyIncome = transactionsThisMonth.Items
            .Where(t => t.Type == Domain.Enums.TransactionType.Income && t.Currency == displayCurrency)
            .Sum(t => t.Amount);
        var transactionsLastMonth = await _transactionService.GetPagedAsync(userId, 1, 500,
            t => t.Date.Month == lastMonth && t.Date.Year == lastMonthYear);
        var lastMonthIncome = transactionsLastMonth.Items
            .Where(t => t.Type == Domain.Enums.TransactionType.Income && t.Currency == displayCurrency)
            .Sum(t => t.Amount);
        decimal? momIncomeChange = lastMonthIncome > 0
            ? Math.Round((monthlyIncome - lastMonthIncome) / lastMonthIncome * 100, 1)
            : (decimal?)null;

        // ── Active budgets count ──────────────────────────────────────
        var activeBudgetsCount = categoryBudgets.Count() + (currentMonthBudget != null ? 1 : 0);

        // ── Six-month cash flow (for bar chart) ───────────────────────
        var sixMonthLabels = new List<string>();
        var sixMonthData = new List<decimal>();
        for (var i = 5; i >= 0; i--)
        {
            var d = now.AddMonths(-i);
            var m = d.Month;
            var y = d.Year;
            sixMonthLabels.Add(d.ToString("MMM", System.Globalization.CultureInfo.InvariantCulture).ToUpperInvariant());
            var monthExpenseTotals = await _expenseQueryService.GetMonthTotalsByCurrencyAsync(userId, m, y);
            var monthExpenses = monthExpenseTotals.GetValueOrDefault(displayCurrency, 0);
            var monthTx = await _transactionService.GetPagedAsync(userId, 1, 500, t => t.Date.Month == m && t.Date.Year == y);
            var monthIncome = monthTx.Items.Where(t => t.Type == Domain.Enums.TransactionType.Income && t.Currency == displayCurrency).Sum(t => t.Amount);
            var monthExpenseTx = monthTx.Items.Where(t => t.Type == Domain.Enums.TransactionType.Expense && t.Currency == displayCurrency).Sum(t => t.Amount);
            sixMonthData.Add(monthIncome - monthExpenses - monthExpenseTx);
        }

        // ── Recent activity (expenses + transactions, by category & type) ─
        var recentActivity = new List<FinanceApp.Web.Models.RecentActivityViewModel>();
        foreach (var e in recentExpenses.Take(5).ToList())
        {
            recentActivity.Add(new FinanceApp.Web.Models.RecentActivityViewModel
            {
                Date = e.ExpenseDate.DateTime,
                CategoryName = e.Category?.Name ?? "Expense",
                Amount = e.Amount,
                Currency = e.Currency.ToString(),
                IsIncome = false
            });
        }
        var recentTx = await _transactionService.GetPagedAsync(userId, 1, 10);
        foreach (var t in recentTx.Items.Where(t => t.Type != Domain.Enums.TransactionType.Transfer))
        {
            recentActivity.Add(new FinanceApp.Web.Models.RecentActivityViewModel
            {
                Date = t.Date.DateTime,
                CategoryName = t.Category?.Name ?? t.Type.ToString(),
                Amount = t.Type == Domain.Enums.TransactionType.Income ? t.Amount : -t.Amount,
                Currency = t.Currency.ToString(),
                IsIncome = t.Type == Domain.Enums.TransactionType.Income
            });
        }
        recentActivity = recentActivity.OrderByDescending(x => x.Date).Take(10).ToList();

        // ── Categories count ──────────────────────────────────────────
        var pagedCategories = await _categoryService.GetPagedCategoriesAsync(pageNumber: 1, pageSize: 100, userId: userId);

        var currentUser = await _userManager.GetUserAsync(User);
        var userFirstName = currentUser?.FirstName ?? currentUser?.Email?.Split('@')[0] ?? "User";

        // ── ViewBag ───────────────────────────────────────────────────
        ViewBag.DisplayCurrency   = displayCurrencyStr;
        ViewBag.TotalSpend        = totalSpend;
        ViewBag.ExpenseCount      = expenseCount;
        ViewBag.CategoryCount     = pagedCategories.TotalItems;

        ViewBag.CurrentMonthName  = now.ToString("MMMM");
        ViewBag.CurrentMonthShort = now.ToString("MMM").ToUpper();
        ViewBag.CurrentMonthYear  = now.Year;

        ViewBag.ThisMonthSpend    = thisMonthSpend;
        ViewBag.LastMonthSpend    = lastMonthSpend;
        ViewBag.MoMSpendChange    = momSpendChange;
        ViewBag.MoMCountChange    = momCountChange;

        ViewBag.AvgDailySpend     = avgDailySpend;
        ViewBag.MoMAvgChange      = momAvgChange;

        ViewBag.ChartLabels       = JsonSerializer.Serialize(trendData.Select(x => x.Date).ToList());
        ViewBag.ChartData         = JsonSerializer.Serialize(trendData.Select(x => x.Amount).ToList());
        ViewBag.ChartCurrency     = displayCurrencyStr;

        ViewBag.DonutLabels       = JsonSerializer.Serialize(categoryBreakdown.Select(x => x.Category).ToList());
        ViewBag.DonutData         = JsonSerializer.Serialize(categoryBreakdown.Select(x => x.Amount).ToList());

        ViewBag.RecentExpenses    = recentExpenses;

        ViewBag.BudgetAmount      = budgetAmount;
        ViewBag.BudgetCurrency    = budgetCurrency.ToString();
        ViewBag.BudgetPct         = budgetPct;
        ViewBag.ThisMonthSpendInBudgetCurrency = thisMonthSpendInBudgetCurrency;
        ViewBag.IsOverBudget      = isOverBudget;
        ViewBag.CategoryBudgetAlerts = categoryBudgetAlerts;

        ViewBag.AccountCards      = accountCards;
        ViewBag.UserFirstName    = userFirstName;
        ViewBag.TotalBalance     = totalBalanceInDisplayCurrency;
        ViewBag.MonthlyIncome    = monthlyIncome;
        ViewBag.MoMIncomeChange  = momIncomeChange;
        ViewBag.ActiveBudgetsCount = activeBudgetsCount;
        ViewBag.CashFlowSixMonthLabels = JsonSerializer.Serialize(sixMonthLabels);
        ViewBag.CashFlowSixMonthData   = JsonSerializer.Serialize(sixMonthData);
        ViewBag.RecentActivity   = recentActivity;
        var lastMonthSpendForCf = lastMonthSpend;
        var cfThis = monthlyIncome - thisMonthSpend;
        var cfLast = lastMonthIncome - lastMonthSpendForCf;
        decimal? cashFlowMoMChange = cfLast != 0 ? Math.Round((cfThis - cfLast) / Math.Abs(cfLast) * 100, 0) : (decimal?)null;
        ViewBag.CashFlowThisMonth = cfThis;
        ViewBag.CashFlowMoMChange = cashFlowMoMChange;

        return View();
    }

    [AllowAnonymous]
    public IActionResult Privacy()
    {
        return View();
    }

    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
