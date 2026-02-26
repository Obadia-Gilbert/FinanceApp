using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using FinanceApp.Domain.Common;
using FinanceApp.Application.Common;

namespace FinanceApp.Application.Interfaces
{
    public interface IRepository<T> where T : BaseEntity
    {
        // Get an entity by Id (ignores soft deleted entities)
        Task<T?> GetByIdAsync(Guid id);

        // Get all entities (ignores soft deleted entities)
        //Task<IEnumerable<T>> GetAllAsync(
        Task<IEnumerable<T>> GetAllAsync(params Expression<Func<T, object>>[] includes);

        // Find entities matching a predicate (combined with soft delete)
        Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);

        // Add a new entity
        Task AddAsync(T entity);

        // Update an existing entity
        void Update(T entity);

        // Hard delete an entity
        void Remove(T entity);

        // Soft delete an entity
        void SoftDelete(T entity);

        // Commit all changes to the database
        Task SaveChangesAsync();

        Task<PagedResult<T>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        Expression<Func<T, bool>>? filter = null,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
        params Expression<Func<T, object>>[] includes);
        }
}