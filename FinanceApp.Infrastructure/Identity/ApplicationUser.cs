using Microsoft.AspNetCore.Identity;
using FinanceApp.Domain.Enums;

namespace FinanceApp.Infrastructure.Identity;

public class ApplicationUser : IdentityUser
{
    /// <summary>User's first name.</summary>
    public string? FirstName { get; set; }

    /// <summary>User's last name.</summary>
    public string? LastName { get; set; }

    /// <summary>Relative path for profile image (e.g. /uploads/profiles/xxx.jpg).</summary>
    public string? ProfileImagePath { get; set; }

    /// <summary>Country name (e.g. Tanzania, United States) for display and reporting.</summary>
    public string? Country { get; set; }

    /// <summary>ISO 3166-1 alpha-2 country code (e.g. TZ, US) for phone dialing and APIs.</summary>
    public string? CountryCode { get; set; }

    /// <summary>Current subscription plan (defaults to Free).</summary>
    public SubscriptionPlan SubscriptionPlan { get; set; } = SubscriptionPlan.Free;

    /// <summary>When the current subscription was assigned.</summary>
    public DateTimeOffset SubscriptionAssignedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>UTC expiry for paid plans from App Store / Play Billing (null = no time-bound entitlement or Free).</summary>
    public DateTimeOffset? SubscriptionExpiresAtUtc { get; set; }

    /// <summary>Whether the plan came from Apple, Google, or manual admin assignment.</summary>
    public SubscriptionBillingSource SubscriptionBillingSource { get; set; } = SubscriptionBillingSource.None;

    /// <summary>Latest known Apple original transaction id (subscriptions).</summary>
    public string? AppleOriginalTransactionId { get; set; }

    /// <summary>Latest Google Play purchase token for the active subscription (for revalidation).</summary>
    public string? GooglePurchaseToken { get; set; }

    /// <summary>BCP 47 language code for UI (e.g. en, sw). Synced with web cookie and mobile i18n.</summary>
    public string PreferredLanguage { get; set; } = "en";

    /// <summary>
    /// If true, user receives daily reminder notifications when no expense/income is logged that day.
    /// </summary>
    public bool DailyReminderEnabled { get; set; } = true;
}