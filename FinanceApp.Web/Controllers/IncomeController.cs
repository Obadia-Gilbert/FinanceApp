using FinanceApp.Application.Interfaces.Services;
using FinanceApp.Domain.Enums;
using FinanceApp.Infrastructure.Identity;
using FinanceApp.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace FinanceApp.Web.Controllers;

[Authorize]
public class IncomeController : Controller
{
    private readonly IIncomeService _incomeService;
    private readonly IAccountService _accountService;
    private readonly ICategoryService _categoryService;
    private readonly ISupportingDocumentService _documentService;
    private readonly UserManager<ApplicationUser> _userManager;

    public IncomeController(
        IIncomeService incomeService,
        IAccountService accountService,
        ICategoryService categoryService,
        ISupportingDocumentService documentService,
        UserManager<ApplicationUser> userManager)
    {
        _incomeService = incomeService;
        _accountService = accountService;
        _categoryService = categoryService;
        _documentService = documentService;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index(int pageNumber = 1, int pageSize = 20)
    {
        var userId = _userManager.GetUserId(User);
        if (userId == null) return Unauthorized();

        var paged = await _incomeService.GetPagedAsync(userId, pageNumber, pageSize);
        var viewModels = paged.Items.Select(i => new IncomeListViewModel
        {
            Id = i.Id,
            AccountId = i.AccountId,
            AccountName = i.Account?.Name ?? "",
            CategoryId = i.CategoryId,
            CategoryName = i.Category?.Name ?? "",
            Amount = i.Amount,
            Currency = i.Currency,
            IncomeDate = i.IncomeDate.DateTime,
            Description = i.Description,
            Source = i.Source
        }).ToList();

        ViewBag.PageNumber = pageNumber;
        ViewBag.PageSize = pageSize;
        ViewBag.TotalItems = paged.TotalItems;
        ViewBag.TotalPages = (int)Math.Ceiling(paged.TotalItems / (double)pageSize);
        return View(viewModels);
    }

    public async Task<IActionResult> Create(bool partial = false)
    {
        var userId = _userManager.GetUserId(User);
        if (userId == null) return Unauthorized();

        await LoadViewBagAsync(userId);
        var model = new IncomeCreateViewModel { IncomeDate = DateTime.Today, Currency = Currency.TZS };

        var isAjax = partial || string.Equals(Request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase);
        return isAjax ? PartialView("_IncomeCreatePartial", model) : View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(IncomeCreateViewModel model)
    {
        var isAjax = string.Equals(Request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase);
        var userId = _userManager.GetUserId(User);
        if (userId == null) return Unauthorized();

        if (!ModelState.IsValid)
        {
            await LoadViewBagAsync(userId);
            return isAjax ? PartialView("_IncomeCreatePartial", model) : View(model);
        }

        var accountId = model.AccountId is { } id && id != Guid.Empty ? id : (Guid?)null;
        var income = await _incomeService.CreateAsync(
            userId,
            accountId,
            model.CategoryId,
            model.Amount,
            model.Currency,
            new DateTimeOffset(model.IncomeDate),
            model.Description,
            model.Source);

        if (model.SupportingFile != null && model.SupportingFile.Length > 0)
        {
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".pdf", ".doc", ".docx", ".xls", ".xlsx" };
            var ext = Path.GetExtension(model.SupportingFile.FileName)?.ToLowerInvariant();
            if (!string.IsNullOrEmpty(ext) && allowedExtensions.Contains(ext) && model.SupportingFile.Length <= 10 * 1024 * 1024)
            {
                try
                {
                    await using var stream = model.SupportingFile.OpenReadStream();
                    await _documentService.UploadAsync(
                        userId,
                        DocumentEntityType.Income,
                        income.Id,
                        model.SupportingFile.FileName,
                        model.SupportingFile.ContentType,
                        model.SupportingFile.Length,
                        stream,
                        label: "Income document");
                }
                catch { /* optional doc; don't fail create */ }
            }
        }

        if (isAjax) return Json(new { success = true });
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(Guid id, bool partial = false)
    {
        var userId = _userManager.GetUserId(User);
        if (userId == null) return Unauthorized();

        var income = await _incomeService.GetByIdAsync(id, userId);
        if (income == null) return NotFound();

        await LoadViewBagAsync(userId);
        ViewBag.SupportingDocs = await _documentService.GetForEntityAsync(DocumentEntityType.Income, id, userId);
        var model = new IncomeEditViewModel
        {
            Id = income.Id,
            AccountId = income.AccountId,
            CategoryId = income.CategoryId,
            Amount = income.Amount,
            Currency = income.Currency,
            IncomeDate = income.IncomeDate.DateTime,
            Description = income.Description,
            Source = income.Source
        };

        var isAjax = partial || string.Equals(Request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase);
        return isAjax ? PartialView("_IncomeEditPartial", model) : View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(IncomeEditViewModel model)
    {
        var isAjax = string.Equals(Request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase);
        var userId = _userManager.GetUserId(User);
        if (userId == null) return Unauthorized();

        if (!ModelState.IsValid)
        {
            await LoadViewBagAsync(userId);
            return isAjax ? PartialView("_IncomeEditPartial", model) : View(model);
        }

        var accountId = model.AccountId is { } id && id != Guid.Empty ? id : (Guid?)null;
        await _incomeService.UpdateAsync(
            model.Id,
            userId,
            model.Amount,
            new DateTimeOffset(model.IncomeDate),
            accountId,
            model.CategoryId,
            model.Description,
            model.Source);

        if (isAjax) return Json(new { success = true });
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        var userId = _userManager.GetUserId(User);
        if (userId == null) return Unauthorized();

        await _incomeService.DeleteAsync(id, userId);
        return RedirectToAction(nameof(Index));
    }

    private async Task LoadViewBagAsync(string userId)
    {
        ViewBag.Accounts = await _accountService.GetAllAsync(userId);
        ViewBag.Categories = await _categoryService.GetCategoriesForIncomeAsync(userId);
        ViewBag.Currencies = Enum.GetValues<Currency>();
    }
}
