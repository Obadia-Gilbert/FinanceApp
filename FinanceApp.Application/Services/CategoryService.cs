using FinanceApp.Application.Common;
using FinanceApp.Application.Interfaces;
using FinanceApp.Application.Interfaces.Services;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;
using FinanceApp.Application.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace FinanceApp.Application.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly IRepository<Category> _categoryRepository;

        public CategoryService(IRepository<Category> categoryRepository)
        {
            _categoryRepository = categoryRepository ?? throw new ArgumentNullException(nameof(categoryRepository));
        }

        /// <summary>
        /// Get a single category by Id and user ownership.
        /// </summary>
        public async Task<Category?> GetByIdAsync(Guid id, string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("UserId is required", nameof(userId));

            var category = await _categoryRepository.GetByIdAsync(id);
            if (category == null || category.UserId != userId)
                return null;

            return category;
        }

        /// <summary>
        /// Get all categories for a specific user.
        /// </summary>
        public async Task<IEnumerable<Category>> GetAllAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("UserId is required", nameof(userId));
                
            return await _categoryRepository.FindAsync(c => c.UserId == userId);
        }

        /// <summary>
        /// Categories that can be used for expenses (Type is Expense or Both).
        /// </summary>
        public async Task<IEnumerable<Category>> GetCategoriesForExpenseAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("UserId is required", nameof(userId));
            var list = await _categoryRepository.FindAsync(c => c.UserId == userId && (c.Type == CategoryType.Expense || c.Type == CategoryType.Both));
            return list.OrderBy(c => c.Name).ToList();
        }

        /// <summary>
        /// Categories that can be used for income (Type is Income or Both).
        /// </summary>
        public async Task<IEnumerable<Category>> GetCategoriesForIncomeAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("UserId is required", nameof(userId));
            var list = await _categoryRepository.FindAsync(c => c.UserId == userId && (c.Type == CategoryType.Income || c.Type == CategoryType.Both));
            return list.OrderBy(c => c.Name).ToList();
        }

        /// <summary>
        /// Get paged categories for a user with optional filtering and ordering.
        /// </summary>
        public async Task<PagedResult<Category>> GetPagedCategoriesAsync(
            int pageNumber,
            int pageSize,
            string userId,
            Expression<Func<Category, bool>>? filter = null,
            Func<IQueryable<Category>, IOrderedQueryable<Category>>? orderBy = null)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("UserId is required", nameof(userId));

            Expression<Func<Category, bool>> userFilter = c => c.UserId == userId;

            if (filter != null)
            {
                userFilter = userFilter.AndAlso(filter); // using ExpressionExtensions.AndAlso
            }

            return await _categoryRepository.GetPagedAsync(
                pageNumber,
                pageSize,
                userFilter,
                orderBy
            );
        }

        /// <summary>
        /// Create a new category for a user.
        /// </summary>
        public async Task<Category> CreateCategoryAsync(string name, string userId, CategoryType type = CategoryType.Expense, string? description = null, string? icon = null, string? badgeColor = null)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Category name is required", nameof(name));

            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("UserId is required", nameof(userId));

            var category = new Category(name, description, type, icon, badgeColor)
            {
                UserId = userId
            };

            await _categoryRepository.AddAsync(category);
            await _categoryRepository.SaveChangesAsync();

            return category;
        }

        /// <summary>
        /// Update an existing category owned by the user.
        /// </summary>
        public async Task UpdateCategoryAsync(Guid id, string userId, string name, CategoryType type, string? description = null, string? icon = null, string? badgeColor = null)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("UserId is required", nameof(userId));

            var category = await _categoryRepository.GetByIdAsync(id);
            if (category == null || category.UserId != userId)
                throw new InvalidOperationException("Category not found or not owned by user");

            category.UpdateName(name);
            category.UpdateType(type);
            category.UpdateDescription(description);
            category.UpdateIcon(icon);
            if (!string.IsNullOrWhiteSpace(badgeColor))
                category.UpdateBadgeColor(badgeColor);

            _categoryRepository.Update(category);
            await _categoryRepository.SaveChangesAsync();
        }

        /// <summary>
        /// Soft delete a category owned by the user.
        /// </summary>
        public async Task DeleteCategoryAsync(Guid id, string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("UserId is required", nameof(userId));

            var category = await _categoryRepository.GetByIdAsync(id);
            if (category == null || category.UserId != userId)
                throw new InvalidOperationException("Category not found or not owned by user");

            _categoryRepository.SoftDelete(category);
            await _categoryRepository.SaveChangesAsync();
        }

public async Task AssignDefaultCategoriesToUserAsync(string userId)
{
    var existing = await _categoryRepository.FindAsync(c => c.UserId == userId);
    var existingNames = new HashSet<string>(existing.Select(c => c.Name.Trim()), StringComparer.OrdinalIgnoreCase);

    foreach (var name in CategoryDefaults.DefaultCategories)
    {
        var trimmed = name.Trim();
        if (string.IsNullOrWhiteSpace(trimmed) || existingNames.Contains(trimmed))
            continue;

        var userCategory = new Category(trimmed, null, CategoryType.Expense, null, null) { UserId = userId };
        await _categoryRepository.AddAsync(userCategory);
        existingNames.Add(trimmed);
    }

    foreach (var name in CategoryDefaults.DefaultIncomeCategories)
    {
        var trimmed = name.Trim();
        if (string.IsNullOrWhiteSpace(trimmed) || existingNames.Contains(trimmed))
            continue;

        var userCategory = new Category(trimmed, null, CategoryType.Income, null, null) { UserId = userId };
        await _categoryRepository.AddAsync(userCategory);
        existingNames.Add(trimmed);
    }

    await _categoryRepository.SaveChangesAsync();
}
    public async Task<IEnumerable<Category>> GetCategoriesAsync(string userId, bool isAdmin)
{
    if (isAdmin)
    {
        return await _categoryRepository.GetAllAsync();
    }

    if (string.IsNullOrWhiteSpace(userId))
        throw new ArgumentException("UserId is required", nameof(userId));

    return await _categoryRepository.FindAsync(c => c.UserId == userId);
}
     }
}