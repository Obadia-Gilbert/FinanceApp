namespace FinanceApp.API.DTOs;

public record SubscriptionDto(
    string CurrentPlan,
    DateTimeOffset? SubscriptionAssignedAt,
    DateTimeOffset? SubscriptionExpiresAtUtc,
    string BillingSource);
