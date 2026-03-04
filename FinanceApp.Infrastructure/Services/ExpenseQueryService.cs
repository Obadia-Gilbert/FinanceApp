using FinanceApp.Application.Common;
using FinanceApp.Application.Interfaces.Services;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;
using FinanceApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FinanceApp.Infrastructure.Services;

public class ExpenseQueryService : IExpenseQueryService
{
    private readonly FinanceDbContext _context;

    public ExpenseQueryService(FinanceDbContext context)
    {
        _context = context;
    }

    public async Task<Dictionary<(Guid CategoryId, Currency Currency), decimal>> GetCategorySpendForMonthAsync(string userId, int month, int year)
    {
        var start = new DateTimeOffset(year, month, 1, 0, 0, 0, TimeSpan.Zero);
        var end = start.AddMonths(1);

        var list = await _context.Expenses
            .AsNoTracking()
            .Where(e => !e.IsDeleted && e.UserId == userId && e.ExpenseDate >= start && e.ExpenseDate < end)
            .GroupBy(e => new { e.CategoryId, e.Currency })
            .Select(g => new { g.Key.CategoryId, g.Key.Currency, Sum = g.Sum(e => e.Amount) })
            .ToListAsync();

        return list.ToDictionary(x => (x.CategoryId, x.Currency), x => x.Sum);
    }

    public async Task<Dictionary<Currency, decimal>> GetTotalsByCurrencyAsync(string userId, DateTimeOffset? from = null, DateTimeOffset? to = null)
    {
        var query = _context.Expenses
            .AsNoTracking()
            .Where(e => !e.IsDeleted && e.UserId == userId);

        if (from.HasValue)
            query = query.Where(e => e.ExpenseDate >= from.Value);
        if (to.HasValue)
            query = query.Where(e => e.ExpenseDate <= to.Value);

        var list = await query
            .GroupBy(e => e.Currency)
            .Select(g => new { Currency = g.Key, Sum = g.Sum(e => e.Amount) })
            .ToListAsync();

        return list.ToDictionary(x => x.Currency, x => x.Sum);
    }

    public async Task<Dictionary<Currency, decimal>> GetMonthTotalsByCurrencyAsync(string userId, int month, int year)
    {
        var start = new DateTimeOffset(year, month, 1, 0, 0, 0, TimeSpan.Zero);
        var end = start.AddMonths(1);

        var list = await _context.Expenses
            .AsNoTracking()
            .Where(e => !e.IsDeleted && e.UserId == userId && e.ExpenseDate >= start && e.ExpenseDate < end)
            .GroupBy(e => e.Currency)
            .Select(g => new { Currency = g.Key, Sum = g.Sum(e => e.Amount) })
            .ToListAsync();

        return list.ToDictionary(x => x.Currency, x => x.Sum);
    }

    public async Task<IReadOnlyList<ExpenseTotalByDayDto>> GetSumsByDayAsync(string userId, DateTimeOffset from, DateTimeOffset to, Currency? currency = null)
    {
        var query = _context.Expenses
            .AsNoTracking()
            .Where(e => !e.IsDeleted && e.UserId == userId && e.ExpenseDate >= from && e.ExpenseDate < to);

        if (currency.HasValue)
            query = query.Where(e => e.Currency == currency.Value);

        var list = await query
            .GroupBy(e => e.ExpenseDate.Date)
            .Select(g => new { Date = g.Key, Sum = g.Sum(e => e.Amount) })
            .OrderBy(x => x.Date)
            .ToListAsync();

        return list.Select(x => new ExpenseTotalByDayDto(x.Date, x.Sum)).ToList();
    }

    public async Task<IReadOnlyList<CategoryTotalDto>> GetCategoryTotalsForMonthAsync(string userId, int month, int year, Currency? currency = null)
    {
        var start = new DateTimeOffset(year, month, 1, 0, 0, 0, TimeSpan.Zero);
        var end = start.AddMonths(1);

        var query = _context.Expenses
            .AsNoTracking()
            .Where(e => !e.IsDeleted && e.UserId == userId && e.ExpenseDate >= start && e.ExpenseDate < end);

        if (currency.HasValue)
            query = query.Where(e => e.Currency == currency.Value);

        var list = await query
            .Join(_context.Set<Category>(), e => e.CategoryId, c => c.Id, (e, c) => new { e.CategoryId, c.Name, e.Amount })
            .GroupBy(x => new { x.CategoryId, x.Name })
            .Select(g => new { g.Key.CategoryId, g.Key.Name, Sum = g.Sum(x => x.Amount) })
            .OrderByDescending(x => x.Sum)
            .ToListAsync();

        return list.Select(x => new CategoryTotalDto(x.CategoryId, x.Name, x.Sum)).ToList();
    }

    public async Task<IReadOnlyList<Expense>> GetRecentExpensesAsync(string userId, int count)
    {
        return await _context.Expenses
            .Where(e => !e.IsDeleted && e.UserId == userId)
            .Include(e => e.Category)
            .OrderByDescending(e => e.ExpenseDate)
            .Take(count)
            .ToListAsync();
    }

    public async Task<int> GetTotalCountAsync(string userId)
    {
        return await _context.Expenses
            .AsNoTracking()
            .CountAsync(e => !e.IsDeleted && e.UserId == userId);
    }

    public async Task<int> GetMonthExpenseCountAsync(string userId, int month, int year)
    {
        var start = new DateTimeOffset(year, month, 1, 0, 0, 0, TimeSpan.Zero);
        var end = start.AddMonths(1);
        return await _context.Expenses
            .AsNoTracking()
            .CountAsync(e => !e.IsDeleted && e.UserId == userId && e.ExpenseDate >= start && e.ExpenseDate < end);
    }

    public async Task<IReadOnlyList<Expense>> GetTopExpensesForMonthAsync(string userId, int month, int year, int count)
    {
        var start = new DateTimeOffset(year, month, 1, 0, 0, 0, TimeSpan.Zero);
        var end = start.AddMonths(1);
        return await _context.Expenses
            .Where(e => !e.IsDeleted && e.UserId == userId && e.ExpenseDate >= start && e.ExpenseDate < end)
            .Include(e => e.Category)
            .OrderByDescending(e => e.Amount)
            .Take(count)
            .ToListAsync();
    }
}
