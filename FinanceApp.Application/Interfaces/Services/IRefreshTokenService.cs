using FinanceApp.Domain.Entities;

namespace FinanceApp.Application.Interfaces.Services;

public interface IRefreshTokenService
{
    Task<RefreshToken> CreateAsync(string userId, int expirationDays);
    Task<RefreshToken?> GetByTokenAsync(string token);
    Task RevokeAsync(string token);
    Task RevokeAllForUserAsync(string userId);
}
