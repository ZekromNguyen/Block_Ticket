using Event.Application.Common.Interfaces;
using Event.Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

namespace Event.API.Filters;

/// <summary>
/// Authorization filter to validate organization access
/// </summary>
public class OrganizationAuthorizationFilter : IAsyncAuthorizationFilter
{
    private readonly IOrganizationContextProvider _organizationContextProvider;
    private readonly ILogger<OrganizationAuthorizationFilter> _logger;

    public OrganizationAuthorizationFilter(
        IOrganizationContextProvider organizationContextProvider,
        ILogger<OrganizationAuthorizationFilter> logger)
    {
        _organizationContextProvider = organizationContextProvider;
        _logger = logger;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        // Skip authorization for anonymous endpoints
        if (context.ActionDescriptor.EndpointMetadata.Any(m => m is AllowAnonymousAttribute))
        {
            return;
        }

        // Check if user is authenticated
        if (!context.HttpContext.User.Identity?.IsAuthenticated == true)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        try
        {
            // Validate organization context
            var organizationId = _organizationContextProvider.GetCurrentOrganizationIdOrNull();
            if (organizationId == null)
            {
                _logger.LogWarning("No organization context found for authenticated user {UserId}", 
                    _organizationContextProvider.GetCurrentUserIdOrNull());
                
                context.Result = new ForbidResult("No organization context available");
                return;
            }

            // Check if route contains organization ID parameter
            if (context.RouteData.Values.TryGetValue("organizationId", out var routeOrgId))
            {
                if (Guid.TryParse(routeOrgId?.ToString(), out var requestedOrgId))
                {
                    if (!_organizationContextProvider.HasAccessToOrganization(requestedOrgId))
                    {
                        _logger.LogWarning("User {UserId} attempted to access organization {RequestedOrgId} but belongs to {CurrentOrgId}",
                            _organizationContextProvider.GetCurrentUserIdOrNull(), requestedOrgId, organizationId);
                        
                        context.Result = new ForbidResult("Access denied to requested organization");
                        return;
                    }
                }
            }

            _logger.LogDebug("Organization authorization successful for user {UserId} in organization {OrganizationId}",
                _organizationContextProvider.GetCurrentUserIdOrNull(), organizationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during organization authorization");
            context.Result = new StatusCodeResult(500);
        }
    }
}

/// <summary>
/// Attribute to require organization authorization
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RequireOrganizationAttribute : Attribute, IFilterFactory
{
    public bool IsReusable => false;

    public IFilterMetadata CreateInstance(IServiceProvider serviceProvider)
    {
        return serviceProvider.GetRequiredService<OrganizationAuthorizationFilter>();
    }
}

/// <summary>
/// Action filter to validate organization ownership of entities
/// </summary>
public class ValidateEntityOrganizationFilter : IAsyncActionFilter
{
    private readonly IOrganizationContextProvider _organizationContextProvider;
    private readonly ILogger<ValidateEntityOrganizationFilter> _logger;

    public ValidateEntityOrganizationFilter(
        IOrganizationContextProvider organizationContextProvider,
        ILogger<ValidateEntityOrganizationFilter> logger)
    {
        _organizationContextProvider = organizationContextProvider;
        _logger = logger;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        try
        {
            // Execute the action
            var executedContext = await next();

            // Validate organization ownership in the result if it's an entity with OrganizationId
            if (executedContext.Result is ObjectResult objectResult && objectResult.Value != null)
            {
                await ValidateEntityOrganization(objectResult.Value, context);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ValidateEntityOrganizationFilter");
            throw;
        }
    }

    private async Task ValidateEntityOrganization(object entity, ActionExecutingContext context)
    {
        // Use reflection to check if entity has OrganizationId property
        var organizationIdProperty = entity.GetType().GetProperty("OrganizationId");
        if (organizationIdProperty?.GetValue(entity) is Guid entityOrgId)
        {
            var currentOrgId = _organizationContextProvider.GetCurrentOrganizationId();
            if (entityOrgId != currentOrgId)
            {
                _logger.LogWarning("User {UserId} attempted to access entity from organization {EntityOrgId} but belongs to {CurrentOrgId}",
                    _organizationContextProvider.GetCurrentUserIdOrNull(), entityOrgId, currentOrgId);
                
                throw new UnauthorizedAccessException("Access denied: Entity belongs to a different organization");
            }
        }

        // Handle collections
        if (entity is IEnumerable<object> collection)
        {
            foreach (var item in collection)
            {
                await ValidateEntityOrganization(item, context);
            }
        }
    }
}

/// <summary>
/// Attribute to validate entity organization ownership
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class ValidateEntityOrganizationAttribute : Attribute, IFilterFactory
{
    public bool IsReusable => false;

    public IFilterMetadata CreateInstance(IServiceProvider serviceProvider)
    {
        return serviceProvider.GetRequiredService<ValidateEntityOrganizationFilter>();
    }
}

/// <summary>
/// Extensions for organization validation
/// </summary>
public static class OrganizationFilterExtensions
{
    /// <summary>
    /// Adds organization authorization services
    /// </summary>
    public static IServiceCollection AddOrganizationAuthorization(this IServiceCollection services)
    {
        services.AddScoped<OrganizationAuthorizationFilter>();
        services.AddScoped<ValidateEntityOrganizationFilter>();
        return services;
    }

    /// <summary>
    /// Configures global organization authorization
    /// </summary>
    public static void ConfigureOrganizationAuthorization(this MvcOptions options)
    {
        // Add global organization authorization filter
        options.Filters.Add<OrganizationAuthorizationFilter>();
    }
}
