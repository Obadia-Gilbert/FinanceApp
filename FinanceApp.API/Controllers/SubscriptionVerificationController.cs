using System.Security.Claims;
using FinanceApp.API.DTOs;
using FinanceApp.Application.Interfaces.Services;
using FinanceApp.Domain.Enums;
using FinanceApp.Infrastructure.Identity;
using FinanceApp.Infrastructure.Subscription;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace FinanceApp.API.Controllers;

/// <summary>Client-reported purchases verified with Apple / Google before entitlement is granted.</summary>
[ApiController]
[Route("api/subscription")]
[Authorize]
public sealed class SubscriptionVerificationController : ControllerBase
{
    private readonly IAppleStoreTransactionVerifier _appleVerifier;
    private readonly IGooglePlaySubscriptionVerifier _googleVerifier;
    private readonly ISubscriptionEntitlementService _entitlement;
    private readonly SubscriptionProductMapper _productMapper;
    private readonly UserManager<ApplicationUser> _userManager;

    public SubscriptionVerificationController(
        IAppleStoreTransactionVerifier appleVerifier,
        IGooglePlaySubscriptionVerifier googleVerifier,
        ISubscriptionEntitlementService entitlement,
        SubscriptionProductMapper productMapper,
        UserManager<ApplicationUser> userManager)
    {
        _appleVerifier = appleVerifier;
        _googleVerifier = googleVerifier;
        _entitlement = entitlement;
        _productMapper = productMapper;
        _userManager = userManager;
    }

    private string? UserId => User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    /// <summary>POST /api/subscription/verify/apple — verify StoreKit 2 JWS and grant plan.</summary>
    [HttpPost("verify/apple")]
    [ProducesResponseType(typeof(SubscriptionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> VerifyApple([FromBody] VerifyAppleSubscriptionRequest request, CancellationToken cancellationToken)
    {
        if (UserId == null) return Unauthorized();

        var verified = await _appleVerifier.VerifySignedTransactionAsync(request.SignedTransactionJws, cancellationToken);
        if (verified == null)
            return BadRequest(new { message = "Invalid or unverifiable Apple transaction." });

        if (!_productMapper.TryMapApple(verified.ProductId, out var plan))
            return BadRequest(new { message = $"Unknown product id: {verified.ProductId}. Configure SubscriptionBilling:AppleProductIdToPlan." });

        var result = await _entitlement.ApplyVerifiedEntitlementAsync(
            UserId,
            plan,
            verified.ExpiresAtUtc,
            SubscriptionBillingSource.Apple,
            verified.OriginalTransactionId,
            verified.ProductId,
            notes: "verify/apple",
            googlePurchaseToken: null,
            cancellationToken);

        if (!result.Success)
            return BadRequest(new { message = result.ErrorMessage });

        var user = await _userManager.FindByIdAsync(UserId);
        return Ok(ToDto(user!));
    }

    /// <summary>POST /api/subscription/verify/google — verify token with Play Developer API and grant plan.</summary>
    [HttpPost("verify/google")]
    [ProducesResponseType(typeof(SubscriptionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> VerifyGoogle([FromBody] VerifyGoogleSubscriptionRequest request, CancellationToken cancellationToken)
    {
        if (UserId == null) return Unauthorized();

        var verified = await _googleVerifier.VerifySubscriptionAsync(request.SubscriptionId, request.PurchaseToken, cancellationToken);
        if (verified == null)
            return BadRequest(new { message = "Invalid Google Play subscription or API not configured." });

        if (!_productMapper.TryMapGoogle(verified.ProductId, out var plan))
            return BadRequest(new { message = $"Unknown subscription id: {verified.ProductId}. Configure SubscriptionBilling:GoogleProductIdToPlan." });

        var result = await _entitlement.ApplyVerifiedEntitlementAsync(
            UserId,
            plan,
            verified.ExpiresAtUtc,
            SubscriptionBillingSource.Google,
            verified.OrderId,
            verified.ProductId,
            notes: "verify/google",
            googlePurchaseToken: request.PurchaseToken,
            cancellationToken);

        if (!result.Success)
            return BadRequest(new { message = result.ErrorMessage });

        var user = await _userManager.FindByIdAsync(UserId);
        return Ok(ToDto(user!));
    }

    private static SubscriptionDto ToDto(ApplicationUser user) =>
        new(
            user.SubscriptionPlan.ToString(),
            user.SubscriptionAssignedAt,
            user.SubscriptionExpiresAtUtc,
            user.SubscriptionBillingSource.ToString());
}
