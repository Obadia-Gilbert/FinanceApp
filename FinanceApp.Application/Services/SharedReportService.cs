using FinanceApp.Application.Interfaces;
using FinanceApp.Application.Interfaces.Services;
using FinanceApp.Domain.Entities;

namespace FinanceApp.Application.Services;

public class SharedReportService : ISharedReportService
{
    private readonly IRepository<SharedReport> _repository;

    public SharedReportService(IRepository<SharedReport> repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public async Task<SharedReport> CreateAsync(string userId, int year, int month, int expiryDays = 7)
    {
        var token = Guid.NewGuid().ToString("N");
        var expiresAt = DateTimeOffset.UtcNow.AddDays(expiryDays);
        var shared = new SharedReport(userId, token, month, year, expiresAt);
        await _repository.AddAsync(shared);
        await _repository.SaveChangesAsync();
        return shared;
    }

    public async Task<SharedReport?> GetByTokenAsync(string token)
    {
        if (string.IsNullOrWhiteSpace(token)) return null;
        var list = await _repository.FindAsync(s => s.Token == token);
        var report = list.FirstOrDefault();
        return report != null && !report.IsExpired ? report : null;
    }
}
