using FinanceApp.Application.Interfaces.Services;
using Google.Apis.AndroidPublisher.v3;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FinanceApp.Infrastructure.Subscription;

public sealed class GooglePlaySubscriptionVerifier : IGooglePlaySubscriptionVerifier
{
    private readonly IConfiguration _config;
    private readonly ILogger<GooglePlaySubscriptionVerifier> _logger;

    public GooglePlaySubscriptionVerifier(IConfiguration config, ILogger<GooglePlaySubscriptionVerifier> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task<GoogleVerifiedSubscription?> VerifySubscriptionAsync(
        string subscriptionId,
        string purchaseToken,
        CancellationToken cancellationToken = default)
    {
        var packageName = _config["SubscriptionBilling:Google:PackageName"];
        var jsonPath = _config["SubscriptionBilling:Google:ServiceAccountJsonPath"];
        if (string.IsNullOrWhiteSpace(packageName) || string.IsNullOrWhiteSpace(jsonPath))
        {
            _logger.LogWarning("Google Play verification skipped: PackageName or ServiceAccountJsonPath not configured.");
            return null;
        }

        if (!File.Exists(jsonPath))
        {
            _logger.LogWarning("Google Play service account file not found at {Path}.", jsonPath);
            return null;
        }

        try
        {
            await using var stream = File.OpenRead(jsonPath);
            var credential = GoogleCredential.FromStream(stream)
                .CreateScoped(AndroidPublisherService.Scope.Androidpublisher);

            var service = new AndroidPublisherService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = "FinanceApp"
            });

            var sub = await service.Purchases.Subscriptions.Get(packageName, subscriptionId, purchaseToken)
                .ExecuteAsync(cancellationToken);

            if (sub == null)
                return null;

            var orderId = sub.OrderId ?? "";
            var expiryMs = sub.ExpiryTimeMillis;
            if (expiryMs == null)
            {
                _logger.LogWarning("Google subscription response missing expiryTimeMillis.");
                return null;
            }

            var expires = DateTimeOffset.FromUnixTimeMilliseconds(expiryMs.Value);
            var autoRenew = sub.AutoRenewing ?? false;

            return new GoogleVerifiedSubscription(orderId, subscriptionId, expires, autoRenew);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Google Play subscription verification failed.");
            return null;
        }
    }
}
