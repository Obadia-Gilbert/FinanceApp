using FinanceApp.Application.Interfaces.Services;
using FinanceApp.Domain.Enums;
using FinanceApp.Infrastructure.Identity;
using FinanceApp.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace FinanceApp.Web.Controllers;

[Authorize]
public class AccountController : Controller
{
    private readonly IAccountService _accountService;
    private readonly UserManager<ApplicationUser> _userManager;

    public AccountController(IAccountService accountService, UserManager<ApplicationUser> userManager)
    {
        _accountService = accountService;
        _userManager = userManager;
    }

    // GET: /Account
    public async Task<IActionResult> Index()
    {
        var userId = _userManager.GetUserId(User);
        if (userId == null) return Unauthorized();

        var accounts = await _accountService.GetAllAsync(userId);
        var viewModels = new List<AccountViewModel>();

        foreach (var a in accounts)
        {
            var balance = await _accountService.GetBalanceAsync(a.Id, userId);
            viewModels.Add(new AccountViewModel
            {
                Id = a.Id, Name = a.Name, Type = a.Type, Currency = a.Currency,
                InitialBalance = a.InitialBalance, CurrentBalance = balance,
                Description = a.Description, IsActive = a.IsActive
            });
        }

        return View(viewModels);
    }

    // GET: /Account/Create (AJAX)
    public IActionResult Create(bool partial = false)
    {
        var model = new AccountCreateViewModel { Currency = Currency.TZS };
        ViewBag.AccountTypes = Enum.GetValues<AccountType>();
        ViewBag.Currencies = Enum.GetValues<Currency>();

        bool isAjax = partial || string.Equals(Request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase);
        return isAjax ? PartialView("_AccountCreatePartial", model) : View(model);
    }

    // POST: /Account/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AccountCreateViewModel model)
    {
        bool isAjax = string.Equals(Request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase);
        var userId = _userManager.GetUserId(User);
        if (userId == null) return Unauthorized();

        if (!ModelState.IsValid)
        {
            ViewBag.AccountTypes = Enum.GetValues<AccountType>();
            ViewBag.Currencies = Enum.GetValues<Currency>();
            return isAjax ? PartialView("_AccountCreatePartial", model) : View(model);
        }

        await _accountService.CreateAsync(userId, model.Name, model.Type, model.Currency, model.InitialBalance, model.Description);

        if (isAjax) return Json(new { success = true });
        return RedirectToAction(nameof(Index));
    }

    // GET: /Account/Edit/{id} (AJAX)
    public async Task<IActionResult> Edit(Guid id, bool partial = false)
    {
        var userId = _userManager.GetUserId(User);
        if (userId == null) return Unauthorized();

        var account = await _accountService.GetByIdAsync(id, userId);
        if (account == null) return NotFound();

        var balance = await _accountService.GetBalanceAsync(id, userId);
        var model = new AccountEditViewModel
        {
            Id = account.Id, Name = account.Name, Description = account.Description,
            Type = account.Type, Currency = account.Currency, CurrentBalance = balance
        };

        bool isAjax = partial || string.Equals(Request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase);
        return isAjax ? PartialView("_AccountEditPartial", model) : View(model);
    }

    // POST: /Account/Edit/{id}
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(AccountEditViewModel model)
    {
        bool isAjax = string.Equals(Request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase);
        var userId = _userManager.GetUserId(User);
        if (userId == null) return Unauthorized();

        if (!ModelState.IsValid)
            return isAjax ? PartialView("_AccountEditPartial", model) : View(model);

        await _accountService.UpdateAsync(model.Id, userId, model.Name, model.Description);

        if (isAjax) return Json(new { success = true });
        return RedirectToAction(nameof(Index));
    }

    // POST: /Account/Delete/{id}
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        var userId = _userManager.GetUserId(User);
        if (userId == null) return Unauthorized();

        await _accountService.DeactivateAsync(id, userId);
        return RedirectToAction(nameof(Index));
    }
}
