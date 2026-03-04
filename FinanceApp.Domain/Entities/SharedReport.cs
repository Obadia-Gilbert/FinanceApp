using FinanceApp.Domain.Common;

namespace FinanceApp.Domain.Entities;

/// <summary>Shareable link for a user's monthly report. Anyone with the token can view the report until expiry.</summary>
public class SharedReport : BaseEntity
{
    public string UserId { get; private set; } = null!;
    public string Token { get; private set; } = null!;
    public int Month { get; private set; }
    public int Year { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }

    protected SharedReport() { }

    public SharedReport(string userId, string token, int month, int year, DateTimeOffset expiresAt)
    {
        if (string.IsNullOrWhiteSpace(userId)) throw new ArgumentException("UserId is required.", nameof(userId));
        if (string.IsNullOrWhiteSpace(token)) throw new ArgumentException("Token is required.", nameof(token));
        if (month < 1 || month > 12) throw new ArgumentOutOfRangeException(nameof(month));
        if (year < 2000 || year > 2100) throw new ArgumentOutOfRangeException(nameof(year));

        UserId = userId;
        Token = token;
        Month = month;
        Year = year;
        ExpiresAt = expiresAt;
    }

    public bool IsExpired => DateTimeOffset.UtcNow > ExpiresAt;
}
