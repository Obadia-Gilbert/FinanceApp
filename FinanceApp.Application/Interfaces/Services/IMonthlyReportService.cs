using FinanceApp.Application.Common;

namespace FinanceApp.Application.Interfaces.Services;

public interface IMonthlyReportService
{
    Task<MonthlyReportResult> GetMonthlyReportAsync(string userId, int year, int month, string? preferredCurrency = null, int topExpensesCount = 20);
}
