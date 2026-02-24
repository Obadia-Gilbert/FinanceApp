using FinanceApp.Application.Interfaces.Services;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using FinanceApp.Web.Models;
using FinanceApp.Infrastructure.Identity; // for ApplicationUser
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using FinanceApp.Application.Interfaces;
using ClosedXML.Excel;
using System.IO;

namespace FinanceApp.Web.Controllers;

[Authorize]
public class ExpenseController : Controller
{
    private readonly IExpenseService _expenseService;
    private readonly IRepository<Category> _categoryRepository;
    private readonly UserManager<ApplicationUser> _userManager;

    public ExpenseController(
        IExpenseService expenseService,
        IRepository<Category> categoryRepository,
        UserManager<ApplicationUser> userManager)
    {
        _expenseService = expenseService;
        _categoryRepository = categoryRepository;
        _userManager = userManager;
    }

    // GET: /Expense
    public async Task<IActionResult> Index(int pageNumber = 1, int pageSize = 10)
    {
        var userId = _userManager.GetUserId(User);
        if (userId == null) return Unauthorized();

        var pagedExpenses = await _expenseService.GetPagedExpensesAsync(
            pageNumber,
            pageSize,
            e => e.UserId == userId,           // show only current user's expenses
            q => q.OrderByDescending(e => e.ExpenseDate)
        );

        return View(pagedExpenses);
    }

    // GET: /Expense/Create
    public async Task<IActionResult> Create()
    {
        ViewBag.Categories = await _categoryRepository.GetAllAsync();
        ViewBag.Currencies = Enum.GetValues(typeof(Currency));
        return View();
    }

    // POST: /Expense/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ExpenseCreateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Categories = await _categoryRepository.GetAllAsync();
            ViewBag.Currencies = Enum.GetValues(typeof(Currency));
            return View(model);
        }

        var userId = _userManager.GetUserId(User);
        if (userId == null) return Unauthorized();

        // Handle file upload
        string? receiptPath = null;
        if (model.ReceiptFile != null && model.ReceiptFile.Length > 0)
        {
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/receipts");
            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

            var category = await _categoryRepository.GetByIdAsync(model.CategoryId);
            var categoryName = category?.Name.Replace(" ", "-") ?? "Unknown";
            var dateString = model.ExpenseDate.ToString("yyyy-MM-dd");
            var expenseId = Guid.NewGuid();
            var fileExtension = Path.GetExtension(model.ReceiptFile.FileName);
            var fileName = $"Expense-{categoryName}-{dateString}-{expenseId}{fileExtension}";
            var filePath = Path.Combine(uploadsFolder, fileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await model.ReceiptFile.CopyToAsync(fileStream);
            }

            receiptPath = $"/uploads/receipts/{fileName}";
        }

        var expense = await _expenseService.CreateExpenseAsync(
            model.Amount,
            model.Currency,
            model.ExpenseDate,
            model.CategoryId,
            userId,
            model.Description,
            receiptPath
        );

        return RedirectToAction(nameof(Index));
    }

    // GET: /Expense/Edit/{id}
    public async Task<IActionResult> Edit(Guid id)
    {
        var expense = await _expenseService.GetByIdAsync(id);
        if (expense == null) return NotFound();

        var model = new ExpenseEditViewModel
        {
            Id = expense.Id,
            Amount = expense.Amount,
            Currency = expense.Currency,
            ExpenseDate = expense.ExpenseDate,
            CategoryId = expense.CategoryId,
            Description = expense.Description,
            ReceiptPath = expense.ReceiptPath
        };

        ViewBag.Categories = await _categoryRepository.GetAllAsync();
        ViewBag.Currencies = Enum.GetValues(typeof(Currency));

        return View(model);
    }

    // POST: /Expense/Edit/{id}
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ExpenseEditViewModel model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Categories = await _categoryRepository.GetAllAsync();
            ViewBag.Currencies = Enum.GetValues(typeof(Currency));
            return View(model);
        }

        var expense = await _expenseService.GetByIdAsync(model.Id);
        if (expense == null) return NotFound();

        expense.UpdateDescription(model.Description ?? "");
        expense.UpdateReceipt(model.ReceiptPath ?? "");
        expense.UpdateCategory(model.CategoryId);
        expense.UpdateCurrency(model.Currency);
        expense.UpdateExpenseDate(model.ExpenseDate);
        expense.UpdateAmount(model.Amount);

        await _expenseService.UpdateExpenseAsync(expense);

        return RedirectToAction(nameof(Index));
    }

    // GET: /Expense/Delete/{id}
    public async Task<IActionResult> Delete(Guid id)
    {
        var expense = await _expenseService.GetByIdAsync(id);
        if (expense == null) return NotFound();

        var vm = new ExpenseDeleteViewModel
        {
            Id = expense.Id,
            Description = expense.Description,
            Currency = expense.Currency,
            CategoryName = (await _categoryRepository.GetByIdAsync(expense.CategoryId))?.Name ?? "",
            Amount = expense.Amount,
            ExpenseDate = expense.ExpenseDate
        };

        return View(vm);
    }

    // POST: /Expense/Delete/{id}
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        await _expenseService.SoftDeleteExpenseAsync(id);
        return RedirectToAction(nameof(Index));
    }

    // GET: /Expense/DownloadExcel
   public async Task<IActionResult> DownloadExcel()
{
    var expenses = await _expenseService.GetAllAsync();

    using var workbook = new XLWorkbook();
    var worksheet = workbook.Worksheets.Add("Expenses");

    // Headers
    worksheet.Cell(1, 1).Value = "Description";
    worksheet.Cell(1, 2).Value = "Amount";
    worksheet.Cell(1, 3).Value = "Currency";
    worksheet.Cell(1, 4).Value = "Category";
    worksheet.Cell(1, 5).Value = "Expense Date";

    var row = 2;
    foreach (var expense in expenses)
    {
        worksheet.Cell(row, 1).Value = expense.Description;
        worksheet.Cell(row, 2).Value = expense.Amount;
        worksheet.Cell(row, 3).Value = expense.Currency.ToString();
        worksheet.Cell(row, 4).Value = expense.Category?.Name;
        worksheet.Cell(row, 5).Value = expense.ExpenseDate.ToString("yyyy-MM-dd");
        row++;
    }

    using var stream = new MemoryStream();
    workbook.SaveAs(stream);
    var content = stream.ToArray();

    return File(
        content,
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        "Expenses.xlsx"
    );
    }
}

// ==============================   
/*
    Notes:
    - For simplicity, file uploads are stored in wwwroot/uploads/receipts. In production, consider using cloud storage (e.g., Azure Blob Storage, AWS S3).
    - The DownloadExcel action generates an Excel file with all expenses. In a real app, you might want to add filters (e.g., by date range, category) before exporting.
    - The Create and Edit views should have forms that bind to ExpenseCreateViewModel and ExpenseEditViewModel respectively, including file upload fields for receipts.
    - Ensure that the necessary client-side validation and user feedback are implemented in the views for a better user experience.
*/

// ==============================
// The above code is a complete implementation of the ExpenseController with CRUD operations, file upload handling, and Excel export functionality. It interacts with the IExpenseService for business logic and uses repositories to fetch related data like categories. The views (not shown here) should be designed to work with the provided view models and support the necessary form fields and actions.

// ==============================
// The following is improved DownloadExcel method that generates an Excel file with all expenses, including their descriptions, amounts, currencies, categories, and expense dates. The file is returned as a downloadable response to the user.
/* public async Task<IActionResult> DownloadExcel()
{
    var expenses = await _expenseService.GetAllAsync();

    using var workbook = new XLWorkbook();
    var worksheet = workbook.Worksheets.Add("Expenses");

    // ===== Header Row =====
    worksheet.Cell(1, 1).Value = "Description";
    worksheet.Cell(1, 2).Value = "Amount";
    worksheet.Cell(1, 3).Value = "Currency";
    worksheet.Cell(1, 4).Value = "Category";
    worksheet.Cell(1, 5).Value = "Expense Date";

    var headerRange = worksheet.Range(1, 1, 1, 5);
    headerRange.Style.Font.Bold = true;
    headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
    headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

    // ===== Data Rows =====
    int row = 2;
    foreach (var expense in expenses)
    {
        worksheet.Cell(row, 1).Value = expense.Description;

        worksheet.Cell(row, 2).Value = expense.Amount;
        worksheet.Cell(row, 2).Style.NumberFormat.Format = "#,##0.00";

        worksheet.Cell(row, 3).Value = expense.Currency.ToString();
        worksheet.Cell(row, 4).Value = expense.Category?.Name;

        worksheet.Cell(row, 5).Value = expense.ExpenseDate;
        worksheet.Cell(row, 5).Style.DateFormat.Format = "yyyy-MM-dd";

        row++;
    }

    // ===== Create Table Style =====
    var tableRange = worksheet.Range(1, 1, row - 1, 5);
    var table = tableRange.CreateTable();
    table.Theme = XLTableTheme.TableStyleMedium2;

    // ===== Auto-fit Columns =====
    worksheet.Columns().AdjustToContents();

    // ===== Freeze Header Row =====
    worksheet.SheetView.FreezeRows(1);

    using var stream = new MemoryStream();
    workbook.SaveAs(stream);
    var content = stream.ToArray();

    return File(
        content,
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        $"Expenses_{DateTime.Now:yyyyMMdd}.xlsx"
    );
} */