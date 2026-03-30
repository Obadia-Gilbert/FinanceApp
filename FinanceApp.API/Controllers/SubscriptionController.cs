using System.Security.Claims;
using FinanceApp.API.DTOs;
using FinanceApp.Application.Interfaces.Services;
using FinanceApp.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace FinanceApp.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SubscriptionController : ControllerBase
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

    private string? UserId => User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    /// <summary>GET api/subscription — current user subscription (expires downgrades when past due).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(SubscriptionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSubscription(CancellationToken cancellationToken)
    {
        if (UserId == null) return Unauthorized();

        await _entitlement.SyncExpiredSubscriptionAsync(UserId, cancellationToken);

        var user = await _userManager.FindByIdAsync(UserId);
        if (user == null) return NotFound();

        return Ok(new SubscriptionDto(
            user.SubscriptionPlan.ToString(),
            user.SubscriptionAssignedAt,
            user.SubscriptionExpiresAtUtc,
            user.SubscriptionBillingSource.ToString()));
    }
}
