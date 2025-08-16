using Event.Domain.Interfaces;
using Event.Domain.Models;
using Event.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Shared.Common.Models;
using System.Linq.Expressions;

namespace Event.Infrastructure.Persistence.Repositories;

/// <summary>
/// Base repository implementation with common CRUD operations and cursor pagination
/// </summary>
/// <typeparam name="T">Entity type that inherits from BaseEntity</typeparam>
public abstract class BaseRepository<T> : IRepository<T>, ICursorRepository<T> where T : BaseEntity
{
    protected readonly EventDbContext Context;
    protected readonly DbSet<T> DbSet;

    protected BaseRepository(EventDbContext context)
    {
        Context = context ?? throw new ArgumentNullException(nameof(context));
        DbSet = context.Set<T>();
    }

    /// <summary>
    /// Get entity by ID
    /// </summary>
    public virtual async Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbSet.FindAsync(new object[] { id }, cancellationToken);
    }

    /// <summary>
    /// Get entity by ID with includes
    /// </summary>
    public virtual async Task<T?> GetByIdAsync(Guid id, params Expression<Func<T, object>>[] includes)
    {
        IQueryable<T> query = DbSet;
        
        foreach (var include in includes)
        {
            query = query.Include(include);
        }
        
        return await query.FirstOrDefaultAsync(e => e.Id == id);
    }

    /// <summary>
    /// Get all entities
    /// </summary>
    public virtual async Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet.ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Get entities with predicate
    /// </summary>
    public virtual async Task<IEnumerable<T>> GetAsync(
        Expression<Func<T, bool>>? predicate = null,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
        params Expression<Func<T, object>>[] includes)
    {
        IQueryable<T> query = DbSet;

        if (predicate != null)
        {
            query = query.Where(predicate);
        }

        foreach (var include in includes)
        {
            query = query.Include(include);
        }

        if (orderBy != null)
        {
            query = orderBy(query);
        }

        return await query.ToListAsync();
    }

    /// <summary>
    /// Get paged entities (IRepository interface implementation)
    /// </summary>
    public virtual async Task<(IEnumerable<T> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        Expression<Func<T, bool>>? predicate = null,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null)
    {
        return await GetPagedAsync(pageNumber, pageSize, predicate, orderBy, Array.Empty<Expression<Func<T, object>>>());
    }

    /// <summary>
    /// Get paged entities with includes
    /// </summary>
    public virtual async Task<(IEnumerable<T> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        Expression<Func<T, bool>>? predicate = null,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
        params Expression<Func<T, object>>[] includes)
    {
        IQueryable<T> query = DbSet;

        if (predicate != null)
        {
            query = query.Where(predicate);
        }

        var totalCount = await query.CountAsync();

        foreach (var include in includes)
        {
            query = query.Include(include);
        }

        if (orderBy != null)
        {
            query = orderBy(query);
        }

        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    /// <summary>
    /// Get first entity matching predicate
    /// </summary>
    public virtual async Task<T?> GetFirstAsync(
        Expression<Func<T, bool>> predicate,
        params Expression<Func<T, object>>[] includes)
    {
        IQueryable<T> query = DbSet;

        foreach (var include in includes)
        {
            query = query.Include(include);
        }

        return await query.FirstOrDefaultAsync(predicate);
    }

    /// <summary>
    /// Check if entity exists by predicate
    /// </summary>
    public virtual async Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
    {
        return await DbSet.AnyAsync(predicate, cancellationToken);
    }

    /// <summary>
    /// Check if entity exists by ID (IRepository interface implementation)
    /// </summary>
    public virtual async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbSet.AnyAsync(e => e.Id == id, cancellationToken);
    }

    /// <summary>
    /// Count entities
    /// </summary>
    public virtual async Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken cancellationToken = default)
    {
        if (predicate == null)
        {
            return await DbSet.CountAsync(cancellationToken);
        }

        return await DbSet.CountAsync(predicate, cancellationToken);
    }

    /// <summary>
    /// Add entity
    /// </summary>
    public virtual async Task<T> AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        var result = await DbSet.AddAsync(entity, cancellationToken);
        return result.Entity;
    }

    /// <summary>
    /// Add multiple entities
    /// </summary>
    public virtual async Task AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
    {
        await DbSet.AddRangeAsync(entities, cancellationToken);
    }

    /// <summary>
    /// Update entity
    /// </summary>
    public virtual void Update(T entity)
    {
        DbSet.Update(entity);
    }

    /// <summary>
    /// Update entity async (IRepository interface implementation)
    /// </summary>
    public virtual async Task UpdateAsync(T entity, CancellationToken cancellationToken = default)
    {
        Update(entity);
        await SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Update multiple entities
    /// </summary>
    public virtual void UpdateRange(IEnumerable<T> entities)
    {
        DbSet.UpdateRange(entities);
    }

    /// <summary>
    /// Delete entity
    /// </summary>
    public virtual void Delete(T entity)
    {
        DbSet.Remove(entity);
    }

    /// <summary>
    /// Delete entity by ID
    /// </summary>
    public virtual async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await GetByIdAsync(id, cancellationToken);
        if (entity != null)
        {
            Delete(entity);
        }
    }

    /// <summary>
    /// Delete entity async (IRepository interface implementation)
    /// </summary>
    public virtual async Task DeleteAsync(T entity, CancellationToken cancellationToken = default)
    {
        Delete(entity);
        await SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Delete multiple entities
    /// </summary>
    public virtual void DeleteRange(IEnumerable<T> entities)
    {
        DbSet.RemoveRange(entities);
    }

    /// <summary>
    /// Soft delete entity (if it inherits from BaseAuditableEntity)
    /// </summary>
    public virtual void SoftDelete(T entity)
    {
        if (entity is BaseAuditableEntity auditableEntity)
        {
            auditableEntity.Delete();
            Update(entity);
        }
        else
        {
            Delete(entity);
        }
    }

    /// <summary>
    /// Save changes to database
    /// </summary>
    public virtual async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await Context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Begin database transaction
    /// </summary>
    public virtual async Task<Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        return await Context.Database.BeginTransactionAsync(cancellationToken);
    }

    /// <summary>
    /// Get queryable for advanced queries
    /// </summary>
    protected virtual IQueryable<T> GetQueryable()
    {
        return DbSet.AsQueryable();
    }

    /// <summary>
    /// Get queryable with no tracking for read-only operations
    /// </summary>
    protected virtual IQueryable<T> GetQueryableNoTracking()
    {
        return DbSet.AsNoTracking();
    }

    #region Cursor Pagination Implementation

    /// <summary>
    /// Get entities using cursor-based pagination with dual sort fields
    /// </summary>
    public virtual async Task<CursorPagedResult<T>> GetCursorPagedAsync<TPrimary, TSecondary>(
        CursorPaginationParams paginationParams,
        Expression<Func<T, bool>>? predicate = null,
        Expression<Func<T, TPrimary>>? primarySelector = null,
        Expression<Func<T, TSecondary>>? secondarySelector = null,
        bool primaryDescending = false,
        bool secondaryDescending = false,
        CancellationToken cancellationToken = default)
        where TPrimary : IComparable<TPrimary>
        where TSecondary : IComparable<TSecondary>
    {
        paginationParams.Validate();

        // Default selectors if not provided (use Created and Id for BaseEntity)
        primarySelector ??= CreateDefaultPrimarySelector<TPrimary>();
        secondarySelector ??= CreateDefaultSecondarySelector<TSecondary>();

        var isBackward = paginationParams.Direction == PaginationDirection.Backward;
        var pageSize = paginationParams.EffectivePageSize;
        var cursor = Cursor.TryDecode(paginationParams.EffectiveCursor);

        // Build base query
        IQueryable<T> query = GetQueryableNoTracking();

        // Apply predicate filter
        if (predicate != null)
        {
            query = query.Where(predicate);
        }

        // Apply cursor filter
        if (cursor != null)
        {
            query = CursorQueryBuilder.ApplyCursorFilter(
                query, cursor, primarySelector, secondarySelector,
                primaryDescending, secondaryDescending, isBackward);
        }

        // Apply sorting
        var sortedQuery = CursorQueryBuilder.ApplySorting(
            query, primarySelector, secondarySelector,
            primaryDescending, secondaryDescending, isBackward);

        // Fetch one extra item to determine if there are more pages
        var items = await sortedQuery
            .Take(pageSize + 1)
            .ToListAsync(cancellationToken);

        // Check if there are more items
        var hasMore = items.Count > pageSize;
        if (hasMore)
        {
            items = items.Take(pageSize).ToList();
        }

        // For backward pagination, reverse the items to maintain correct order
        if (isBackward)
        {
            items.Reverse();
        }

        // Generate cursors
        string? nextCursor = null;
        string? previousCursor = null;

        if (items.Any())
        {
            if (!isBackward && hasMore)
            {
                // Forward pagination with more items
                nextCursor = CursorQueryBuilder.CreateCursor(items.Last(), primarySelector, secondarySelector);
            }
            else if (isBackward && hasMore)
            {
                // Backward pagination with more items
                previousCursor = CursorQueryBuilder.CreateCursor(items.First(), primarySelector, secondarySelector);
            }

            if (cursor != null)
            {
                // If we used a cursor, there might be items in the opposite direction
                if (isBackward)
                {
                    nextCursor = CursorQueryBuilder.CreateCursor(items.Last(), primarySelector, secondarySelector);
                }
                else
                {
                    previousCursor = CursorQueryBuilder.CreateCursor(items.First(), primarySelector, secondarySelector);
                }
            }
        }

        // Get total count if requested
        int? totalCount = null;
        if (paginationParams.IncludeTotalCount)
        {
            var baseQuery = GetQueryableNoTracking();
            if (predicate != null)
            {
                baseQuery = baseQuery.Where(predicate);
            }
            totalCount = await GetEfficientCountAsync(baseQuery, true, 10000, cancellationToken);
        }

        return new CursorPagedResult<T>
        {
            Items = items,
            NextCursor = nextCursor,
            PreviousCursor = previousCursor,
            HasNextPage = !isBackward ? hasMore : cursor != null,
            HasPreviousPage = isBackward ? hasMore : cursor != null,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    /// <summary>
    /// Get entities using cursor-based pagination with single sort field
    /// </summary>
    public virtual async Task<CursorPagedResult<T>> GetCursorPagedAsync<TSort>(
        CursorPaginationParams paginationParams,
        Expression<Func<T, bool>>? predicate = null,
        Expression<Func<T, TSort>>? sortSelector = null,
        bool sortDescending = false,
        CancellationToken cancellationToken = default)
        where TSort : IComparable<TSort>
    {
        // Use ID as secondary sort for consistency
        return await GetCursorPagedAsync<TSort, Guid>(
            paginationParams, predicate, sortSelector, e => e.Id,
            sortDescending, false, cancellationToken);
    }

    /// <summary>
    /// Get entities using cursor-based pagination with default sorting (Created, Id)
    /// </summary>
    public virtual async Task<CursorPagedResult<T>> GetCursorPagedAsync(
        CursorPaginationParams paginationParams,
        Expression<Func<T, bool>>? predicate = null,
        CancellationToken cancellationToken = default)
    {
        // Default to Created date descending, then ID ascending
        return await GetCursorPagedAsync<DateTime, Guid>(
            paginationParams, predicate, e => e.Created ?? DateTime.MinValue, e => e.Id,
            true, false, cancellationToken);
    }

    /// <summary>
    /// Create default primary selector
    /// </summary>
    private Expression<Func<T, TPrimary>> CreateDefaultPrimarySelector<TPrimary>()
    {
        if (typeof(TPrimary) == typeof(DateTime))
        {
            return (Expression<Func<T, TPrimary>>)(Expression<Func<T, DateTime>>)(e => e.Created ?? DateTime.MinValue);
        }
        if (typeof(TPrimary) == typeof(Guid))
        {
            return (Expression<Func<T, TPrimary>>)(Expression<Func<T, Guid>>)(e => e.Id);
        }
        
        throw new NotSupportedException($"Default primary selector not supported for type {typeof(TPrimary)}");
    }

    /// <summary>
    /// Create default secondary selector
    /// </summary>
    private Expression<Func<T, TSecondary>> CreateDefaultSecondarySelector<TSecondary>()
    {
        if (typeof(TSecondary) == typeof(Guid))
        {
            return (Expression<Func<T, TSecondary>>)(Expression<Func<T, Guid>>)(e => e.Id);
        }
        if (typeof(TSecondary) == typeof(DateTime))
        {
            return (Expression<Func<T, TSecondary>>)(Expression<Func<T, DateTime>>)(e => e.Created ?? DateTime.MinValue);
        }
        
        throw new NotSupportedException($"Default secondary selector not supported for type {typeof(TSecondary)}");
    }

    /// <summary>
    /// Get efficient count for cursor pagination (with optional limit for performance)
    /// </summary>
    protected async Task<int?> GetEfficientCountAsync(
        IQueryable<T> baseQuery, 
        bool includeTotalCount, 
        int? maxCountLimit = 10000,
        CancellationToken cancellationToken = default)
    {
        if (!includeTotalCount) return null;

        // For performance, limit the count query if the dataset is very large
        if (maxCountLimit.HasValue)
        {
            var limitedQuery = baseQuery.Take(maxCountLimit.Value + 1);
            var count = await limitedQuery.CountAsync(cancellationToken);
            return count > maxCountLimit.Value ? null : count; // Return null if exceeds limit
        }

        return await baseQuery.CountAsync(cancellationToken);
    }

    #endregion
}
