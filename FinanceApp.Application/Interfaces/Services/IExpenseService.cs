using FinanceApp.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

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
    }
}