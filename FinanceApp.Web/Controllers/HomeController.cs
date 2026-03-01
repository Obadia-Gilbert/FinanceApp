using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using FinanceApp.Web.Models;
using FinanceApp.Infrastructure.Identity;
using FinanceApp.Application.Interfaces.Services;
using System.Text.Json;

namespace FinanceApp.Web.Controllers;

[Authorize]
public class HomeController : Controller
{
    private readonly IExpenseService _expenseService;
    private readonly ICategoryService _categoryService;
    private readonly UserManager<ApplicationUser> _userManager;

    public HomeController(
        IExpenseService expenseService,
        ICategoryService categoryService,
        UserManager<ApplicationUser> userManager)
    {
        _expenseService = expenseService;
        _categoryService = categoryService;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var userId = _userManager.GetUserId(User);
        if (userId == null) return Unauthorized();

        // Fetch all user expenses
        var allExpenses = await _expenseService.GetPagedExpensesAsync(
            pageNumber: 1,
            pageSize: 1000,
            filter: e => e.UserId == userId,
            orderBy: q => q.OrderByDescending(e => e.ExpenseDate)
        );

        var expenses = allExpenses.Items.ToList();

        // Get user's categories (all categories assigned to this user, not just ones used)
        var pagedCategories = await _categoryService.GetPagedCategoriesAsync(pageNumber: 1, pageSize: 100, userId: userId);
        var categoryCount = pagedCategories.TotalItems;

        // Calculate dashboard metrics
        var totalSpend = expenses.Sum(e => e.Amount);
        var expenseCount = expenses.Count;
        var currentMonth = DateTime.Now.Month;
        var currentYear = DateTime.Now.Year;
        var thisMonthExpense = expenses
            .Where(e => e.ExpenseDate.Month == currentMonth && e.ExpenseDate.Year == currentYear)
            .Sum(e => e.Amount);

        // Group expenses by date for trend chart (last 30 days)
        var last30Days = DateTime.Now.AddDays(-30);
        var last30Expenses = expenses
            .Where(e => e.ExpenseDate >= last30Days)
            .GroupBy(e => e.ExpenseDate.Date)
            .OrderBy(g => g.Key)
            .Select(g => new { Date = g.Key.ToString("MMM dd"), Amount = g.Sum(e => e.Amount) })
            .ToList();

        // Pass data to view via ViewBag
        ViewBag.TotalSpend = totalSpend.ToString("F2");
        ViewBag.ExpenseCount = expenseCount;
        ViewBag.CategoryCount = categoryCount;
        ViewBag.ThisMonthSpend = thisMonthExpense.ToString("F2");
        ViewBag.ChartLabels = JsonSerializer.Serialize(last30Expenses.Select(x => x.Date).ToList());
        ViewBag.ChartData = JsonSerializer.Serialize(last30Expenses.Select(x => x.Amount).ToList());

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
