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
    private readonly IStripeBillingService _stripe;

    public SubscriptionController(
        UserManager<ApplicationUser> userManager,
        ISubscriptionEntitlementService entitlement,
        IStripeBillingService stripe)
    {
        _userManager = userManager;
        _entitlement = entitlement;
        _stripe = stripe;
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
            BillingSource = user.SubscriptionBillingSource,
            WebCheckoutEnabled = _stripe.IsConfigured,
            CanManageWebBilling = _stripe.IsConfigured &&
                user.SubscriptionBillingSource == SubscriptionBillingSource.Web &&
                !string.IsNullOrWhiteSpace(user.StripeCustomerId)
        };

        return View(model);
    }

    /// <summary>Stripe Checkout for Pro or Premium (web subscribers).</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Checkout(string plan)
    {
        if (!_stripe.IsConfigured)
        {
            TempData["SubscriptionError"] = "Web billing is not configured. Contact support or subscribe in the mobile app.";
            return RedirectToAction(nameof(Index));
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        if (!Enum.TryParse<SubscriptionPlan>(plan, ignoreCase: true, out var targetPlan) ||
            targetPlan == SubscriptionPlan.Free)
        {
            TempData["SubscriptionError"] = "Invalid subscription plan selected.";
            return RedirectToAction(nameof(Index));
        }

        if (user.SubscriptionBillingSource is SubscriptionBillingSource.Apple or SubscriptionBillingSource.Google)
        {
            TempData["SubscriptionInfo"] =
                "Your plan is billed through the App Store or Google Play. Manage it on your phone, or cancel there before subscribing on the web.";
            return RedirectToAction(nameof(Index));
        }

        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var checkoutUrl = await _stripe.CreateCheckoutSessionUrlAsync(
            user.Id,
            user.Email ?? string.Empty,
            targetPlan,
            successUrl: $"{baseUrl}/Subscription/Success?session_id={{CHECKOUT_SESSION_ID}}",
            cancelUrl: $"{baseUrl}/Subscription",
            user.StripeCustomerId,
            HttpContext.RequestAborted);

        if (checkoutUrl == null)
        {
            TempData["SubscriptionError"] = "Could not start checkout. Check Stripe price configuration.";
            return RedirectToAction(nameof(Index));
        }

        return Redirect(checkoutUrl);
    }

    /// <summary>Stripe Customer Portal — update card, cancel, view invoices.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ManageBilling()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        if (string.IsNullOrWhiteSpace(user.StripeCustomerId))
        {
            TempData["SubscriptionError"] = "No web billing account found for this user.";
            return RedirectToAction(nameof(Index));
        }

        var returnUrl = $"{Request.Scheme}://{Request.Host}/Subscription";
        var portalUrl = await _stripe.CreateCustomerPortalUrlAsync(
            user.StripeCustomerId,
            returnUrl,
            HttpContext.RequestAborted);

        if (portalUrl == null)
        {
            TempData["SubscriptionError"] = "Could not open billing portal.";
            return RedirectToAction(nameof(Index));
        }

        return Redirect(portalUrl);
    }

    public IActionResult Success()
    {
        TempData["SubscriptionSuccess"] =
            "Thank you! Your payment is processing — Pro or Premium unlocks on this site and in the mobile app once Stripe confirms.";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Dev/support only: instant plan change.</summary>
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
            user.StripeCustomerId = null;
            user.StripeSubscriptionId = null;
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
