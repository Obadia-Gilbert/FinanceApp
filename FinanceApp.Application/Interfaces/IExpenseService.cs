using System;   
using FinanceApp.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;
using FinanceApp.Application.Common;
using FinanceApp.Domain.Common;
using FinanceApp.Domain.Enums;
using System.Linq.Expressions;


namespace FinanceApp.Application.Interfaces.Services
{
    public interface IExpenseService
    {
        Task<Expense?> GetByIdAsync(Guid id);
        Task<IEnumerable<Expense>> GetAllAsync();
        Task<IEnumerable<Expense>> GetByCategoryIdAsync(Guid categoryId);

        Task AddExpenseAsync(Expense expense);
        Task UpdateExpenseAsync(Expense expense);
        Task SoftDeleteExpenseAsync(Guid id);

        Task<PagedResult<Expense>> GetPagedExpensesAsync(
            int pageNumber,
            int pageSize,
            Expression<Func<Expense, bool>>? filter = null,
            Func<IQueryable<Expense>, IOrderedQueryable<Expense>>? orderBy = null);

        // Optional: create via parameters (instead of passing full entity)
        Task<Expense> CreateExpenseAsync(
            decimal amount,
            Currency currency,
            DateTime expenseDate,
            Guid categoryId,
            string userId,
            string description,
            string? receiptPath = null);
        Task<PagedResult<Expense>> GetByCategoryIdAsync(
            Guid categoryId,
            string userId,
            int pageNumber,
            int pageSize);
    }
}
