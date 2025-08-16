using Event.Domain.Models;
using System.Linq.Expressions;

namespace Event.Domain.Interfaces;

/// <summary>
/// Interface for repositories that support cursor-based pagination
/// </summary>
public interface ICursorRepository<T> where T : class
{
    /// <summary>
    /// Get entities using cursor-based pagination
    /// </summary>
    /// <param name="paginationParams">Cursor pagination parameters</param>
    /// <param name="predicate">Optional filter predicate</param>
    /// <param name="primarySelector">Primary sort field selector (e.g., e => e.CreatedAt)</param>
    /// <param name="secondarySelector">Secondary sort field selector for tie-breaking (e.g., e => e.Id)</param>
    /// <param name="primaryDescending">Primary sort direction</param>
    /// <param name="secondaryDescending">Secondary sort direction</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Cursor-paginated result</returns>
    Task<CursorPagedResult<T>> GetCursorPagedAsync<TPrimary, TSecondary>(
        CursorPaginationParams paginationParams,
        Expression<Func<T, bool>>? predicate = null,
        Expression<Func<T, TPrimary>>? primarySelector = null,
        Expression<Func<T, TSecondary>>? secondarySelector = null,
        bool primaryDescending = false,
        bool secondaryDescending = false,
        CancellationToken cancellationToken = default)
        where TPrimary : IComparable<TPrimary>
        where TSecondary : IComparable<TSecondary>;

    /// <summary>
    /// Get entities using cursor-based pagination with single sort field
    /// </summary>
    /// <param name="paginationParams">Cursor pagination parameters</param>
    /// <param name="predicate">Optional filter predicate</param>
    /// <param name="sortSelector">Sort field selector</param>
    /// <param name="sortDescending">Sort direction</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Cursor-paginated result</returns>
    Task<CursorPagedResult<T>> GetCursorPagedAsync<TSort>(
        CursorPaginationParams paginationParams,
        Expression<Func<T, bool>>? predicate = null,
        Expression<Func<T, TSort>>? sortSelector = null,
        bool sortDescending = false,
        CancellationToken cancellationToken = default)
        where TSort : IComparable<TSort>;

    /// <summary>
    /// Get entities using cursor-based pagination with default sorting (CreatedAt, Id)
    /// </summary>
    /// <param name="paginationParams">Cursor pagination parameters</param>
    /// <param name="predicate">Optional filter predicate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Cursor-paginated result</returns>
    Task<CursorPagedResult<T>> GetCursorPagedAsync(
        CursorPaginationParams paginationParams,
        Expression<Func<T, bool>>? predicate = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for entities that support cursor-based pagination
/// </summary>
public interface ICursorEntity
{
    /// <summary>
    /// Primary identifier for the entity
    /// </summary>
    Guid Id { get; }

    /// <summary>
    /// Creation timestamp for default cursor ordering
    /// </summary>
    DateTime CreatedAt { get; }
}

/// <summary>
/// Cursor pagination helper for building database queries
/// </summary>
public static class CursorQueryBuilder
{
    /// <summary>
    /// Build query with cursor-based filtering
    /// </summary>
    public static IQueryable<T> ApplyCursorFilter<T, TPrimary, TSecondary>(
        IQueryable<T> query,
        Cursor? cursor,
        Expression<Func<T, TPrimary>> primarySelector,
        Expression<Func<T, TSecondary>> secondarySelector,
        bool primaryDescending,
        bool secondaryDescending,
        bool isBackward)
        where TPrimary : IComparable<TPrimary>
        where TSecondary : IComparable<TSecondary>
    {
        if (cursor == null) return query;

        try
        {
            var primaryValue = (TPrimary)cursor.PrimaryValue;
            var secondaryValue = cursor.SecondaryValue != null ? (TSecondary)cursor.SecondaryValue : default(TSecondary);

            // Build parameter expressions
            var parameter = Expression.Parameter(typeof(T), "x");
            var primaryProperty = Expression.Invoke(primarySelector, parameter);
            var secondaryProperty = Expression.Invoke(secondarySelector, parameter);

            // Create primary comparison
            var primaryConstant = Expression.Constant(primaryValue);
            Expression primaryComparison;

            if (isBackward)
            {
                // For backward pagination, reverse the comparison
                primaryComparison = primaryDescending 
                    ? Expression.GreaterThan(primaryProperty, primaryConstant)
                    : Expression.LessThan(primaryProperty, primaryConstant);
            }
            else
            {
                // For forward pagination
                primaryComparison = primaryDescending
                    ? Expression.LessThan(primaryProperty, primaryConstant)
                    : Expression.GreaterThan(primaryProperty, primaryConstant);
            }

            Expression finalCondition = primaryComparison;

            // Add secondary comparison for tie-breaking
            if (secondaryValue != null)
            {
                var secondaryConstant = Expression.Constant(secondaryValue);
                var primaryEqual = Expression.Equal(primaryProperty, primaryConstant);

                Expression secondaryComparison;
                if (isBackward)
                {
                    secondaryComparison = secondaryDescending
                        ? Expression.GreaterThan(secondaryProperty, secondaryConstant)
                        : Expression.LessThan(secondaryProperty, secondaryConstant);
                }
                else
                {
                    secondaryComparison = secondaryDescending
                        ? Expression.LessThan(secondaryProperty, secondaryConstant)
                        : Expression.GreaterThan(secondaryProperty, secondaryConstant);
                }

                var secondaryCondition = Expression.AndAlso(primaryEqual, secondaryComparison);
                finalCondition = Expression.OrElse(primaryComparison, secondaryCondition);
            }

            var lambda = Expression.Lambda<Func<T, bool>>(finalCondition, parameter);
            return query.Where(lambda);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to apply cursor filter: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Apply sorting to query
    /// </summary>
    public static IOrderedQueryable<T> ApplySorting<T, TPrimary, TSecondary>(
        IQueryable<T> query,
        Expression<Func<T, TPrimary>> primarySelector,
        Expression<Func<T, TSecondary>> secondarySelector,
        bool primaryDescending,
        bool secondaryDescending,
        bool isBackward)
        where TPrimary : IComparable<TPrimary>
        where TSecondary : IComparable<TSecondary>
    {
        // For backward pagination, we reverse the sort order temporarily
        var effectivePrimaryDesc = isBackward ? !primaryDescending : primaryDescending;
        var effectiveSecondaryDesc = isBackward ? !secondaryDescending : secondaryDescending;

        IOrderedQueryable<T> ordered;

        if (effectivePrimaryDesc)
        {
            ordered = query.OrderByDescending(primarySelector);
        }
        else
        {
            ordered = query.OrderBy(primarySelector);
        }

        if (effectiveSecondaryDesc)
        {
            ordered = ordered.ThenByDescending(secondarySelector);
        }
        else
        {
            ordered = ordered.ThenBy(secondarySelector);
        }

        return ordered;
    }

    /// <summary>
    /// Create cursor from entity using reflection
    /// </summary>
    public static string CreateCursor<T, TPrimary, TSecondary>(
        T entity,
        Expression<Func<T, TPrimary>> primarySelector,
        Expression<Func<T, TSecondary>> secondarySelector)
        where TPrimary : IComparable<TPrimary>
        where TSecondary : IComparable<TSecondary>
    {
        var primaryFunc = primarySelector.Compile();
        var secondaryFunc = secondarySelector.Compile();

        var primaryValue = primaryFunc(entity);
        var secondaryValue = secondaryFunc(entity);

        return new Cursor(primaryValue, secondaryValue).Encode();
    }

    /// <summary>
    /// Get efficient count for cursor pagination (with optional limit for performance)
    /// </summary>
    public static Task<int?> GetCountAsync<T>(
        IQueryable<T> baseQuery, 
        bool includeTotalCount, 
        int? maxCountLimit = 10000,
        CancellationToken cancellationToken = default)
    {
        if (!includeTotalCount) return Task.FromResult<int?>(null);

        // Note: This method signature is defined here but implementation
        // should be in the Infrastructure layer where EntityFramework is available
        throw new NotImplementedException("This method should be implemented in the Infrastructure layer");
    }
}
