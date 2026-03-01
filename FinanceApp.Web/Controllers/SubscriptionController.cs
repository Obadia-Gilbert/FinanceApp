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

    public SubscriptionController(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        var model = new SubscriptionViewModel
        {
            CurrentPlan = user.SubscriptionPlan,
            SubscriptionAssignedAt = user.SubscriptionAssignedAt
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
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
            TempData["SubscriptionError"] = "You are already on the Free plan.";
            return RedirectToAction(nameof(Index));
        }

        if (user.SubscriptionPlan == nextPlan)
        {
            TempData["SubscriptionInfo"] = $"You are already on the {nextPlan} plan.";
            return RedirectToAction(nameof(Index));
        }

        user.SubscriptionPlan = nextPlan;
        user.SubscriptionAssignedAt = DateTimeOffset.UtcNow;
        var result = await _userManager.UpdateAsync(user);

        if (!result.Succeeded)
        {
            TempData["SubscriptionError"] = "Failed to update subscription. Please try again.";
            return RedirectToAction(nameof(Index));
        }

        TempData["SubscriptionSuccess"] = $"Subscription upgraded to {nextPlan}.";
        return RedirectToAction(nameof(Index));
    }
}
