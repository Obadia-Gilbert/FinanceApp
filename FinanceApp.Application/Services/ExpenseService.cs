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
        private readonly ITransactionService _transactionService;
        private readonly IBudgetNotificationService _budgetNotificationService;

        public ExpenseService(
            IRepository<Expense> expenseRepository,
            ITransactionService transactionService,
            IBudgetNotificationService budgetNotificationService)
        {
            _expenseRepository = expenseRepository;
            _transactionService = transactionService;
            _budgetNotificationService = budgetNotificationService;
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
            if (expense.TransactionId.HasValue)
            {
                await _transactionService.UpdateAsync(
                    expense.TransactionId.Value,
                    expense.UserId,
                    expense.Amount,
                    expense.ExpenseDate,
                    expense.CategoryId,
                    expense.Description);
            }
            await _expenseRepository.SaveChangesAsync();

            var month = expense.ExpenseDate.Month;
            var year = expense.ExpenseDate.Year;
            await _budgetNotificationService.EvaluateAndCreateNotificationsAsync(expense.UserId, month, year);
        }

        public async Task SoftDeleteExpenseAsync(Guid id)
        {
            var expense = await _expenseRepository.GetByIdAsync(id);
            if (expense == null) return;

            var userId = expense.UserId;
            var month = expense.ExpenseDate.Month;
            var year = expense.ExpenseDate.Year;

            if (expense.TransactionId.HasValue)
                await _transactionService.DeleteAsync(expense.TransactionId.Value, expense.UserId);

            _expenseRepository.SoftDelete(expense);
            await _expenseRepository.SaveChangesAsync();

            await _budgetNotificationService.EvaluateAndCreateNotificationsAsync(userId, month, year);
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
            string? receiptPath = null,
            Guid? accountId = null)
        {
            Guid? transactionId = null;
            if (accountId.HasValue)
            {
                var transaction = await _transactionService.CreateAsync(
                    userId, accountId.Value, TransactionType.Expense, amount, currency,
                    new DateTimeOffset(expenseDate), categoryId, description, isRecurring: false);
                transactionId = transaction.Id;
            }

            var expense = new Expense(
                amount: amount,
                currency: currency,
                expenseDate: expenseDate,
                categoryId: categoryId,
                userId: userId,
                accountId: accountId,
                description: description,
                receiptPath: receiptPath,
                transactionId: transactionId);

            await _expenseRepository.AddAsync(expense);
            await _expenseRepository.SaveChangesAsync();

            var month = expense.ExpenseDate.Month;
            var year = expense.ExpenseDate.Year;
            await _budgetNotificationService.EvaluateAndCreateNotificationsAsync(userId, month, year);

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
