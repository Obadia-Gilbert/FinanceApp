using FinanceApp.Application.Interfaces.Services;
using FinanceApp.Domain.Enums;
using FinanceApp.Infrastructure.Identity;
using FinanceApp.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace FinanceApp.Web.Controllers;

[Authorize]
public class SubscriptionController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ISubscriptionEntitlementService _entitlement;

    public SubscriptionController(
        UserManager<ApplicationUser> userManager,
        ISubscriptionEntitlementService entitlement)
    {
        _userManager = userManager;
        _entitlement = entitlement;
    }

    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        await _entitlement.SyncExpiredSubscriptionAsync(user.Id, HttpContext.RequestAborted);
        user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        var model = new SubscriptionViewModel
        {
            CurrentPlan = user.SubscriptionPlan,
            SubscriptionAssignedAt = user.SubscriptionAssignedAt,
            SubscriptionExpiresAtUtc = user.SubscriptionExpiresAtUtc,
            BillingSource = user.SubscriptionBillingSource
        };

        return View(model);
    }

    /// <summary>Dev/support only: instant plan change. Prefer App Store / Play Billing in production.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Upgrade(string plan)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        if (!Enum.TryParse<SubscriptionPlan>(plan, ignoreCase: true, out var nextPlan))
        {
            TempData["SubscriptionError"] = "Invalid subscription plan selected.";
            return RedirectToAction(nameof(Index));
        }

        if (nextPlan == SubscriptionPlan.Free)
        {
            user.SubscriptionPlan = SubscriptionPlan.Free;
            user.SubscriptionExpiresAtUtc = null;
            user.SubscriptionBillingSource = SubscriptionBillingSource.AdminManual;
            user.AppleOriginalTransactionId = null;
            user.GooglePurchaseToken = null;
            user.SubscriptionAssignedAt = DateTimeOffset.UtcNow;
            await _userManager.UpdateAsync(user);
            TempData["SubscriptionSuccess"] = "Plan set to Free.";
            return RedirectToAction(nameof(Index));
        }

        var expires = DateTimeOffset.UtcNow.AddYears(100);
        var result = await _entitlement.ApplyVerifiedEntitlementAsync(
            user.Id,
            nextPlan,
            expires,
            SubscriptionBillingSource.AdminManual,
            externalTransactionId: $"web-admin-{Guid.NewGuid():N}",
            productId: "web-admin-manual",
            notes: "MVC admin upgrade",
            googlePurchaseToken: null,
            HttpContext.RequestAborted);

        if (!result.Success)
        {
            TempData["SubscriptionError"] = result.ErrorMessage ?? "Failed to update subscription.";
            return RedirectToAction(nameof(Index));
        }

        TempData["SubscriptionSuccess"] = $"Subscription set to {nextPlan} (admin).";
        return RedirectToAction(nameof(Index));
    }
}
