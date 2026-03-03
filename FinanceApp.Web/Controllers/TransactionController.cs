using FinanceApp.Application.Interfaces.Services;
using FinanceApp.Domain.Enums;
using FinanceApp.Infrastructure.Identity;
using FinanceApp.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using FinanceApp.Application.Interfaces;

namespace FinanceApp.Web.Controllers;

[Authorize]
public class TransactionController : Controller
{
    private readonly ITransactionService _transactionService;
    private readonly IAccountService _accountService;
    private readonly ICategoryService _categoryService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ISupportingDocumentService _documentService;

    public TransactionController(
        ITransactionService transactionService,
        IAccountService accountService,
        ICategoryService categoryService,
        UserManager<ApplicationUser> userManager,
        ISupportingDocumentService documentService)
    {
        _transactionService = transactionService;
        _accountService = accountService;
        _categoryService = categoryService;
        _userManager = userManager;
        _documentService = documentService;
    }

    // GET: /Transaction
    public async Task<IActionResult> Index(int pageNumber = 1, int pageSize = 15)
    {
        var userId = _userManager.GetUserId(User);
        if (userId == null) return Unauthorized();

        var paged = await _transactionService.GetPagedAsync(userId, pageNumber, pageSize);

        var viewModels = paged.Items.Select(t => new TransactionViewModel
        {
            Id = t.Id, AccountId = t.AccountId, AccountName = t.Account?.Name ?? "",
            Type = t.Type, Amount = t.Amount, Currency = t.Currency, Date = t.Date,
            CategoryId = t.CategoryId, CategoryName = t.Category?.Name,
            Note = t.Note, TransferGroupId = t.TransferGroupId, IsRecurring = t.IsRecurring
        }).ToList();

        ViewBag.PageNumber = pageNumber;
        ViewBag.PageSize = pageSize;
        ViewBag.TotalItems = paged.TotalItems;
        ViewBag.TotalPages = (int)Math.Ceiling(paged.TotalItems / (double)pageSize);

        return View(viewModels);
    }

    // GET: /Transaction/Create (AJAX)
    public async Task<IActionResult> Create(bool partial = false)
    {
        var userId = _userManager.GetUserId(User);
        if (userId == null) return Unauthorized();

        await LoadViewBagAsync(userId);
        var model = new TransactionCreateViewModel { Date = DateTime.Today, Currency = Currency.TZS };

        bool isAjax = partial || string.Equals(Request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase);
        return isAjax ? PartialView("_TransactionCreatePartial", model) : View(model);
    }

    // POST: /Transaction/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TransactionCreateViewModel model)
    {
        bool isAjax = string.Equals(Request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase);
        var userId = _userManager.GetUserId(User);
        if (userId == null) return Unauthorized();

        if (!ModelState.IsValid)
        {
            await LoadViewBagAsync(userId);
            return isAjax ? PartialView("_TransactionCreatePartial", model) : View(model);
        }

        await _transactionService.CreateAsync(
            userId, model.AccountId, model.Type, model.Amount, model.Currency,
            new DateTimeOffset(model.Date), model.CategoryId, model.Note, model.IsRecurring);

        if (isAjax) return Json(new { success = true });
        return RedirectToAction(nameof(Index));
    }

    // GET: /Transaction/Transfer (AJAX)
    public async Task<IActionResult> Transfer(bool partial = false)
    {
        var userId = _userManager.GetUserId(User);
        if (userId == null) return Unauthorized();

        await LoadViewBagAsync(userId);
        var model = new TransactionTransferViewModel { Date = DateTime.Today, Currency = Currency.TZS };

        bool isAjax = partial || string.Equals(Request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase);
        return isAjax ? PartialView("_TransactionTransferPartial", model) : View(model);
    }

    // POST: /Transaction/Transfer
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Transfer(TransactionTransferViewModel model)
    {
        bool isAjax = string.Equals(Request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase);
        var userId = _userManager.GetUserId(User);
        if (userId == null) return Unauthorized();

        if (model.FromAccountId == model.ToAccountId)
            ModelState.AddModelError(nameof(model.ToAccountId), "Source and destination accounts must be different.");

        if (!ModelState.IsValid)
        {
            await LoadViewBagAsync(userId);
            return isAjax ? PartialView("_TransactionTransferPartial", model) : View(model);
        }

        await _transactionService.CreateTransferAsync(
            userId, model.FromAccountId, model.ToAccountId,
            model.Amount, model.Currency, new DateTimeOffset(model.Date), model.Note);

        if (isAjax) return Json(new { success = true });
        return RedirectToAction(nameof(Index));
    }

    // GET: /Transaction/Edit/{id} (AJAX)
    public async Task<IActionResult> Edit(Guid id, bool partial = false)
    {
        var userId = _userManager.GetUserId(User);
        if (userId == null) return Unauthorized();

        var t = await _transactionService.GetByIdAsync(id, userId);
        if (t == null) return NotFound();

        await LoadViewBagAsync(userId);
        ViewBag.SupportingDocs = await _documentService.GetForEntityAsync(
            DocumentEntityType.Transaction, id, userId);

        var model = new TransactionEditViewModel
        {
            Id = t.Id, Amount = t.Amount, Date = t.Date.DateTime,
            CategoryId = t.CategoryId, Note = t.Note,
            Type = t.Type, AccountName = t.Account?.Name ?? "", Currency = t.Currency
        };

        bool isAjax = partial || string.Equals(Request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase);
        return isAjax ? PartialView("_TransactionEditPartial", model) : View(model);
    }

    // POST: /Transaction/Edit/{id}
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(TransactionEditViewModel model)
    {
        bool isAjax = string.Equals(Request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase);
        var userId = _userManager.GetUserId(User);
        if (userId == null) return Unauthorized();

        if (!ModelState.IsValid)
        {
            await LoadViewBagAsync(userId);
            return isAjax ? PartialView("_TransactionEditPartial", model) : View(model);
        }

        await _transactionService.UpdateAsync(
            model.Id, userId, model.Amount, new DateTimeOffset(model.Date), model.CategoryId, model.Note);

        if (isAjax) return Json(new { success = true });
        return RedirectToAction(nameof(Index));
    }

    // POST: /Transaction/Delete/{id}
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        var userId = _userManager.GetUserId(User);
        if (userId == null) return Unauthorized();

        await _transactionService.DeleteAsync(id, userId);
        return RedirectToAction(nameof(Index));
    }

    private async Task LoadViewBagAsync(string userId)
    {
        ViewBag.Accounts = await _accountService.GetAllAsync(userId);
        ViewBag.Categories = await _categoryService.GetAllAsync(userId);
        ViewBag.Currencies = Enum.GetValues<Currency>();
        ViewBag.TransactionTypes = new[] { TransactionType.Income, TransactionType.Expense };
    }
}
