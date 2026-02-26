using FinanceApp.Application.Interfaces.Services;
using FinanceApp.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FinanceApp.Application.Interfaces;
using FinanceApp.Application.Common;
using FinanceApp.Domain.Common;
using System.Linq.Expressions;
using System.Linq;
using FinanceApp.Domain.Enums;

namespace FinanceApp.Application.Services
{
    public class ExpenseService : IExpenseService
    {
        private readonly IRepository<Expense> _expenseRepository;

        public ExpenseService(IRepository<Expense> expenseRepository)
        {
            _expenseRepository = expenseRepository;
        }

        // -----------------------
        // Basic CRUD
        // -----------------------
        public async Task<Expense?> GetByIdAsync(Guid id)
        {
            return await _expenseRepository.GetByIdAsync(id);
        }

        public async Task<IEnumerable<Expense>> GetAllAsync()
        {
            return await _expenseRepository.GetAllAsync(e => e.Category);
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

        // -----------------------
        // Pagination
        // -----------------------
        public async Task<PagedResult<Expense>> GetPagedExpensesAsync(
            int pageNumber,
            int pageSize,
            Expression<Func<Expense, bool>>? filter = null,
            Func<IQueryable<Expense>, IOrderedQueryable<Expense>>? orderBy = null)
        {
            return await _expenseRepository.GetPagedAsync(
                pageNumber,
                pageSize,
                filter,
                orderBy,
                e => e.Category // include Category relationship
            );
        }

        // -----------------------
        // Create Expense helper
        // -----------------------
        public async Task<Expense> CreateExpenseAsync(
            decimal amount,
            Currency currency,
            DateTime expenseDate,
            Guid categoryId,
            string userId,
            string description,
            string? receiptPath = null)
        {
            var expense = new Expense(
                amount: amount,
                currency: currency,
                expenseDate: expenseDate,
                categoryId: categoryId,
                userId: userId,
                description: description,
                receiptPath: receiptPath
            );

            await _expenseRepository.AddAsync(expense);
            await _expenseRepository.SaveChangesAsync();

            return expense;
        }
        public async Task<PagedResult<Expense>> GetByCategoryIdAsync(
            Guid categoryId,
            string userId,
            int pageNumber,
            int pageSize)
        {
            return await _expenseRepository.GetPagedAsync(
                pageNumber,
                pageSize,
                filter: e => e.CategoryId == categoryId && e.UserId == userId,
                orderBy: q => q.OrderByDescending(e => e.ExpenseDate),
                includes: e => e.Category
            );
        }
   }
}
