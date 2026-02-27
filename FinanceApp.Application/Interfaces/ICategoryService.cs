using FinanceApp.Application.Common;
using FinanceApp.Domain.Entities;
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
        Task<PagedResult<Category>> GetPagedCategoriesAsync(
            int pageNumber,
            int pageSize,
            string userId,
            Expression<Func<Category, bool>>? filter = null,
            Func<IQueryable<Category>, IOrderedQueryable<Category>>? orderBy = null
        );

        Task<Category> CreateCategoryAsync(string name, string userId, string? description = null);
        Task UpdateCategoryAsync(Guid id, string userId, string name, string? description = null);
        Task DeleteCategoryAsync(Guid id, string userId);
        Task AssignDefaultCategoriesToUserAsync(string userId);

        Task<IEnumerable<Category>> GetCategoriesAsync(string userId, bool isAdmin);
    }
}