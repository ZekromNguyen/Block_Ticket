using Identity.Application.Common.Interfaces;
using Identity.Application.Common.Models;
using Identity.Domain.Entities;
using Identity.Domain.Repositories;
using Identity.Domain.Services;
using Microsoft.Extensions.Logging;

namespace Identity.Application.Features.Authentication.Commands;

public record LogoutCommand(
    Guid UserId,
    string? SessionId = null,
    string? IpAddress = null,
    string? UserAgent = null) : ICommand<Result>;

public class LogoutCommandHandler : ICommandHandler<LogoutCommand, Result>
{
    private readonly IUserSessionRepository _sessionRepository;
    private readonly ITokenService _tokenService;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly ILogger<LogoutCommandHandler> _logger;

    public LogoutCommandHandler(
        IUserSessionRepository sessionRepository,
        ITokenService tokenService,
        IAuditLogRepository auditLogRepository,
        ILogger<LogoutCommandHandler> logger)
    {
        _sessionRepository = sessionRepository;
        _tokenService = tokenService;
        _auditLogRepository = auditLogRepository;
        _logger = logger;
    }

    public async Task<Result> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        try
        {
            if (!string.IsNullOrEmpty(request.SessionId))
            {
                // Logout specific session
                var sessionGuid = Guid.Parse(request.SessionId);
                var session = await _sessionRepository.GetByIdAsync(sessionGuid, cancellationToken);
                
                if (session != null && session.UserId == request.UserId)
                {
                    // End the session
                    session.End();
                    await _sessionRepository.UpdateAsync(session, cancellationToken);

                    // Revoke all tokens for this session
                    await _tokenService.RevokeSessionTokensAsync(request.SessionId);

                    _logger.LogInformation("Session {SessionId} ended for user {UserId}", request.SessionId, request.UserId);
                }
                else
                {
                    _logger.LogWarning("Session {SessionId} not found or doesn't belong to user {UserId}", request.SessionId, request.UserId);
                    return Result.Failure("Session not found");
                }
            }
            else
            {
                // Logout all sessions for the user
                var sessions = await _sessionRepository.GetActiveSessionsByUserIdAsync(request.UserId, cancellationToken);
                
                foreach (var session in sessions)
                {
                    session.End();
                    await _sessionRepository.UpdateAsync(session, cancellationToken);
                    
                    // Revoke all tokens for this session
                    if (!string.IsNullOrEmpty(session.Id.ToString()))
                    {
                        await _tokenService.RevokeSessionTokensAsync(session.Id.ToString());
                    }
                }

                // Also revoke all user tokens (in case some don't have session IDs)
                await _tokenService.RevokeAllUserTokensAsync(request.UserId);

                _logger.LogInformation("All sessions ended for user {UserId}", request.UserId);
            }

            // Create audit log
            var auditLog = AuditLog.CreateTokenEvent(
                string.IsNullOrEmpty(request.SessionId) ? "LOGOUT_ALL_SESSIONS" : "LOGOUT_SESSION",
                null, // clientId
                request.UserId,
                request.IpAddress ?? "Unknown",
                request.UserAgent ?? "Unknown",
                true,
                null, // errorMessage
                request.SessionId
            );
            await _auditLogRepository.AddAsync(auditLog, cancellationToken);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout for user {UserId}, session {SessionId}", request.UserId, request.SessionId);
            return Result.Failure("An error occurred during logout");
        }
    }
}

public record LogoutAllSessionsCommand(
    Guid UserId,
    string? IpAddress = null,
    string? UserAgent = null) : ICommand<Result>;

public class LogoutAllSessionsCommandHandler : ICommandHandler<LogoutAllSessionsCommand, Result>
{
    private readonly IUserSessionRepository _sessionRepository;
    private readonly ITokenService _tokenService;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly ILogger<LogoutAllSessionsCommandHandler> _logger;

    public LogoutAllSessionsCommandHandler(
        IUserSessionRepository sessionRepository,
        ITokenService tokenService,
        IAuditLogRepository auditLogRepository,
        ILogger<LogoutAllSessionsCommandHandler> logger)
    {
        _sessionRepository = sessionRepository;
        _tokenService = tokenService;
        _auditLogRepository = auditLogRepository;
        _logger = logger;
    }

    public async Task<Result> Handle(LogoutAllSessionsCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Logout all sessions for the user
            var sessions = await _sessionRepository.GetActiveSessionsByUserIdAsync(request.UserId, cancellationToken);

            foreach (var session in sessions)
            {
                session.End();
                await _sessionRepository.UpdateAsync(session, cancellationToken);

                // Revoke all tokens for this session
                if (!string.IsNullOrEmpty(session.Id.ToString()))
                {
                    await _tokenService.RevokeSessionTokensAsync(session.Id.ToString());
                }
            }

            // Also revoke all user tokens (in case some don't have session IDs)
            await _tokenService.RevokeAllUserTokensAsync(request.UserId);

            _logger.LogInformation("All sessions ended for user {UserId}", request.UserId);

            // Create audit log
            var auditLog = AuditLog.CreateTokenEvent(
                "LOGOUT_ALL_SESSIONS",
                null, // clientId
                request.UserId,
                request.IpAddress ?? "Unknown",
                request.UserAgent ?? "Unknown",
                true,
                null, // errorMessage
                null // sessionId
            );
            await _auditLogRepository.AddAsync(auditLog, cancellationToken);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout all sessions for user {UserId}", request.UserId);
            return Result.Failure("An error occurred during logout");
        }
    }
}
