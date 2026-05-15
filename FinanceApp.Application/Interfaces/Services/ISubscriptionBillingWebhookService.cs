using System.Text.Json;

namespace FinanceApp.Application.Interfaces.Services;

/// <summary>
/// Processes App Store Server Notifications v2 and Google Play RTDN (Pub/Sub push or direct JSON).
/// </summary>
public interface ISubscriptionBillingWebhookService
{
    Task ProcessAppleNotificationAsync(JsonElement body, CancellationToken cancellationToken = default);

    Task ProcessGoogleNotificationAsync(JsonElement body, CancellationToken cancellationToken = default);
}
