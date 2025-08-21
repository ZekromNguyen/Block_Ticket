namespace Event.Application.Common.Interfaces;

/// <summary>
/// Provides organization context for the current user/request
/// </summary>
public interface IOrganizationContextProvider
{
    /// <summary>
    /// Gets the current organization ID from the authenticated user context
    /// </summary>
    Guid GetCurrentOrganizationId();

    /// <summary>
    /// Gets the current organization ID, returning null if not available
    /// </summary>
    Guid? GetCurrentOrganizationIdOrNull();

    /// <summary>
    /// Gets the current user ID from the authenticated user context
    /// </summary>
    Guid GetCurrentUserId();

    /// <summary>
    /// Gets the current user ID, returning null if not available
    /// </summary>
    Guid? GetCurrentUserIdOrNull();

    /// <summary>
    /// Validates that the current user has access to the specified organization
    /// </summary>
    bool HasAccessToOrganization(Guid organizationId);

    /// <summary>
    /// Gets all organization IDs that the current user has access to
    /// </summary>
    Task<IEnumerable<Guid>> GetAccessibleOrganizationIdsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current correlation ID for request tracing
    /// </summary>
    string GetCorrelationId();

    /// <summary>
    /// Sets the organization context for the current request (used in testing)
    /// </summary>
    void SetOrganizationContext(Guid organizationId, Guid userId);

    /// <summary>
    /// Clears the organization context (used in testing)
    /// </summary>
    void ClearOrganizationContext();
}
