using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace FinanceApp.API.Controllers;

/// <summary>
/// Endpoints for Apple App Store Server Notifications V2 and Google Play Real-time Developer Notifications.
/// Harden with signature verification and idempotency before production; configure URLs in App Store Connect / Google Cloud Console.
/// </summary>
[ApiController]
[Route("api/subscription/webhooks")]
public sealed class SubscriptionWebhooksController : ControllerBase
{
    private readonly ILogger<SubscriptionWebhooksController> _logger;
    private readonly IConfiguration _config;

    public SubscriptionWebhooksController(ILogger<SubscriptionWebhooksController> logger, IConfiguration config)
    {
        _logger = logger;
        _config = config;
    }

    /// <summary>Apple ASN v2 — JSON body contains <c>signedPayload</c> (JWS). Implement full verification + user lookup by originalTransactionId.</summary>
    [HttpPost("apple")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Apple([FromBody] JsonElement body)
    {
        if (!string.IsNullOrEmpty(_config["SubscriptionBilling:Webhooks:Apple:SharedSecret"]))
        {
            var header = Request.Headers["X-Apple-Webhook-Secret"].ToString();
            if (header != _config["SubscriptionBilling:Webhooks:Apple:SharedSecret"])
                return Unauthorized();
        }

        _logger.LogInformation("Apple subscription webhook received (process renewals/cancellations in a follow-up). Payload length: {Len}", body.GetRawText().Length);
        return Ok();
    }

    /// <summary>Google RTDN — typically Pub/Sub push; accept raw JSON for testing.</summary>
    [HttpPost("google")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Google([FromBody] JsonElement body)
    {
        if (!string.IsNullOrEmpty(_config["SubscriptionBilling:Webhooks:Google:SharedSecret"]))
        {
            var header = Request.Headers["X-Google-Webhook-Secret"].ToString();
            if (header != _config["SubscriptionBilling:Webhooks:Google:SharedSecret"])
                return Unauthorized();
        }

        _logger.LogInformation("Google subscription webhook received. Payload length: {Len}", body.GetRawText().Length);
        return Ok();
    }
}
