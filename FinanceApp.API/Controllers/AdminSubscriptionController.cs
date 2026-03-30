using FinanceApp.API.DTOs;
using FinanceApp.Application.Interfaces.Services;
using FinanceApp.Domain.Enums;
using FinanceApp.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace FinanceApp.API.Controllers;

/// <summary>Manual subscription assignment for administrators (support / QA).</summary>
[ApiController]
[Route("api/admin/subscriptions")]
[Authorize(Roles = "Admin")]
public sealed class AdminSubscriptionController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ISubscriptionEntitlementService _entitlement;

    public AdminSubscriptionController(
        UserManager<ApplicationUser> userManager,
        ISubscriptionEntitlementService entitlement)
    {
        _userManager = userManager;
        _entitlement = entitlement;
    }

    /// <summary>Assign or change a user's plan without a store purchase.</summary>
    [HttpPost("assign")]
    [ProducesResponseType(typeof(SubscriptionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Assign([FromBody] AdminAssignSubscriptionRequest request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(request.UserId);
        if (user == null)
            return NotFound(new { message = "User not found." });

        if (!Enum.TryParse<SubscriptionPlan>(request.Plan, ignoreCase: true, out var plan))
            return BadRequest(new { message = "Invalid plan." });

        if (plan == SubscriptionPlan.Free)
        {
            user.SubscriptionPlan = SubscriptionPlan.Free;
            user.SubscriptionExpiresAtUtc = null;
            user.SubscriptionBillingSource = SubscriptionBillingSource.AdminManual;
            user.AppleOriginalTransactionId = null;
            user.GooglePurchaseToken = null;
            user.SubscriptionAssignedAt = DateTimeOffset.UtcNow;
            await _userManager.UpdateAsync(user);
            var free = await _userManager.FindByIdAsync(user.Id);
            return Ok(new SubscriptionDto(
                free!.SubscriptionPlan.ToString(),
                free.SubscriptionAssignedAt,
                free.SubscriptionExpiresAtUtc,
                free.SubscriptionBillingSource.ToString()));
        }

        var expires = request.ExpiresAtUtc ?? DateTimeOffset.UtcNow.AddYears(100);

        var result = await _entitlement.ApplyVerifiedEntitlementAsync(
            user.Id,
            plan,
            expires,
            SubscriptionBillingSource.AdminManual,
            externalTransactionId: $"admin-{Guid.NewGuid():N}",
            productId: "admin-manual",
            notes: "admin assign",
            googlePurchaseToken: null,
            cancellationToken);

        if (!result.Success)
            return BadRequest(new { message = result.ErrorMessage });

        var updated = await _userManager.FindByIdAsync(user.Id);
        return Ok(new SubscriptionDto(
            updated!.SubscriptionPlan.ToString(),
            updated.SubscriptionAssignedAt,
            updated.SubscriptionExpiresAtUtc,
            updated.SubscriptionBillingSource.ToString()));
    }
}
