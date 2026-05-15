using System.Text.Json;
using FinanceApp.Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace FinanceApp.API.Controllers;

/// <summary>
/// Apple App Store Server Notifications V2 and Google Play Real-time Developer Notifications.
/// </summary>
[ApiController]
[Route("api/subscription/webhooks")]
public sealed class SubscriptionWebhooksController : ControllerBase
{
    private readonly ILogger<SubscriptionWebhooksController> _logger;
    private readonly IConfiguration _config;
    private readonly ISubscriptionBillingWebhookService _webhooks;
    private readonly IStripeBillingWebhookHandler _stripeWebhooks;

    public SubscriptionWebhooksController(
        ILogger<SubscriptionWebhooksController> logger,
        IConfiguration config,
        ISubscriptionBillingWebhookService webhooks,
        IStripeBillingWebhookHandler stripeWebhooks)
    {
        _logger = logger;
        _config = config;
        _webhooks = webhooks;
        _stripeWebhooks = stripeWebhooks;
    }

    /// <summary>Apple ASN v2 — JSON body contains <c>signedPayload</c> (JWS).</summary>
    [HttpPost("apple")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Apple([FromBody] JsonElement body, CancellationToken cancellationToken)
    {
        if (!ValidateSharedSecret("SubscriptionBilling:Webhooks:Apple:SharedSecret", "X-Apple-Webhook-Secret"))
            return Unauthorized();

        try
        {
            await _webhooks.ProcessAppleNotificationAsync(body, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Apple subscription webhook processing failed.");
        }

        return Ok();
    }

    /// <summary>Google RTDN — Pub/Sub push JSON or direct test payload.</summary>
    [HttpPost("google")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Google([FromBody] JsonElement body, CancellationToken cancellationToken)
    {
        if (!ValidateSharedSecret("SubscriptionBilling:Webhooks:Google:SharedSecret", "X-Google-Webhook-Secret"))
            return Unauthorized();

        try
        {
            await _webhooks.ProcessGoogleNotificationAsync(body, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Google subscription webhook processing failed.");
        }

        return Ok();
    }

    /// <summary>Stripe billing webhooks (checkout, subscription lifecycle).</summary>
    [HttpPost("stripe")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Stripe(CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(Request.Body);
        var json = await reader.ReadToEndAsync(cancellationToken);
        var signature = Request.Headers["Stripe-Signature"].ToString();

        try
        {
            await _stripeWebhooks.ProcessWebhookAsync(json, signature, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Stripe subscription webhook processing failed.");
        }

        return Ok();
    }

    private bool ValidateSharedSecret(string configKey, string headerName)
    {
        var expected = _config[configKey];
        if (string.IsNullOrEmpty(expected))
            return true;

        var header = Request.Headers[headerName].ToString();
        return header == expected;
    }
}
