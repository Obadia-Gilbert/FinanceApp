using FinanceApp.Application.Common;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace FinanceApp.Application.Interfaces.Services
{
    public interface ICategoryService
    {
        Task<Category?> GetByIdAsync(Guid id, string userId);
        Task<IEnumerable<Category>> GetAllAsync(string userId);

        /// <summary>Categories that can be used for expenses (Type is Expense or Both).</summary>
        Task<IEnumerable<Category>> GetCategoriesForExpenseAsync(string userId);

        /// <summary>Categories that can be used for income (Type is Income or Both).</summary>
        Task<IEnumerable<Category>> GetCategoriesForIncomeAsync(string userId);

        Task<PagedResult<Category>> GetPagedCategoriesAsync(
            int pageNumber,
            int pageSize,
            string userId,
            Expression<Func<Category, bool>>? filter = null,
            Func<IQueryable<Category>, IOrderedQueryable<Category>>? orderBy = null
        );

        Task<Category> CreateCategoryAsync(string name, string userId, CategoryType type = CategoryType.Expense, string? description = null, string? icon = null, string? badgeColor = null);
        Task UpdateCategoryAsync(Guid id, string userId, string name, CategoryType type, string? description = null, string? icon = null, string? badgeColor = null);
        Task DeleteCategoryAsync(Guid id, string userId);
        Task AssignDefaultCategoriesToUserAsync(string userId);

        Task<IEnumerable<Category>> GetCategoriesAsync(string userId, bool isAdmin);
    }
}