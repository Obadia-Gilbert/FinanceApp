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

    /// <summary>Current subscription plan (defaults to Free).</summary>
    public SubscriptionPlan SubscriptionPlan { get; set; } = SubscriptionPlan.Free;

    /// <summary>When the current subscription was assigned.</summary>
    public DateTimeOffset SubscriptionAssignedAt { get; set; } = DateTimeOffset.UtcNow;
}