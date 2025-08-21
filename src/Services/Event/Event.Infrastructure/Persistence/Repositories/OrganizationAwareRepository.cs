using Event.Application.Common.Interfaces;
using Event.Domain.Exceptions;
using Event.Domain.Interfaces;
using Event.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared.Common.Models;
using System.Linq.Expressions;

namespace Event.Infrastructure.Persistence.Repositories;

/// <summary>
/// Base repository with organization-aware operations for multi-tenant entities
/// </summary>
/// <typeparam name="T">Entity type that has OrganizationId property</typeparam>
public abstract class OrganizationAwareRepository<T> : BaseRepository<T> where T : BaseEntity
{
    protected readonly IOrganizationContextProvider OrganizationContextProvider;
    protected readonly ILogger Logger;

    protected OrganizationAwareRepository(
        EventDbContext context, 
        IOrganizationContextProvider organizationContextProvider,
        ILogger logger) : base(context)
    {
        OrganizationContextProvider = organizationContextProvider;
        Logger = logger;
    }

    /// <summary>
    /// Gets entity by ID with organization validation
    /// </summary>
    public override async Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await base.GetByIdAsync(id, cancellationToken);
        if (entity != null)
        {
            ValidateEntityOrganization(entity);
        }
        return entity;
    }

    /// <summary>
    /// Gets entities by organization
    /// </summary>
    public virtual async Task<IEnumerable<T>> GetByOrganizationAsync(CancellationToken cancellationToken = default)
    {
        var organizationId = OrganizationContextProvider.GetCurrentOrganizationId();
        return await GetByOrganizationAsync(organizationId, cancellationToken);
    }

    /// <summary>
    /// Gets entities by specific organization ID (with access validation)
    /// </summary>
    public virtual async Task<IEnumerable<T>> GetByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        OrganizationContextProvider.ValidateOrganizationAccess(organizationId);
        
        return await DbSet
            .Where(GetOrganizationFilter(organizationId))
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Adds entity with organization context
    /// </summary>
    public override async Task<T> AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        SetEntityOrganization(entity);
        return await base.AddAsync(entity, cancellationToken);
    }

    /// <summary>
    /// Updates entity with organization validation
    /// </summary>
    public override void Update(T entity)
    {
        ValidateEntityOrganization(entity);
        base.Update(entity);
    }

    /// <summary>
    /// Deletes entity with organization validation
    /// </summary>
    public override void Delete(T entity)
    {
        ValidateEntityOrganization(entity);
        base.Delete(entity);
    }

    /// <summary>
    /// Finds entities with organization filtering
    /// </summary>
    public virtual async Task<IEnumerable<T>> FindAsync(
        Expression<Func<T, bool>> predicate, 
        CancellationToken cancellationToken = default)
    {
        var organizationId = OrganizationContextProvider.GetCurrentOrganizationId();
        var organizationFilter = GetOrganizationFilter(organizationId);
        
        // Combine organization filter with the provided predicate
        var combinedFilter = CombineFilters(organizationFilter, predicate);
        
        return await DbSet
            .Where(combinedFilter)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Counts entities with organization filtering
    /// </summary>
    public override async Task<int> CountAsync(
        Expression<Func<T, bool>>? predicate = null,
        CancellationToken cancellationToken = default)
    {
        var organizationId = OrganizationContextProvider.GetCurrentOrganizationId();
        var organizationFilter = GetOrganizationFilter(organizationId);
        
        var query = DbSet.Where(organizationFilter);
        
        if (predicate != null)
        {
            query = query.Where(predicate);
        }
        
        return await query.CountAsync(cancellationToken);
    }

    /// <summary>
    /// Checks if entity exists with organization filtering
    /// </summary>
    public override async Task<bool> ExistsAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        var organizationId = OrganizationContextProvider.GetCurrentOrganizationId();
        var organizationFilter = GetOrganizationFilter(organizationId);
        
        var combinedFilter = CombineFilters(organizationFilter, predicate);
        
        return await DbSet.AnyAsync(combinedFilter, cancellationToken);
    }

    /// <summary>
    /// Gets the organization filter expression for the entity type
    /// </summary>
    protected virtual Expression<Func<T, bool>> GetOrganizationFilter(Guid organizationId)
    {
        // This method should be overridden in derived classes to provide the correct organization filtering
        // For entities with direct OrganizationId property
        var parameter = Expression.Parameter(typeof(T), "x");
        var property = Expression.Property(parameter, "OrganizationId");
        var constant = Expression.Constant(organizationId);
        var equality = Expression.Equal(property, constant);
        
        return Expression.Lambda<Func<T, bool>>(equality, parameter);
    }

    /// <summary>
    /// Sets the organization ID on the entity
    /// </summary>
    protected virtual void SetEntityOrganization(T entity)
    {
        var organizationId = OrganizationContextProvider.GetCurrentOrganizationId();
        
        // Use reflection to set OrganizationId if the property exists
        var organizationIdProperty = typeof(T).GetProperty("OrganizationId");
        if (organizationIdProperty != null && organizationIdProperty.CanWrite)
        {
            organizationIdProperty.SetValue(entity, organizationId);
        }
    }

    /// <summary>
    /// Validates that the entity belongs to the current user's organization
    /// </summary>
    protected virtual void ValidateEntityOrganization(T entity)
    {
        var currentOrgId = OrganizationContextProvider.GetCurrentOrganizationId();
        
        // Use reflection to get OrganizationId from the entity
        var organizationIdProperty = typeof(T).GetProperty("OrganizationId");
        if (organizationIdProperty?.GetValue(entity) is Guid entityOrgId)
        {
            if (entityOrgId != currentOrgId)
            {
                Logger.LogWarning("Organization access violation: User {UserId} from organization {CurrentOrgId} attempted to access entity {EntityId} from organization {EntityOrgId}",
                    OrganizationContextProvider.GetCurrentUserIdOrNull(), currentOrgId, entity.Id, entityOrgId);
                
                throw new UnauthorizedAccessException($"Access denied: Entity belongs to organization {entityOrgId} but current user belongs to organization {currentOrgId}");
            }
        }
    }

    /// <summary>
    /// Combines two filter expressions with AND logic
    /// </summary>
    protected static Expression<Func<T, bool>> CombineFilters(
        Expression<Func<T, bool>> filter1, 
        Expression<Func<T, bool>> filter2)
    {
        var parameter = Expression.Parameter(typeof(T), "x");
        
        var body1 = ReplaceParameter(filter1.Body, filter1.Parameters[0], parameter);
        var body2 = ReplaceParameter(filter2.Body, filter2.Parameters[0], parameter);
        
        var combined = Expression.AndAlso(body1, body2);
        
        return Expression.Lambda<Func<T, bool>>(combined, parameter);
    }

    /// <summary>
    /// Replaces parameter in expression
    /// </summary>
    private static Expression ReplaceParameter(Expression expression, ParameterExpression oldParameter, ParameterExpression newParameter)
    {
        return new ParameterReplacer(oldParameter, newParameter).Visit(expression);
    }

    /// <summary>
    /// Expression visitor to replace parameters
    /// </summary>
    private class ParameterReplacer : ExpressionVisitor
    {
        private readonly ParameterExpression _oldParameter;
        private readonly ParameterExpression _newParameter;

        public ParameterReplacer(ParameterExpression oldParameter, ParameterExpression newParameter)
        {
            _oldParameter = oldParameter;
            _newParameter = newParameter;
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            return node == _oldParameter ? _newParameter : base.VisitParameter(node);
        }
    }
}

/// <summary>
/// Extensions for organization-aware repositories
/// </summary>
public static class OrganizationAwareRepositoryExtensions
{
    /// <summary>
    /// Validates that all entities in the collection belong to the current organization
    /// </summary>
    public static void ValidateOrganizationOwnership<T>(
        this IOrganizationContextProvider provider, 
        IEnumerable<T> entities) where T : BaseEntity
    {
        var currentOrgId = provider.GetCurrentOrganizationId();
        
        foreach (var entity in entities)
        {
            var organizationIdProperty = typeof(T).GetProperty("OrganizationId");
            if (organizationIdProperty?.GetValue(entity) is Guid entityOrgId)
            {
                if (entityOrgId != currentOrgId)
                {
                    throw new UnauthorizedAccessException($"Access denied: Entity {entity.Id} belongs to a different organization");
                }
            }
        }
    }
}
