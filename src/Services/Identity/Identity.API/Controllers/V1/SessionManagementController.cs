using Identity.Application.DTOs;
using Identity.Domain.Repositories;
using Identity.Domain.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Identity.API.Controllers.V1;

/// <summary>
/// Session management endpoints for users to view and manage their active sessions
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/sessions")]
[Authorize]
[Produces("application/json")]
public class SessionManagementController : ControllerBase
{
    private readonly ISessionManagementService _sessionManagementService;
    private readonly IUserSessionRepository _sessionRepository;
    private readonly ILogger<SessionManagementController> _logger;

    public SessionManagementController(
        ISessionManagementService sessionManagementService,
        IUserSessionRepository sessionRepository,
        ILogger<SessionManagementController> logger)
    {
        _sessionManagementService = sessionManagementService;
        _sessionRepository = sessionRepository;
        _logger = logger;
    }

    /// <summary>
    /// Get current user's session limit information
    /// </summary>
    /// <returns>Session limit information including active sessions</returns>
    [HttpGet("limits")]
    [ProducesResponseType(typeof(SessionLimitInfoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetSessionLimits()
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty)
        {
            return Unauthorized(new ProblemDetails
            {
                Title = "Authentication Required",
                Detail = "User ID not found in token",
                Status = StatusCodes.Status401Unauthorized
            });
        }

        var maxAllowed = await _sessionManagementService.GetMaxAllowedSessionsAsync(userId);
        var currentActive = await _sessionManagementService.GetActiveSessionCountAsync(userId);
        var canCreate = await _sessionManagementService.CanCreateSessionAsync(userId);
        var activeSessions = await _sessionManagementService.GetActiveSessionsAsync(userId);

        var currentSessionId = HttpContext.TraceIdentifier; // You might want to get this from the token claims

        var sessionDtos = activeSessions.Select(s => new UserSessionDto
        {
            Id = s.Id,
            DeviceInfo = s.DeviceInfo,
            IpAddress = s.IpAddress,
            CreatedAt = s.CreatedAt,
            ExpiresAt = s.ExpiresAt,
            EndedAt = s.EndedAt,
            IsActive = s.IsActive,
            IsCurrentSession = false // TODO: Implement current session detection
        }).ToList();

        var result = new SessionLimitInfoDto
        {
            MaxAllowedSessions = maxAllowed,
            CurrentActiveSessions = currentActive,
            CanCreateNewSession = canCreate,
            LimitBehavior = "RevokeOldest", // TODO: Get from configuration
            ActiveSessions = sessionDtos
        };

        return Ok(result);
    }

    /// <summary>
    /// Get current user's active sessions
    /// </summary>
    /// <returns>List of active sessions</returns>
    [HttpGet("active")]
    [ProducesResponseType(typeof(IEnumerable<UserSessionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetActiveSessions()
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty)
        {
            return Unauthorized(new ProblemDetails
            {
                Title = "Authentication Required",
                Detail = "User ID not found in token",
                Status = StatusCodes.Status401Unauthorized
            });
        }

        var activeSessions = await _sessionManagementService.GetActiveSessionsAsync(userId);

        var sessionDtos = activeSessions.Select(s => new UserSessionDto
        {
            Id = s.Id,
            DeviceInfo = s.DeviceInfo,
            IpAddress = s.IpAddress,
            CreatedAt = s.CreatedAt,
            ExpiresAt = s.ExpiresAt,
            EndedAt = s.EndedAt,
            IsActive = s.IsActive,
            IsCurrentSession = false // TODO: Implement current session detection
        });

        return Ok(sessionDtos);
    }

    /// <summary>
    /// End a specific session
    /// </summary>
    /// <param name="sessionId">Session ID to end</param>
    /// <returns>Success confirmation</returns>
    [HttpDelete("{sessionId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> EndSession(Guid sessionId)
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty)
        {
            return Unauthorized(new ProblemDetails
            {
                Title = "Authentication Required",
                Detail = "User ID not found in token",
                Status = StatusCodes.Status401Unauthorized
            });
        }

        var session = await _sessionRepository.GetByIdAsync(sessionId);
        if (session == null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Session Not Found",
                Detail = $"Session with ID {sessionId} not found",
                Status = StatusCodes.Status404NotFound
            });
        }

        // Verify the session belongs to the current user
        if (session.UserId != userId)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Access Denied",
                Detail = "You can only end your own sessions",
                Status = StatusCodes.Status400BadRequest
            });
        }

        if (!session.IsActive)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Session Already Ended",
                Detail = "This session has already been ended",
                Status = StatusCodes.Status400BadRequest
            });
        }

        session.End();
        await _sessionRepository.UpdateAsync(session);

        _logger.LogInformation("User {UserId} ended session {SessionId}", userId, sessionId);

        return Ok(new { message = "Session ended successfully" });
    }

    /// <summary>
    /// End all other sessions (keep current session active)
    /// </summary>
    /// <returns>Number of sessions ended</returns>
    [HttpDelete("others")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> EndOtherSessions()
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty)
        {
            return Unauthorized(new ProblemDetails
            {
                Title = "Authentication Required",
                Detail = "User ID not found in token",
                Status = StatusCodes.Status401Unauthorized
            });
        }

        var activeSessions = await _sessionManagementService.GetActiveSessionsAsync(userId);
        var currentSessionToken = GetCurrentSessionToken();
        
        var sessionsToEnd = activeSessions.Where(s => s.RefreshToken != currentSessionToken).ToList();

        foreach (var session in sessionsToEnd)
        {
            session.End();
            await _sessionRepository.UpdateAsync(session);
        }

        _logger.LogInformation("User {UserId} ended {Count} other sessions", userId, sessionsToEnd.Count);

        return Ok(new { message = $"Ended {sessionsToEnd.Count} other sessions" });
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
        if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
        {
            return userId;
        }
        return Guid.Empty;
    }

    private string? GetCurrentSessionToken()
    {
        // This would need to be implemented based on how you store session info in the token
        // For now, returning null as a placeholder
        return null;
    }
}
