using FinanceApp.Domain.Entities;

namespace FinanceApp.Application.Interfaces.Services;

public interface ISharedReportService
{
    /// <summary>Creates a shareable report link; returns the SharedReport with token and expiry.</summary>
    Task<SharedReport> CreateAsync(string userId, int year, int month, int expiryDays = 7);

    /// <summary>Gets a shared report by token if not expired; returns null otherwise.</summary>
    Task<SharedReport?> GetByTokenAsync(string token);
}
