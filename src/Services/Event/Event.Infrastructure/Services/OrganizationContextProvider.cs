using Event.Application.Common.Interfaces;
using Event.Domain.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace Event.Infrastructure.Services;

/// <summary>
/// Implementation of organization context provider using HTTP context and JWT claims
/// </summary>
public class OrganizationContextProvider : IOrganizationContextProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<OrganizationContextProvider> _logger;
    
    // For testing scenarios
    private static readonly AsyncLocal<OrganizationContext?> _testContext = new();

    public OrganizationContextProvider(
        IHttpContextAccessor httpContextAccessor,
        ILogger<OrganizationContextProvider> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public Guid GetCurrentOrganizationId()
    {
        var organizationId = GetCurrentOrganizationIdOrNull();
        if (organizationId == null)
        {
            throw new UnauthorizedAccessException("No organization context available for the current user");
        }
        return organizationId.Value;
    }

    public Guid? GetCurrentOrganizationIdOrNull()
    {
        // Check test context first (for unit testing)
        if (_testContext.Value != null)
        {
            return _testContext.Value.OrganizationId;
        }

        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext?.User?.Identity?.IsAuthenticated != true)
        {
            _logger.LogWarning("No authenticated user found in HTTP context");
            return null;
        }

        // Try to get organization ID from JWT claims
        var orgClaim = httpContext.User.FindFirst("organization_id") ?? 
                      httpContext.User.FindFirst("org_id") ??
                      httpContext.User.FindFirst(ClaimTypes.GroupSid);

        if (orgClaim != null && Guid.TryParse(orgClaim.Value, out var organizationId))
        {
            return organizationId;
        }

        // Try to get from custom header (for service-to-service calls)
        if (httpContext.Request.Headers.TryGetValue("X-Organization-Id", out var headerValue))
        {
            if (Guid.TryParse(headerValue.FirstOrDefault(), out var headerOrgId))
            {
                return headerOrgId;
            }
        }

        _logger.LogWarning("No organization ID found in user claims or headers for user {UserId}", 
            GetCurrentUserIdOrNull());
        
        return null;
    }

    public Guid GetCurrentUserId()
    {
        var userId = GetCurrentUserIdOrNull();
        if (userId == null)
        {
            throw new UnauthorizedAccessException("No user context available");
        }
        return userId.Value;
    }

    public Guid? GetCurrentUserIdOrNull()
    {
        // Check test context first
        if (_testContext.Value != null)
        {
            return _testContext.Value.UserId;
        }

        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext?.User?.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        // Try to get user ID from JWT claims
        var userClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier) ??
                       httpContext.User.FindFirst("sub") ??
                       httpContext.User.FindFirst("user_id");

        if (userClaim != null && Guid.TryParse(userClaim.Value, out var userId))
        {
            return userId;
        }

        return null;
    }

    public bool HasAccessToOrganization(Guid organizationId)
    {
        var currentOrgId = GetCurrentOrganizationIdOrNull();
        if (currentOrgId == null)
        {
            return false;
        }

        // For now, users only have access to their primary organization
        // In a more complex system, this could check multiple organization memberships
        return currentOrgId.Value == organizationId;
    }

    public async Task<IEnumerable<Guid>> GetAccessibleOrganizationIdsAsync(CancellationToken cancellationToken = default)
    {
        var currentOrgId = GetCurrentOrganizationIdOrNull();
        if (currentOrgId == null)
        {
            return Enumerable.Empty<Guid>();
        }

        // For now, return only the current organization
        // In a more complex system, this could query a user-organization membership table
        return new[] { currentOrgId.Value };
    }

    public string GetCorrelationId()
    {
        // Check test context first
        if (_testContext.Value != null)
        {
            return _testContext.Value.CorrelationId ?? Guid.NewGuid().ToString();
        }

        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext != null)
        {
            // Try to get correlation ID from headers
            if (httpContext.Request.Headers.TryGetValue("X-Correlation-ID", out var correlationId))
            {
                return correlationId.FirstOrDefault() ?? Guid.NewGuid().ToString();
            }

            // Try to get from trace identifier
            if (!string.IsNullOrEmpty(httpContext.TraceIdentifier))
            {
                return httpContext.TraceIdentifier;
            }
        }

        return Guid.NewGuid().ToString();
    }

    public void SetOrganizationContext(Guid organizationId, Guid userId)
    {
        _testContext.Value = new OrganizationContext
        {
            OrganizationId = organizationId,
            UserId = userId,
            CorrelationId = Guid.NewGuid().ToString()
        };
    }

    public void ClearOrganizationContext()
    {
        _testContext.Value = null;
    }

    /// <summary>
    /// Internal class for test context
    /// </summary>
    private class OrganizationContext
    {
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }
        public string? CorrelationId { get; set; }
    }
}

/// <summary>
/// Extensions for organization context validation
/// </summary>
public static class OrganizationContextExtensions
{
    /// <summary>
    /// Validates that the current user has access to the specified organization
    /// </summary>
    public static void ValidateOrganizationAccess(this IOrganizationContextProvider provider, Guid organizationId)
    {
        if (!provider.HasAccessToOrganization(organizationId))
        {
            throw new UnauthorizedAccessException($"User does not have access to organization {organizationId}");
        }
    }

    /// <summary>
    /// Ensures that the entity belongs to the current user's organization
    /// </summary>
    public static void ValidateEntityOrganization(this IOrganizationContextProvider provider, Guid entityOrganizationId, string entityType = "entity")
    {
        var currentOrgId = provider.GetCurrentOrganizationId();
        if (entityOrganizationId != currentOrgId)
        {
            throw new EventDomainException($"Access denied: {entityType} belongs to a different organization");
        }
    }
}
