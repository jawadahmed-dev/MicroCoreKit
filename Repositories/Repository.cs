using MicroCoreKit.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace MicroCoreKit.Repositories
{
    public class Repository<TEntity, TContext> : IRepository<TEntity, TContext>
        where TEntity : BaseEntity
        where TContext : BaseContext
    {
        private readonly TContext _context;
        private readonly DbSet<TEntity> _entities;

        public Repository(TContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _entities = context.Set<TEntity>();
        }

        public async Task<TEntity> GetByIdAsync(Guid id)
        {
            return await _entities.AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == id && !e.Deleted);
        }

        public async Task<TEntity> AddAsync(TEntity entity, bool createNewId = true)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));

            if (createNewId)
            {
                entity.Id = Guid.NewGuid();
            }

            SetAuditFields(entity, isNew: true);
            await _entities.AddAsync(entity);
            await _context.SaveChangesAsync();

            return entity;
        }

        public async Task<IReadOnlyList<TEntity>> AddRangeAsync(IList<TEntity> entities, bool createNewId = true)
        {
            if (entities == null || !entities.Any())
                return Array.Empty<TEntity>();

            foreach (var entity in entities)
            {
                if (createNewId)
                {
                    entity.Id = Guid.NewGuid();
                }
                SetAuditFields(entity, isNew: true);
            }

            await _entities.AddRangeAsync(entities);
            await _context.SaveChangesAsync();

            return entities.AsReadOnly();
        }

        public async Task<TEntity> UpdateAsync(TEntity entity)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));

            SetAuditFields(entity, isNew: false);
            _entities.Update(entity);
            await _context.SaveChangesAsync();

            return entity;
        }

        public async Task<IReadOnlyList<TEntity>> UpdateRangeAsync(IList<TEntity> entities)
        {
            if (entities == null || !entities.Any())
                return Array.Empty<TEntity>();

            foreach (var entity in entities)
            {
                SetAuditFields(entity, isNew: false);
            }

            _entities.UpdateRange(entities);
            await _context.SaveChangesAsync();

            return entities.AsReadOnly();
        }

        public async Task<bool> DeleteAsync(Guid id, string modifiedBy = "")
        {
            var entity = await _entities.FindAsync(id);
            if (entity == null || entity.Deleted) return false;

            entity.Deleted = true;
            SetAuditFields(entity, isNew: false, modifiedByOverride: modifiedBy);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> DeleteRangeAsync(IList<Guid> ids, string modifiedBy = "")
        {
            var entities = await _entities
                .Where(e => ids.Contains(e.Id) && !e.Deleted)
                .ToListAsync();

            if (!entities.Any()) return false;

            foreach (var entity in entities)
            {
                entity.Deleted = true;
                SetAuditFields(entity, isNew: false, modifiedByOverride: modifiedBy);
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IReadOnlyList<TEntity>> GetAllAsync()
        {
            return await _entities
                .Where(e => !e.Deleted)
                .OrderByDescending(e => e.CreatedWhen)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<bool> ExistsAsync(Guid id)
        {
            return await _entities
                .AsNoTracking()
                .AnyAsync(e => e.Id == id && !e.Deleted);
        }

        public async Task<bool> ExistsAsync(Expression<Func<TEntity, bool>> predicate)
        {
            return await _entities
                .AsNoTracking()
                .Where(e => !e.Deleted)
                .AnyAsync(predicate);
        }

        public async Task<(int totalCount, IReadOnlyList<TEntity> items)> GetPaginatedAsync(
            Expression<Func<TEntity, bool>> predicate,
            int pageNumber,
            int pageSize,
            string orderByField = "",
            bool ascending = true,
            params string[] includes)
        {
            pageNumber = Math.Max(1, pageNumber);
            pageSize = Math.Max(1, pageSize);

            var query = BuildQuery(predicate, includes);
            var totalCount = await query.CountAsync();

            if (!string.IsNullOrEmpty(orderByField))
            {
                query = await SortQueryAsync(query, orderByField, ascending);
            }
            else
            {
                query = query.OrderByDescending(e => e.CreatedWhen);
            }

            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (totalCount, items.AsReadOnly());
        }

        public IQueryable<TEntity> Query(
            Expression<Func<TEntity, bool>> predicate = null,
            params string[] includes)
        {
            return BuildQuery(predicate, includes);
        }

        public async Task<TEntity> FirstOrDefaultAsync(
            Expression<Func<TEntity, bool>> predicate,
            string orderByField = "",
            bool ascending = true,
            params string[] includes)
        {
            var query = BuildQuery(predicate, includes);

            if (!string.IsNullOrEmpty(orderByField))
            {
                query = await SortQueryAsync(query, orderByField, ascending);
            }
            else
            {
                query = query.OrderByDescending(e => e.CreatedWhen);
            }

            return await query.FirstOrDefaultAsync();
        }

        // Private helper methods
        private IQueryable<TEntity> BuildQuery(
            Expression<Func<TEntity, bool>> predicate,
            string[] includes)
        {
            var query = _entities.AsNoTracking()
                .Where(e => !e.Deleted);

            if (includes != null && includes.Any())
            {
                query = includes.Aggregate(query, (current, include) => current.Include(include));
            }

            if (predicate != null)
            {
                query = query.Where(predicate);
            }

            return query;
        }

        private void SetAuditFields(
            TEntity entity,
            bool isNew,
            string modifiedByOverride = "")
        {
            var userId = string.IsNullOrEmpty(modifiedByOverride)
                ? _context.getUserId()
                : modifiedByOverride;

            if (isNew)
            {
                entity.CreatedBy = userId;
                entity.CreatedWhen = DateTime.UtcNow;
            }

            entity.ModifiedBy = userId;
            entity.ModifiedWhen = DateTime.UtcNow;
        }

        private async Task<IQueryable<TEntity>> SortQueryAsync(
            IQueryable<TEntity> query,
            string orderByField,
            bool ascending)
        {
            var parameter = Expression.Parameter(typeof(TEntity), "e");
            var property = orderByField.Split('.').Aggregate(
                (Expression)parameter,
                Expression.Property);

            var lambda = Expression.Lambda(property, parameter);
            var methodName = ascending ? "OrderBy" : "OrderByDescending";

            var resultExpression = Expression.Call(
                typeof(Queryable),
                methodName,
                new[] { typeof(TEntity), property.Type },
                query.Expression,
                Expression.Quote(lambda));

            return await Task.FromResult(query.Provider.CreateQuery<TEntity>(resultExpression));
        }
    }
}
