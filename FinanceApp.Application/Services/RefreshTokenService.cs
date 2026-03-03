using FinanceApp.Application.Interfaces;
using FinanceApp.Application.Interfaces.Services;
using FinanceApp.Domain.Entities;
using System.Security.Cryptography;

namespace FinanceApp.Application.Services;

public class RefreshTokenService : IRefreshTokenService
{
    private readonly IRepository<RefreshToken> _repository;

    public RefreshTokenService(IRepository<RefreshToken> repository)
    {
        _repository = repository;
    }

    public async Task<RefreshToken> CreateAsync(string userId, int expirationDays)
    {
        var token = GenerateSecureToken();
        var refreshToken = new RefreshToken(userId, token, DateTime.UtcNow.AddDays(expirationDays));
        await _repository.AddAsync(refreshToken);
        await _repository.SaveChangesAsync();
        return refreshToken;
    }

    public async Task<RefreshToken?> GetByTokenAsync(string token)
    {
        var results = await _repository.FindAsync(rt => rt.Token == token);
        return results.FirstOrDefault();
    }

    public async Task RevokeAsync(string token)
    {
        var refreshToken = await GetByTokenAsync(token);
        if (refreshToken == null || !refreshToken.IsActive) return;
        refreshToken.Revoke();
        _repository.Update(refreshToken);
        await _repository.SaveChangesAsync();
    }

    public async Task RevokeAllForUserAsync(string userId)
    {
        var tokens = await _repository.FindAsync(rt => rt.UserId == userId && !rt.IsRevoked);
        foreach (var t in tokens)
            t.Revoke();
        await _repository.SaveChangesAsync();
    }

    private static string GenerateSecureToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes);
    }
}
