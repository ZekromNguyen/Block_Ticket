using Identity.Domain.Entities;

namespace Identity.Domain.Services;

public interface ISessionManagementService
{
    /// <summary>
    /// Validates if a new session can be created for the user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if session can be created, false otherwise</returns>
    Task<bool> CanCreateSessionAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Enforces session limits for a user when creating a new session
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="newSession">The new session being created</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of sessions that were revoked to make room for the new session</returns>
    Task<IEnumerable<UserSession>> EnforceSessionLimitsAsync(Guid userId, UserSession newSession, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the number of active sessions for a user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of active sessions</returns>
    Task<int> GetActiveSessionCountAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active sessions for a user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of active sessions</returns>
    Task<IEnumerable<UserSession>> GetActiveSessionsAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes oldest sessions for a user to enforce limits
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="sessionsToRevoke">Number of sessions to revoke</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of revoked sessions</returns>
    Task<IEnumerable<UserSession>> RevokeOldestSessionsAsync(Guid userId, int sessionsToRevoke, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates session limits configuration and user permissions
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Maximum allowed sessions for the user</returns>
    Task<int> GetMaxAllowedSessionsAsync(Guid userId, CancellationToken cancellationToken = default);
}
