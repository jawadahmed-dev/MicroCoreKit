using MicroCoreKit.Base;
using MicroCoreKit.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace MicroCoreKit.Repositories
{

    public interface IRepository<TEntity, TContext> where TEntity : BaseEntity
    {
        // Basic CRUD Operations
        Task<TEntity> GetByIdAsync(Guid id);
        Task<TEntity> AddAsync(TEntity entity, bool createNewId = true);
        Task<IReadOnlyList<TEntity>> AddRangeAsync(IList<TEntity> entities, bool createNewId = true);
        Task<TEntity> UpdateAsync(TEntity entity);
        Task<IReadOnlyList<TEntity>> UpdateRangeAsync(IList<TEntity> entities);
        Task<bool> DeleteAsync(Guid id, string modifiedBy = "");
        Task<bool> DeleteRangeAsync(IList<Guid> ids, string modifiedBy = "");

        // Query Operations
        Task<IReadOnlyList<TEntity>> GetAllAsync();
        Task<bool> ExistsAsync(Guid id);
        Task<bool> ExistsAsync(Expression<Func<TEntity, bool>> predicate);

        // Advanced Queries with Pagination and Includes
        Task<(int totalCount, IReadOnlyList<TEntity> items)> GetPaginatedAsync(
            Expression<Func<TEntity, bool>> predicate,
            int pageNumber,
            int pageSize,
            string orderByField = "",
            bool ascending = true,
            params string[] includes);

        IQueryable<TEntity> Query(
            Expression<Func<TEntity, bool>> predicate = null,
            params string[] includes);

        // Single Entity Fetching
        Task<TEntity> FirstOrDefaultAsync(
            Expression<Func<TEntity, bool>> predicate,
            string orderByField = "",
            bool ascending = true,
            params string[] includes);
    }

    


    public class Repository<TEntity, TContext> : IRepository<TEntity, TContext> where TEntity : BaseEntity where TContext : BaseContext
    {
        private readonly TContext _context;
        private readonly DbSet<TEntity> _entities;


        public Repository(TContext context)
        {
            this._context = context;
            _entities = context.Set<TEntity>();

        }
        public async Task<IReadOnlyList<TEntity>> GetAllAsync()
        {
            return await _entities.OrderByDescending(d => d.CreatedWhen).AsNoTracking().ToListAsync();
        }
        public async Task<TEntity> GetByIdAsync(Guid Id)
        {
            return await _entities.AsNoTracking().FirstOrDefaultAsync(x => x.Id == Id);
        }
        public async Task<TEntity> AddAsync(TEntity entity, bool createNewId = true)
        {
            if (createNewId)
            {
                entity.Id = Guid.NewGuid();
            }
            entity.CreatedBy = entity.ModifiedBy = _context.getUserId();
            entity.CreatedWhen = DateTime.UtcNow;
            entity.ModifiedWhen = DateTime.UtcNow;
            entity.Deleted = false;
            await _entities.AddAsync(entity);
            await _context.SaveChangesAsync();

            return entity;
        }
        public async Task<TEntity> AddDetachedAsync(TEntity entity, bool createNewId = true)
        {
            if (createNewId)
            {
                entity.Id = Guid.NewGuid();
            }
            entity.CreatedBy = entity.ModifiedBy = _context.getUserId();
            entity.CreatedWhen = DateTime.UtcNow;
            entity.ModifiedWhen = DateTime.UtcNow;
            entity.Deleted = false;
            await _entities.AddAsync(entity);
            await _context.SaveChangesAsync();
            _context.Entry<TEntity>(entity).State = EntityState.Detached;
            return entity;
        }

        public async Task<IList<TEntity>> AddAsync(IList<TEntity> entities, bool createNewId = true)
        {
            int sec = 1;
            foreach (var item in entities)
            {
                if (createNewId)
                {
                    item.Id = Guid.NewGuid();
                }
                item.CreatedBy = item.ModifiedBy = _context.getUserId();
                item.CreatedWhen = DateTime.UtcNow.AddSeconds(sec);
                item.ModifiedWhen = DateTime.UtcNow.AddSeconds(sec);
                item.Deleted = false;
                _entities.Add(item);
                sec++;
            }
            await _context.SaveChangesAsync();
            return entities;
        }

        public TEntity AddSync(TEntity entity)
        {
            entity.Id = Guid.NewGuid();
            entity.CreatedBy = entity.ModifiedBy = _context.getUserId();
            entity.CreatedWhen = DateTime.UtcNow;
            entity.ModifiedWhen = DateTime.UtcNow;
            entity.Deleted = false;
            _entities.AddAsync(entity);
            _context.SaveChangesAsync();
            return entity;
        }
        public async Task<TEntity> UpdateAsync(TEntity entity, bool isDetached = false)
        {
            if (entity == null)
            {
                return null;
            }
            entity.ModifiedWhen = DateTime.UtcNow;
            entity.ModifiedBy = _context.getUserId();
            _entities.Attach(entity);
            _context.Entry(entity).State = EntityState.Modified;
            _context.Entry(entity).Property("CreatedBy").IsModified = false;
            _context.Entry(entity).Property("CreatedWhen").IsModified = false;
            await _context.SaveChangesAsync();
            if (isDetached == true)
            {
                _context.Entry<TEntity>(entity).State = EntityState.Detached;
            }
            return entity;
        }

        public async Task<List<TEntity>> UpdateAsync(List<TEntity> entities, bool isDetached = false)
        {
            if (entities == null)
            {
                throw new ArgumentNullException("entity");
            }
            foreach (var item in entities)
            {
                item.ModifiedBy = _context.getUserId();
                item.ModifiedWhen = DateTime.UtcNow;
                _context.Entry(item).Property("CreatedBy").IsModified = false;
                _context.Entry(item).Property("CreatedWhen").IsModified = false;
                _context.Attach(item);
                _context.Entry(item).State = EntityState.Modified;
            }
            await _context.SaveChangesAsync();
            return entities;
        }

        public async void SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        public async Task<TEntity> DeleteAsync(Guid Id, string modifiedBy = "")
        {
            var data = await _entities.FindAsync(Id);
            if (data == null)
            {
                return null;
            }
            data.ModifiedBy = _context.getUserId();
            if (modifiedBy != "")
            {
                data.ModifiedBy = modifiedBy;
            }
            data.ModifiedWhen = DateTime.UtcNow;
            data.Deleted = true;
            await _context.SaveChangesAsync();
            return data;
        }

        public async Task<TEntity> HardDeleteAsync(Guid Id)
        {
            var data = await _entities.FindAsync(Id);
            if (data == null)
            {
                return null;
            }
            _entities.Remove(data);
            await _context.SaveChangesAsync();
            return data;
        }

        public async Task<bool> DeleteMultipleAsync(List<Guid> Ids, string modifiedBy = "")
        {
            foreach (var id in Ids)
            {
                var data = await _entities.FindAsync(id);
                if (data == null)
                {
                    continue;
                }
                data.ModifiedBy = _context.getUserId();
                if (modifiedBy != "")
                {
                    data.ModifiedBy = modifiedBy;
                }
                data.ModifiedWhen = DateTime.UtcNow;
                data.Deleted = true;
            }
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteMultipleAsync(Expression<Func<TEntity, bool>> where, string modifiedBy = "")
        {
            var data = await _entities.Where(where).ToListAsync();
            foreach (var item in data)
            {
                item.ModifiedBy = _context.getUserId();
                if (modifiedBy != "")
                {
                    item.ModifiedBy = modifiedBy;
                }
                item.ModifiedWhen = DateTime.UtcNow;
                item.Deleted = true;
            }
            await _context.SaveChangesAsync();
            return true;
        }
        /// <summary>
        /// Get list of entities with pagination
        /// </summary>
        /// <param name="where">Predicate</param>
        /// <param name="PageNumber">Page Number (should be greater than 0)</param>
        /// <param name="PageSize">Page Size (should be greate than 0)</param>
        /// <param name="OrderBy">0=>ascending 1=>descending</param>
        /// <returns></returns>
        public async Task<(int, List<TEntity>)> GetListWithPagination(Expression<Func<TEntity, bool>> where, int PageNumber, int PageSize, int? OrderBy = 0)
        {
            int skip = (PageNumber - 1) * PageSize;
            //int take = PageSize * PageNumber;
            int take = PageSize;
            var initial = _entities.Where(where).OrderByDescending(x => x.CreatedWhen).AsNoTracking();
            var total = initial.Count();
            if (OrderBy == 1)
            {
                return (total, await initial.Skip(skip).Take(take).ToListAsync());
            }
            else
            {
                return (total, await initial.Skip(skip).Take(take).ToListAsync());
            }
        }
        /// <summary>
        /// Get list of entities with pagination
        /// </summary>
        /// <param name="PageNumber">Page Number (should be greater than 0)</param>
        /// <param name="PageSize">Page Size (should be greate than 0)</param>
        /// <param name="OrderBy">0=>ascending 1=>descending</param>
        /// <returns></returns>
        public async Task<(int, List<TEntity>)> GetAllWithPagination(int PageNumber, int PageSize, int? OrderBy = 0)
        {
            int skip = (PageNumber - 1) * PageSize;
            int take = PageSize;
            var initial = _entities.AsNoTracking().OrderByDescending(x => x.CreatedWhen);
            var total = initial.Count();
            if (OrderBy == 1)
            {
                return (total, await initial.Skip(skip).Take(take).ToListAsync());
            }
            else
            {
                return (total, await initial.Skip(skip).Take(take).ToListAsync());
            }
        }

        public IQueryable<TEntity> GetWithInclude(Expression<Func<TEntity, bool>> predicate, params string[] include)
        {
            bool includeDeleted = include.Contains("deleted");
            include = include.Where(x => x != "deleted").ToArray();
            IQueryable<TEntity> query = _entities;
            query = include.Aggregate(query, (current, inc) => current.Include(inc));
            if (includeDeleted)
            {
                return query.IgnoreQueryFilters().Where(predicate).OrderByDescending(d => d.CreatedWhen).AsNoTracking();
            }
            else
            {
                return query.Where(predicate).Where(x => x.Deleted == false).OrderByDescending(d => d.CreatedWhen).AsNoTracking();
            }
        }

        public (IQueryable<TEntity>, int) GetWithIncludePaginatedQueryAble(Expression<Func<TEntity, bool>> predicate, int PageNumber, int PageSize, params string[] include)
        {
            int skip = (PageNumber - 1) * PageSize;
            int take = PageSize;
            IQueryable<TEntity> query = _entities;
            query = include.Aggregate(query, (current, inc) => current.Include(inc));
            var total = query.AsNoTracking().Where(predicate).Count();
            return (query.Where(predicate).OrderByDescending(d => d.CreatedWhen).AsNoTracking().Skip(skip).Take(take), total);
        }
        public (IQueryable<TEntity>, int) GetPaginatedQueryAble(Expression<Func<TEntity, bool>> predicate, int PageNumber, int PageSize)
        {
            int skip = (PageNumber - 1) * PageSize;
            int take = PageSize;
            IQueryable<TEntity> query = _entities;
            var total = query.AsNoTracking().Where(predicate).Count();
            return (query.Where(predicate).OrderByDescending(d => d.CreatedWhen).Skip(skip).Take(take).AsNoTracking(), total);
        }

        public (IQueryable<TEntity>, int) GetWithIncludeQueryAbleWithoutOrder(Expression<Func<TEntity, bool>> predicate, params string[] include)
        {
            IQueryable<TEntity> query = _entities;
            query = include.Aggregate(query, (current, inc) => current.Include(inc));
            var total = query.AsNoTracking().Where(predicate).Count();
            return (query.Where(predicate).AsNoTracking(), total);
        }

        public async Task<(int, IList<TEntity>)> GetPaginationWithIncludeAsync(Expression<Func<TEntity, bool>> predicate, int PageNumber, int PageSize, int? OrderBy = 0, params string[] include)
        {
            int skip = (PageNumber - 1) * PageSize;
            IQueryable<TEntity> query = _entities;
            query = include.Aggregate(query, (current, inc) => current.Include(inc));
            var initial = query.Where(predicate).OrderByDescending(d => d.CreatedWhen).AsNoTracking();

            var total = initial.Count();
            if (OrderBy == 1)
            {
                return (total, await initial.Skip(skip).Take(PageSize).ToListAsync());
            }
            else
            {
                return (total, await initial.Skip(skip).Take(PageSize).ToListAsync());
            }
        }



        public TEntity GetOneDefaultWithInclude(Expression<Func<TEntity, bool>> predicate, params string[] include)
        {
            IQueryable<TEntity> query = _entities;
            query = include.Aggregate(query, (current, inc) => current.Include(inc));
            return query.Where(predicate).AsNoTracking().FirstOrDefault();
        }


        public async Task<bool> Exists(Expression<Func<TEntity, bool>> predicate)
        {
            return await _entities.Where(x => x.Deleted == false).AsNoTracking().AnyAsync(predicate);
        }

        public async Task<bool> Exists(object primaryKey)
        {
            return await _entities.FindAsync(primaryKey) != null;
        }

        public async Task<List<TEntity>> GetMany(Expression<Func<TEntity, bool>> where)
        {
            return await _entities.Where(where).Where(x => x.Deleted == false).OrderByDescending(d => d.CreatedWhen).AsNoTracking().ToListAsync();
        }


        public IQueryable<TEntity> GetManyIQueryable(Expression<Func<TEntity, bool>> where)
        {
            return _entities.Where(where).Where(x => x.Deleted == false).AsQueryable().OrderByDescending(d => d.CreatedWhen).AsNoTracking();
        }
        public IQueryable<TEntity> GetManyIQueryableWithDeleted(Expression<Func<TEntity, bool>> where)
        {
            return _entities.IgnoreQueryFilters().Where(where).AsQueryable().OrderByDescending(d => d.CreatedWhen).AsNoTracking();
        }


        public async Task<TEntity> GetFirst(Expression<Func<TEntity, bool>> predicate, int? OrderBy = 1)
        {
            if (OrderBy == 0)
            {
                return await _entities.Where(x => x.Deleted == false).OrderByDescending(x => x.CreatedWhen).AsNoTracking().FirstOrDefaultAsync(predicate);
            }
            else
            {
                return await _entities.Where(x => x.Deleted == false).AsNoTracking().FirstOrDefaultAsync(predicate);
            }
        }

        public async Task<IQueryable<TEntity>> SortQueryable(IQueryable<TEntity> records, string orderByField, bool ascending = true)
        {
            if (orderByField.Split('.').Length > 1)
            {
                records = ascending ? records.OrderBy(GetNestedOrderExpression(orderByField)) : records.OrderByDescending(GetNestedOrderExpression(orderByField));
            }
            else
            {
                records = ascending ? records.OrderBy(GetOrderExpression(orderByField)) : records.OrderByDescending(GetOrderExpression(orderByField));
            }
            return records;
        }

        public async Task<(int, IQueryable<TEntity>)> PaginateQueryable(IQueryable<TEntity> records, int pageNumber, int pageSize)
        {
            pageNumber = pageNumber < 1 ? 1 : pageNumber;
            int skip = (pageNumber - 1) * pageSize;
            int total = await records.CountAsync();
            var result = records.Skip(skip).Take(pageSize);
            return (total, result);
        }
        public async Task<(int, IList<TEntity>)> GetPaginationWithIncludeAndOrderAsync(Expression<Func<TEntity, bool>> predicate, int pageNumber, int pageSize, string orderByField, bool ascending = true, params string[] include)
        {
            pageNumber = pageNumber < 1 ? 1 : pageNumber;
            int skip = (pageNumber - 1) * pageSize;
            IQueryable<TEntity> query = _entities;
            query = include.Aggregate(query, (current, inc) => current.Include(inc));

            query = query.Where(predicate).AsNoTracking();

            if (orderByField.Split('.').Length > 1)
            {
                query = ascending ? query.OrderBy(GetNestedOrderExpression(orderByField)) : query.OrderByDescending(GetNestedOrderExpression(orderByField));
            }
            else
            {
                query = ascending ? query.OrderBy(GetOrderExpression(orderByField)) : query.OrderByDescending(GetOrderExpression(orderByField));
            }

            int total = await query.CountAsync();
            var result = await query.Skip(skip).Take(pageSize).ToListAsync();

            return (total, result);
        }
        public async Task<(int, List<TEntity>)> GetListWithPaginationAndOrder(Expression<Func<TEntity, bool>> where, int pageNumber, int pageSize, string orderByField, bool ascending = true)
        {
            pageNumber = pageNumber < 1 ? 1 : pageNumber;
            int skip = (pageNumber - 1) * pageSize;
            int take = pageSize;

            var query = _entities.Where(where).AsNoTracking();

            // Apply ordering dynamically based on the orderByField and the ascending flag
            // If ascending is true, use OrderBy; otherwise, use OrderByDescending
            query = ascending ? query.OrderBy(GetOrderExpression(orderByField)) : query.OrderByDescending(GetOrderExpression(orderByField));
            int total = await query.CountAsync();
            var result = await query.Skip(skip).Take(take).ToListAsync();

            // Return the total count and the list of entities for the requested page
            return (total, result);
        }

        // Method to create a dynamic expression for ordering based on a field name
        private Expression<Func<TEntity, object>> GetOrderExpression(string orderByField)
        {
            // Create a parameter expression representing an instance of TEntity (e.g., x => ...)
            var parameter = Expression.Parameter(typeof(TEntity), "x");
            // Access the property of TEntity specified by orderByField (e.g., x => x.Property)
            var property = Expression.Property(parameter, orderByField);
            // Convert the property to an object type to create a compatible expression (e.g., x => (object)x.Property)

            Expression propertyWithDefault;

            // If the property type is a value type (e.g., int, double, DateTime), provide a type-specific default value
            if (property.Type == typeof(string))
            {
                // If the property type is a reference type (e.g., string, object), use an empty string or appropriate reference type default
                var defaultValue = Expression.Constant(property.Type == typeof(string) ? string.Empty : null, property.Type);
                propertyWithDefault = Expression.Coalesce(property, defaultValue);
            }
            else
            {
                propertyWithDefault = property;
            }
            Expression converted;
            if (property.Type == typeof(string))
            {
                converted = Expression.Convert(propertyWithDefault, typeof(string));
            }
            else
            {
                converted = Expression.Convert(propertyWithDefault, typeof(object));
            }
            // Create and return the lambda expression for ordering (e.g., x => (object)x.Property)
            return Expression.Lambda<Func<TEntity, object>>(converted, parameter);
        }

        // Method to create a dynamic expression for ordering based on a nested field name
        private Expression<Func<TEntity, object>> GetNestedOrderExpression(string orderByField)
        {
            // Create a parameter expression representing an instance of TEntity (e.g., x => ...)
            var parameter = Expression.Parameter(typeof(TEntity), "x");
            // Split the orderByField by '.' to support nested properties (e.g., "User.Name")
            string[] properties = orderByField.Split('.');
            Expression property = parameter;
            // Traverse the properties to build the full property access expression
            foreach (var prop in properties)
            {
                property = Expression.Property(property, prop);
            }


            Expression propertyWithDefault;
            // If the property type is a value type (e.g., int, double, DateTime), provide a type-specific default value
            if (property.Type == typeof(string))
            {
                // If the property type is a reference type (e.g., string, object), use an empty string or appropriate reference type default
                var defaultValue = Expression.Constant(property.Type == typeof(string) ? string.Empty : null, property.Type);
                propertyWithDefault = Expression.Coalesce(property, defaultValue);
            }
            else
            {
                propertyWithDefault = property;
            }
            Expression converted;
            if (property.Type == typeof(string))
            {
                converted = Expression.Convert(propertyWithDefault, typeof(string));
            }
            else
            {
                converted = Expression.Convert(propertyWithDefault, typeof(object));
            }
            // Create and return the lambda expression for ordering (e.g., x => (object)x.User.Name)
            return Expression.Lambda<Func<TEntity, object>>(converted, parameter);
        }

    }

    
}


