using Identity.Domain.Configuration;
using Identity.Domain.Entities;
using Identity.Domain.Repositories;
using Identity.Domain.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Identity.Infrastructure.Services;

public class SessionManagementService : ISessionManagementService
{
    private readonly IUserSessionRepository _sessionRepository;
    private readonly IUserRepository _userRepository;
    private readonly ITokenService _tokenService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SessionManagementService> _logger;
    private readonly SessionConfiguration _sessionConfig;

    public SessionManagementService(
        IUserSessionRepository sessionRepository,
        IUserRepository userRepository,
        ITokenService tokenService,
        IConfiguration configuration,
        ILogger<SessionManagementService> logger)
    {
        _sessionRepository = sessionRepository;
        _userRepository = userRepository;
        _tokenService = tokenService;
        _configuration = configuration;
        _logger = logger;

        // Load session configuration
        _sessionConfig = new SessionConfiguration();
        _configuration.GetSection("Security").Bind(_sessionConfig);
    }

    public async Task<bool> CanCreateSessionAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_sessionConfig.EnableSessionLimits || _sessionConfig.SessionLimitBehavior == SessionLimitBehavior.Unlimited)
            {
                return true;
            }

            var maxAllowedSessions = await GetMaxAllowedSessionsAsync(userId, cancellationToken);
            var activeSessionCount = await GetActiveSessionCountAsync(userId, cancellationToken);

            // If we're at or over the limit and behavior is to reject new sessions
            if (_sessionConfig.SessionLimitBehavior == SessionLimitBehavior.RejectNew && activeSessionCount >= maxAllowedSessions)
            {
                _logger.LogWarning("Session creation rejected for user {UserId}. Active sessions: {ActiveSessions}, Max allowed: {MaxAllowed}", 
                    userId, activeSessionCount, maxAllowedSessions);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if session can be created for user {UserId}", userId);
            // Default to allowing session creation in case of errors
            return true;
        }
    }

    public async Task<IEnumerable<UserSession>> EnforceSessionLimitsAsync(Guid userId, UserSession newSession, CancellationToken cancellationToken = default)
    {
        var revokedSessions = new List<UserSession>();

        try
        {
            if (!_sessionConfig.EnableSessionLimits || _sessionConfig.SessionLimitBehavior == SessionLimitBehavior.Unlimited)
            {
                return revokedSessions;
            }

            var maxAllowedSessions = await GetMaxAllowedSessionsAsync(userId, cancellationToken);
            var activeSessions = (await GetActiveSessionsAsync(userId, cancellationToken)).ToList();

            // If we're at the limit and behavior is to revoke oldest sessions
            if (_sessionConfig.SessionLimitBehavior == SessionLimitBehavior.RevokeOldest && activeSessions.Count >= maxAllowedSessions)
            {
                var sessionsToRevoke = activeSessions.Count - maxAllowedSessions + 1;
                revokedSessions.AddRange(await RevokeOldestSessionsAsync(userId, sessionsToRevoke, cancellationToken));
            }

            _logger.LogInformation("Session limits enforced for user {UserId}. Revoked {RevokedCount} sessions", 
                userId, revokedSessions.Count);

            return revokedSessions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enforcing session limits for user {UserId}", userId);
            return revokedSessions;
        }
    }

    public async Task<int> GetActiveSessionCountAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var activeSessions = await _sessionRepository.GetActiveSessionsByUserIdAsync(userId, cancellationToken);
            return activeSessions.Count();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active session count for user {UserId}", userId);
            return 0;
        }
    }

    public async Task<IEnumerable<UserSession>> GetActiveSessionsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _sessionRepository.GetActiveSessionsByUserIdAsync(userId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active sessions for user {UserId}", userId);
            return Enumerable.Empty<UserSession>();
        }
    }

    public async Task<IEnumerable<UserSession>> RevokeOldestSessionsAsync(Guid userId, int sessionsToRevoke, CancellationToken cancellationToken = default)
    {
        var revokedSessions = new List<UserSession>();

        try
        {
            if (sessionsToRevoke <= 0)
            {
                return revokedSessions;
            }

            var activeSessions = await _sessionRepository.GetActiveSessionsByUserIdAsync(userId, cancellationToken);
            var oldestSessions = activeSessions
                .OrderBy(s => s.CreatedAt)
                .Take(sessionsToRevoke)
                .ToList();

            foreach (var session in oldestSessions)
            {
                // End the session
                session.End();
                await _sessionRepository.UpdateAsync(session, cancellationToken);

                // Revoke associated tokens
                if (!string.IsNullOrEmpty(session.RefreshToken))
                {
                    await _tokenService.RevokeTokenAsync(session.RefreshToken);
                }

                // Revoke session tokens if session ID is available
                await _tokenService.RevokeSessionTokensAsync(session.Id.ToString());

                revokedSessions.Add(session);

                _logger.LogInformation("Revoked session {SessionId} for user {UserId} due to session limit enforcement", 
                    session.Id, userId);
            }

            return revokedSessions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking oldest sessions for user {UserId}", userId);
            return revokedSessions;
        }
    }

    public async Task<int> GetMaxAllowedSessionsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            // Base limit from configuration
            var baseLimit = _sessionConfig.MaxConcurrentSessions;

            // You can extend this to check user roles or subscription tiers
            // For example, premium users might get more sessions
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user == null)
            {
                return baseLimit;
            }

            // Check for admin/super_admin roles - they might get unlimited sessions
            var userRoles = user.GetActiveRoles().Select(r => r.Name.ToLower()).ToList();
            if (userRoles.Contains("admin") || userRoles.Contains("super_admin"))
            {
                // Admins get double the normal limit
                return baseLimit * 2;
            }

            // Check for promoter role - they might need more sessions for events management
            if (userRoles.Contains("promoter"))
            {
                // Promoters get 50% more sessions
                return (int)(baseLimit * 1.5);
            }

            return baseLimit;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting max allowed sessions for user {UserId}", userId);
            return _sessionConfig.MaxConcurrentSessions;
        }
    }
}
