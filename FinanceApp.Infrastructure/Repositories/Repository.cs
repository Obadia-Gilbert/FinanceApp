using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using FinanceApp.Application.Interfaces;
using FinanceApp.Domain.Common;
using FinanceApp.Application.Common;
using FinanceApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using FinanceApp.Infrastructure.Extensions;

namespace FinanceApp.Infrastructure.Repositories
{
    public class Repository<T> : IRepository<T> where T : BaseEntity
    {
        protected readonly FinanceDbContext _context;
        protected readonly DbSet<T> _dbSet;

        public Repository(FinanceDbContext context)
        {
            _context = context;
            _dbSet = context.Set<T>();
        }

        // Get entity by Id (ignores soft deleted)
        public async Task<T?> GetByIdAsync(Guid id)
        {
            return await _dbSet.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
        }

        // Get all entities (ignores soft deleted)
        /*public async Task<IEnumerable<T>> GetAllAsync()
        {
            return await _dbSet.Where(x => !x.IsDeleted).ToListAsync();
        }*/
        public async Task<IEnumerable<T>> GetAllAsync(params Expression<Func<T, object>>[] includes)
        {
            IQueryable<T> query = _dbSet.Where(x => !x.IsDeleted);

            if (includes != null && includes.Any())
        {
            query = includes.Aggregate(query, (current, include) => current.Include(include));
        }

            return await query.ToListAsync();
        }
        // Find entities by predicate (combines with soft delete filter)
        public async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
        {
            var notDeletedPredicate = predicate.AndAlso(x => !x.IsDeleted);
            return await _dbSet.Where(notDeletedPredicate).ToListAsync();
        }

        // Add new entity
        public async Task AddAsync(T entity)
        {
            await _dbSet.AddAsync(entity);
        }

        // Update existing entity
        public void Update(T entity)
        {
            _dbSet.Update(entity);
        }

        // Hard delete entity
        public void Remove(T entity)
        {
            _dbSet.Remove(entity);
        }

        // Soft delete entity
        public void SoftDelete(T entity)
        {
            entity.SoftDelete();
            _dbSet.Update(entity);
        }

        // Commit changes
        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        public async Task<PagedResult<T>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        Expression<Func<T, bool>>? filter = null,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
        params Expression<Func<T, object>>[] includes)
        {
        IQueryable<T> query = _dbSet.Where(x => !x.IsDeleted);

        if (filter != null)
        query = query.Where(filter);

        if (includes != null && includes.Any())
        query = includes.Aggregate(query, (current, include) => current.Include(include));

        if (orderBy != null)
        query = orderBy(query);

        var totalItems = await query.CountAsync();

        var items = await query
        .Skip((pageNumber - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync();

        return new PagedResult<T>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalItems = totalItems
        };
        }

    }
}