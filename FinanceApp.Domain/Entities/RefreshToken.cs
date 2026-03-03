using FinanceApp.Domain.Common;

namespace FinanceApp.Domain.Entities;

/// <summary>
/// Stores issued refresh tokens. One user can have multiple active tokens (e.g. multiple devices).
/// Tokens are rotated: each use invalidates the old token and issues a new one.
/// </summary>
public class RefreshToken : BaseEntity
{
    public string UserId { get; private set; } = null!;
    public string Token { get; private set; } = null!;
    public DateTime ExpiresAt { get; private set; }
    public bool IsRevoked { get; private set; }

    protected RefreshToken() { }

    public RefreshToken(string userId, string token, DateTime expiresAt)
    {
        UserId = userId;
        Token = token;
        ExpiresAt = expiresAt;
    }

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsActive => !IsRevoked && !IsExpired;

    public void Revoke() => IsRevoked = true;
}
