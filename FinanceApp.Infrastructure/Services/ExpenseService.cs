using FinanceApp.Application.Interfaces.Services;
using FinanceApp.Domain.Entities;
using FinanceApp.Infrastructure.Repositories;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FinanceApp.Application.Interfaces;   // For IRepository<T>


namespace FinanceApp.Infrastructure.Services
{
    public class ExpenseService : IExpenseService
    {
        private readonly IRepository<Expense> _expenseRepository;

        public ExpenseService(IRepository<Expense> expenseRepository)
        {
            _expenseRepository = expenseRepository;
        }

        public async Task<Expense?> GetByIdAsync(Guid id)
        {
            return await _expenseRepository.GetByIdAsync(id);
        }

        public async Task<IEnumerable<Expense>> GetAllAsync()
        {
            return await _expenseRepository.GetAllAsync();
        }

        public async Task<IEnumerable<Expense>> GetByCategoryIdAsync(Guid categoryId)
        {
            return await _expenseRepository.FindAsync(e => e.CategoryId == categoryId);
        }

        public async Task AddExpenseAsync(Expense expense)
        {
            await _expenseRepository.AddAsync(expense);
            await _expenseRepository.SaveChangesAsync();
        }

        public async Task UpdateExpenseAsync(Expense expense)
        {
            _expenseRepository.Update(expense);
            await _expenseRepository.SaveChangesAsync();
        }

        public async Task SoftDeleteExpenseAsync(Guid id)
        {
            var expense = await _expenseRepository.GetByIdAsync(id);
            if (expense == null) return;

            _expenseRepository.SoftDelete(expense);
            await _expenseRepository.SaveChangesAsync();
        }
    }
}