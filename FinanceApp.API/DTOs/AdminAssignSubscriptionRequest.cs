using System.ComponentModel.DataAnnotations;

namespace FinanceApp.API.DTOs;

/// <summary>Admin-only manual subscription assignment (support, testing).</summary>
public sealed class AdminAssignSubscriptionRequest
{
    [Required]
    public string UserId { get; set; } = "";

    [Required]
    public string Plan { get; set; } = "";

    /// <summary>Optional UTC expiry; if null, entitlement has no expiry (not recommended for production).</summary>
    public DateTimeOffset? ExpiresAtUtc { get; set; }
}
