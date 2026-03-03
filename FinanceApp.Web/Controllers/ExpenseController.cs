using FinanceApp.Application.Interfaces.Services;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;
using FinanceApp.Web.Models;
using Microsoft.AspNetCore.Mvc;
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
    private readonly ICategoryService _categoryService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ISupportingDocumentService _documentService;

    public ExpenseController(
        IExpenseService expenseService,
        ICategoryService categoryService,
        UserManager<ApplicationUser> userManager,
        ISupportingDocumentService documentService)
    {
        _expenseService = expenseService;
        _categoryService = categoryService;
        _userManager = userManager;
        _documentService = documentService;
    }

    // GET: /Expense
    // Loads ALL user expenses — DataTables handles client-side pagination/search/sort.
    public async Task<IActionResult> Index()
    {
        var userId = _userManager.GetUserId(User);
        if (userId == null) return Unauthorized();

        var allExpenses = await _expenseService.GetPagedExpensesAsync(
            pageNumber: 1,
            pageSize: int.MaxValue,
            filter: e => e.UserId == userId,
            orderBy: q => q.OrderByDescending(e => e.ExpenseDate)
        );

        return View(allExpenses);
    }

    // GET: /Expense/Create
    // Accepts ?partial=true or AJAX requests to return a layout-less partial for offcanvas
    public async Task<IActionResult> Create(bool partial = false)
    {
        var userId = _userManager.GetUserId(User);
        if (userId == null) return Unauthorized();

        var model = new FinanceApp.Web.Models.ExpenseCreateViewModel
        {
            ExpenseDate = DateTime.Today
        };

        ViewBag.Categories = await _categoryService.GetCategoriesForExpenseAsync(userId);
        ViewBag.Currencies = Enum.GetValues(typeof(Currency));

        bool isAjax = partial ||
                     string.Equals(Request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase);
        if (isAjax)
        {
            return PartialView("_ExpenseCreatePartial", model);
        }

        return View(model);
    }

    // POST: /Expense/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ExpenseCreateViewModel model)
    {
        bool isAjax = string.Equals(Request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase);
        var userId = _userManager.GetUserId(User);
        if (userId == null) return Unauthorized();

        if (!ModelState.IsValid)
        {
            ViewBag.Categories = await _categoryService.GetCategoriesForExpenseAsync(userId);
            ViewBag.Currencies = Enum.GetValues(typeof(Currency));
            if (isAjax)
            {
                return PartialView("_ExpenseCreatePartial", model);
            }

            return View(model);
        }

        // Handle optional receipt upload (images + PDF, max 5MB)
        string? receiptPath = null;
        if (model.ReceiptFile != null && model.ReceiptFile.Length > 0)
        {
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".pdf" };
            var ext = Path.GetExtension(model.ReceiptFile.FileName)?.ToLowerInvariant();
            if (string.IsNullOrEmpty(ext) || !allowedExtensions.Contains(ext))
            {
                ModelState.AddModelError(nameof(model.ReceiptFile), "Receipt must be JPG, PNG, GIF, WebP or PDF.");
            }
            else if (model.ReceiptFile.Length > 5 * 1024 * 1024) // 5MB
            {
                ModelState.AddModelError(nameof(model.ReceiptFile), "Receipt must be 5MB or less.");
            }
            else
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/receipts");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                var category = await _categoryService.GetByIdAsync(model.CategoryId, userId);
                var categoryName = category?.Name.Replace(" ", "-") ?? "Unknown";
                var dateString = model.ExpenseDate.ToString("yyyy-MM-dd");
                var expenseId = Guid.NewGuid();
                var fileName = $"Expense-{categoryName}-{dateString}-{expenseId}{ext}";
                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await model.ReceiptFile.CopyToAsync(fileStream);
                }

                receiptPath = $"/uploads/receipts/{fileName}";
            }
        }
        if (!ModelState.IsValid)
        {
            ViewBag.Categories = await _categoryService.GetCategoriesForExpenseAsync(userId);
            ViewBag.Currencies = Enum.GetValues(typeof(Currency));
            if (isAjax) return PartialView("_ExpenseCreatePartial", model);
            return View(model);
        }

        var expense = await _expenseService.CreateExpenseAsync(
            model.Amount,
            model.Currency,
            model.ExpenseDate,
            model.CategoryId,
            userId,
            model.Description ?? "",
            receiptPath
        );

        if (isAjax)
        {
            return Json(new { success = true });
        }

        return RedirectToAction(nameof(Index));
    }

    // GET: /Expense/Edit/{id}
    // Optional ?partial=true or AJAX header to return layout‑less form for offcanvas
    public async Task<IActionResult> Edit(Guid id, bool partial = false)
    {
        var userId = _userManager.GetUserId(User);
        if (userId == null) return Unauthorized();

        var expense = await _expenseService.GetByIdAsync(id);
        if (expense == null) return NotFound();
        if (expense.UserId != userId) return Forbid();

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

        ViewBag.Categories = await _categoryService.GetCategoriesForExpenseAsync(userId);
        ViewBag.Currencies = Enum.GetValues(typeof(Currency));
        ViewBag.SupportingDocs = await _documentService.GetForEntityAsync(
            DocumentEntityType.Expense, id, userId);

        bool isAjax = partial ||
                     string.Equals(Request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase);
        if (isAjax)
        {
            return PartialView("_ExpenseEditPartial", model);
        }

        return View(model);
    }

    // POST: /Expense/Edit/{id}
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ExpenseEditViewModel model)
    {
        bool isAjax = string.Equals(Request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase);
        var userId = _userManager.GetUserId(User);
        if (userId == null) return Unauthorized();

        if (!ModelState.IsValid)
        {
            ViewBag.Categories = await _categoryService.GetCategoriesForExpenseAsync(userId);
            ViewBag.Currencies = Enum.GetValues(typeof(Currency));
            if (isAjax)
            {
                return PartialView("_ExpenseEditPartial", model);
            }

            return View(model);
        }

        var expense = await _expenseService.GetByIdAsync(model.Id);
        if (expense == null) return NotFound();
        var userIdCheck = _userManager.GetUserId(User);
        if (userIdCheck == null || expense.UserId != userIdCheck) return Forbid();

        // Handle optional receipt replacement
        var receiptPath = expense.ReceiptPath;
        if (model.ReceiptFile != null && model.ReceiptFile.Length > 0)
        {
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".pdf" };
            var ext = Path.GetExtension(model.ReceiptFile.FileName)?.ToLowerInvariant();
            if (!string.IsNullOrEmpty(ext) && allowedExtensions.Contains(ext) && model.ReceiptFile.Length <= 5 * 1024 * 1024)
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/receipts");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);
                var category = await _categoryService.GetByIdAsync(model.CategoryId, userIdCheck);
                var categoryName = category?.Name.Replace(" ", "-") ?? "Unknown";
                var dateString = model.ExpenseDate.ToString("yyyy-MM-dd");
                var fileName = $"Expense-{categoryName}-{dateString}-{expense.Id}{ext}";
                var filePath = Path.Combine(uploadsFolder, fileName);
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await model.ReceiptFile.CopyToAsync(fileStream);
                }
                receiptPath = $"/uploads/receipts/{fileName}";
            }
        }

        expense.UpdateDescription(model.Description ?? "");
        expense.UpdateReceipt(receiptPath ?? "");
        expense.UpdateCategory(model.CategoryId);
        expense.UpdateCurrency(model.Currency);
        expense.UpdateExpenseDate(model.ExpenseDate);
        expense.UpdateAmount(model.Amount);

        await _expenseService.UpdateExpenseAsync(expense);

        if (isAjax)
        {
            return Json(new { success = true });
        }

        return RedirectToAction(nameof(Index));
    }

    // GET: /Expense/Delete/{id}
    public async Task<IActionResult> Delete(Guid id, bool partial = false)
    {
        var userId = _userManager.GetUserId(User);
        if (userId == null) return Unauthorized();

        var expense = await _expenseService.GetByIdAsync(id);
        if (expense == null) return NotFound();
        if (expense.UserId != userId) return Forbid();

        var vm = new ExpenseDeleteViewModel
        {
            Id = expense.Id,
            Description = expense.Description,
            Currency = expense.Currency,
            CategoryName = (await _categoryService.GetByIdAsync(expense.CategoryId, userId!))?.Name ?? "",
            Amount = expense.Amount,
            ExpenseDate = expense.ExpenseDate
        };

        bool isAjax = partial || string.Equals(Request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase);
        if (isAjax)
        {
            return PartialView("_ExpenseDeletePartial", vm);
        }

        return View(vm);
    }

    // POST: /Expense/Delete/{id}
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        var userId = _userManager.GetUserId(User);
        if (userId == null) return Unauthorized();

        var expense = await _expenseService.GetByIdAsync(id);
        if (expense == null) return NotFound();
        if (expense.UserId != userId) return Forbid();

        bool isAjax = string.Equals(Request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase);
        await _expenseService.SoftDeleteExpenseAsync(id);
        if (isAjax)
        {
            return Json(new { success = true });
        }

        return RedirectToAction(nameof(Index));
    }

    // GET: /Expense/Receipt/{id} — serve receipt with ownership check
    [HttpGet]
    public async Task<IActionResult> Receipt(Guid id)
    {
        var expense = await _expenseService.GetByIdAsync(id);
        if (expense == null) return NotFound();
        var userId = _userManager.GetUserId(User);
        if (userId == null || expense.UserId != userId) return Forbid();

        if (string.IsNullOrEmpty(expense.ReceiptPath)) return NotFound();

        var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", expense.ReceiptPath.TrimStart('/'));
        if (!System.IO.File.Exists(fullPath)) return NotFound();

        var ext = Path.GetExtension(fullPath)?.ToLowerInvariant();
        var contentType = ext switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            ".pdf" => "application/pdf",
            _ => "application/octet-stream"
        };

        return PhysicalFile(fullPath, contentType, Path.GetFileName(fullPath));
    }

    // GET: /Expense/DownloadExcel
    public async Task<IActionResult> DownloadExcel()
    {
        var userId = _userManager.GetUserId(User);
        if (userId == null) return Unauthorized();

        var paged = await _expenseService.GetPagedExpensesAsync(
            pageNumber: 1,
            pageSize: int.MaxValue,
            filter: e => e.UserId == userId,
            orderBy: q => q.OrderByDescending(e => e.ExpenseDate));
        var expenses = paged.Items;

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
    //GET: /Expense/Category/{categoryId}
    public async Task<IActionResult> ByCategory(
     Guid categoryId,
     int pageNumber = 1,
     int pageSize = 10)
    {
        var userId = _userManager.GetUserId(User);
        if (userId == null)
            return Unauthorized();

        var expenses = await _expenseService.GetByCategoryIdAsync(
            categoryId,
            userId,
            pageNumber,
            pageSize);

        return View("Index", expenses);
    }

}