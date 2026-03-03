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
    private readonly ICategoryService _categoryService;
    private readonly IBudgetService _budgetService;
    private readonly ICategoryBudgetService _categoryBudgetService;
    private readonly IAccountService _accountService;
    private readonly ICurrencyConversionService _currencyConversion;
    private readonly UserManager<ApplicationUser> _userManager;

    public HomeController(
        IExpenseService expenseService,
        ICategoryService categoryService,
        IBudgetService budgetService,
        ICategoryBudgetService categoryBudgetService,
        IAccountService accountService,
        ICurrencyConversionService currencyConversion,
        UserManager<ApplicationUser> userManager)
    {
        _expenseService = expenseService;
        _categoryService = categoryService;
        _budgetService = budgetService;
        _categoryBudgetService = categoryBudgetService;
        _accountService = accountService;
        _currencyConversion = currencyConversion;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var userId = _userManager.GetUserId(User);
        if (userId == null) return Unauthorized();

        // ── All expenses ─────────────────────────────────────────────
        var allExpenses = await _expenseService.GetPagedExpensesAsync(
            pageNumber: 1,
            pageSize: int.MaxValue,
            filter: e => e.UserId == userId,
            orderBy: q => q.OrderByDescending(e => e.ExpenseDate)
        );
        var expenses = allExpenses.Items.ToList();

        // ── Display currency (highest-volume currency) ────────────────
        var displayCurrency = expenses.Count > 0
            ? expenses.GroupBy(e => e.Currency)
                      .OrderByDescending(g => g.Sum(x => x.Amount))
                      .Select(g => g.Key)
                      .First()
            : Currency.TZS;

        var displayCurrencyStr = displayCurrency.ToString();

        // ── Date helpers ──────────────────────────────────────────────
        var now          = DateTime.Now;
        var currentMonth = now.Month;
        var currentYear  = now.Year;
        var lastMonth    = currentMonth == 1 ? 12 : currentMonth - 1;
        var lastMonthYear = currentMonth == 1 ? currentYear - 1 : currentYear;

        // ── KPI: totals ───────────────────────────────────────────────
        var totalSpend = expenses
            .Where(e => e.Currency == displayCurrency)
            .Sum(e => e.Amount);

        // USD-equivalent totals (for multi-currency or cross-currency comparison)
        var totalSpendUsd = expenses.Sum(e => _currencyConversion.ConvertToUsd(e.Amount, e.Currency));

        var expenseCount = expenses.Count;

        var thisMonthExpenses = expenses
            .Where(e => e.Currency == displayCurrency
                     && e.ExpenseDate.Month == currentMonth
                     && e.ExpenseDate.Year == currentYear)
            .ToList();

        var thisMonthSpend = thisMonthExpenses.Sum(e => e.Amount);
        var thisMonthSpendUsd = thisMonthExpenses.Sum(e => _currencyConversion.ConvertToUsd(e.Amount, e.Currency));

        var lastMonthSpend = expenses
            .Where(e => e.Currency == displayCurrency
                     && e.ExpenseDate.Month == lastMonth
                     && e.ExpenseDate.Year == lastMonthYear)
            .Sum(e => e.Amount);

        var lastMonthCount = expenses
            .Count(e => e.ExpenseDate.Month == lastMonth && e.ExpenseDate.Year == lastMonthYear);

        // Month-over-month % change (null = no prior data)
        decimal? momSpendChange = lastMonthSpend > 0
            ? Math.Round((thisMonthSpend - lastMonthSpend) / lastMonthSpend * 100, 1)
            : (decimal?)null;

        decimal? momCountChange = lastMonthCount > 0
            ? Math.Round(((decimal)(thisMonthExpenses.Count - lastMonthCount)) / lastMonthCount * 100, 1)
            : (decimal?)null;

        // Average daily spend this month
        var daysPassed       = Math.Max(1, now.Day);
        var avgDailySpend    = daysPassed > 0 ? Math.Round(thisMonthSpend / daysPassed, 0) : 0m;
        var lastMonthDays    = DateTime.DaysInMonth(lastMonthYear, lastMonth);
        var avgDailyLastMonth = lastMonthDays > 0 ? Math.Round(lastMonthSpend / lastMonthDays, 0) : 0m;
        decimal? momAvgChange = avgDailyLastMonth > 0
            ? Math.Round((avgDailySpend - avgDailyLastMonth) / avgDailyLastMonth * 100, 1)
            : (decimal?)null;

        // ── Category breakdown (this month, display currency, top 6) ─
        var categoryBreakdown = thisMonthExpenses
            .GroupBy(e => e.Category?.Name ?? "Other")
            .Select(g => new { Category = g.Key, Amount = g.Sum(e => e.Amount) })
            .OrderByDescending(x => x.Amount)
            .Take(6)
            .ToList();

        // ── Trend chart: last 30 days grouped by date ─────────────────
        var last30Start = now.Date.AddDays(-29);
        var trendData = Enumerable.Range(0, 30)
            .Select(i => last30Start.AddDays(i))
            .Select(d => new
            {
                Date   = d.ToString("MMM dd"),
                Amount = expenses
                    .Where(e => e.Currency == displayCurrency && e.ExpenseDate.Date == d)
                    .Sum(e => e.Amount)
            })
            .ToList();

        // ── Recent expenses (last 8) ──────────────────────────────────
        var recentExpenses = expenses.Take(8).ToList();

        // ── Budget ────────────────────────────────────────────────────
        var currentMonthBudget = await _budgetService.GetBudgetForMonthAsync(userId, currentMonth, currentYear);
        decimal? budgetAmount   = currentMonthBudget?.Amount;
        var budgetCurrency      = currentMonthBudget?.Currency ?? displayCurrency;
        var thisMonthSpendInBudgetCurrency = expenses
            .Where(e => e.Currency == budgetCurrency
                     && e.ExpenseDate.Month == currentMonth
                     && e.ExpenseDate.Year == currentYear)
            .Sum(e => e.Amount);
        var isOverBudget = budgetAmount.HasValue && budgetAmount.Value > 0
                        && thisMonthSpendInBudgetCurrency >= budgetAmount.Value;
        decimal budgetPct = budgetAmount.HasValue && budgetAmount.Value > 0
            ? Math.Min(100, Math.Round(thisMonthSpendInBudgetCurrency / budgetAmount.Value * 100, 1))
            : 0m;

        // ── Category budgets alerts ───────────────────────────────────
        var categoryBudgets     = await _categoryBudgetService.GetForMonthAsync(userId, currentMonth, currentYear);
        var categoryBudgetAlerts = new List<CategoryBudgetAlertViewModel>();
        foreach (var cb in categoryBudgets)
        {
            var spent = await _categoryBudgetService.GetCategorySpendAsync(userId, cb.CategoryId, currentMonth, currentYear, cb.Currency);
            if (spent >= cb.Amount)
                categoryBudgetAlerts.Add(new CategoryBudgetAlertViewModel
                    { CategoryName = cb.Category?.Name ?? "Unknown", Spent = spent, Budget = cb.Amount, Currency = cb.Currency.ToString(), IsOver = true });
            else if (cb.Amount > 0 && spent >= cb.Amount * 0.8m)
                categoryBudgetAlerts.Add(new CategoryBudgetAlertViewModel
                    { CategoryName = cb.Category?.Name ?? "Unknown", Spent = spent, Budget = cb.Amount, Currency = cb.Currency.ToString(), IsOver = false });
        }

        // ── Account balances ──────────────────────────────────────────
        var accounts = (await _accountService.GetAllAsync(userId)).Where(a => a.IsActive).ToList();
        var accountCards = new List<(string Name, string Type, decimal Balance, string Currency)>();
        foreach (var acc in accounts.Take(4))
        {
            var bal = await _accountService.GetBalanceAsync(acc.Id, userId);
            accountCards.Add((acc.Name, acc.Type.ToString(), bal, acc.Currency.ToString()));
        }

        // ── Categories count ──────────────────────────────────────────
        var pagedCategories = await _categoryService.GetPagedCategoriesAsync(pageNumber: 1, pageSize: 100, userId: userId);

        // ── ViewBag ───────────────────────────────────────────────────
        ViewBag.DisplayCurrency   = displayCurrencyStr;
        ViewBag.TotalSpend        = totalSpend;
        ViewBag.TotalSpendUsd     = totalSpendUsd;
        ViewBag.ThisMonthSpendUsd = thisMonthSpendUsd;
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
