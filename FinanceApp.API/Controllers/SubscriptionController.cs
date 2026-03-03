using System.Security.Claims;
using FinanceApp.API.DTOs;
using FinanceApp.Domain.Enums;
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

    public SubscriptionController(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    private string? UserId => User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    /// <summary>GET api/subscription — current user subscription.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(SubscriptionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSubscription()
    {
        if (UserId == null) return Unauthorized();

        var user = await _userManager.FindByIdAsync(UserId);
        if (user == null) return NotFound();

        return Ok(new SubscriptionDto(
            user.SubscriptionPlan.ToString(),
            user.SubscriptionAssignedAt));
    }

    /// <summary>POST api/subscription/upgrade — upgrade plan (Pro or Premium).</summary>
    [HttpPost("upgrade")]
    [ProducesResponseType(typeof(SubscriptionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Upgrade([FromBody] UpgradeSubscriptionRequest request)
    {
        if (UserId == null) return Unauthorized();

        var user = await _userManager.FindByIdAsync(UserId);
        if (user == null) return NotFound();

        if (!Enum.TryParse<SubscriptionPlan>(request.Plan, ignoreCase: true, out var nextPlan))
            return BadRequest(new { message = "Invalid plan. Use 'Pro' or 'Premium'." });

        if (nextPlan == SubscriptionPlan.Free)
            return BadRequest(new { message = "Use the Free plan; no upgrade needed." });

        if (user.SubscriptionPlan == nextPlan)
            return Ok(new SubscriptionDto(user.SubscriptionPlan.ToString(), user.SubscriptionAssignedAt));

        user.SubscriptionPlan = nextPlan;
        user.SubscriptionAssignedAt = DateTimeOffset.UtcNow;
        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });

        return Ok(new SubscriptionDto(user.SubscriptionPlan.ToString(), user.SubscriptionAssignedAt));
    }
}
