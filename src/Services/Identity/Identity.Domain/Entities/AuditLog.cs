using Shared.Common.Models;

namespace Identity.Domain.Entities;

public class AuditLog : BaseEntity
{
    public Guid? UserId { get; private set; }
    public string Action { get; private set; } = string.Empty;
    public string Resource { get; private set; } = string.Empty;
    public AuditLogLevel Level { get; private set; }
    public string IpAddress { get; private set; } = string.Empty;
    public string UserAgent { get; private set; } = string.Empty;
    public string? AdditionalData { get; private set; } // JSON
    public bool Success { get; private set; }
    public string? ErrorMessage { get; private set; }
    public string? SessionId { get; private set; }
    public string? ClientId { get; private set; }

    private AuditLog() { } // For EF Core

    public AuditLog(
        string action,
        string resource,
        AuditLogLevel level,
        string ipAddress,
        string userAgent,
        bool success = true,
        Guid? userId = null,
        string? additionalData = null,
        string? errorMessage = null,
        string? sessionId = null,
        string? clientId = null)
    {
        Action = action;
        Resource = resource;
        Level = level;
        IpAddress = ipAddress;
        UserAgent = userAgent;
        Success = success;
        UserId = userId;
        AdditionalData = additionalData;
        ErrorMessage = errorMessage;
        SessionId = sessionId;
        ClientId = clientId;
    }

    public static AuditLog CreateLoginAttempt(
        Guid userId,
        string email,
        string ipAddress,
        string userAgent,
        bool success,
        string? errorMessage = null)
    {
        return new AuditLog(
            "LOGIN_ATTEMPT",
            "USER_AUTHENTICATION",
            success ? AuditLogLevel.Information : AuditLogLevel.Warning,
            ipAddress,
            userAgent,
            success,
            userId,
            $"{{\"email\":\"{email}\"}}",
            errorMessage);
    }

    public static AuditLog CreateRegistration(
        Guid userId,
        string email,
        string userType,
        string ipAddress,
        string userAgent)
    {
        return new AuditLog(
            "USER_REGISTRATION",
            "USER_MANAGEMENT",
            AuditLogLevel.Information,
            ipAddress,
            userAgent,
            true,
            userId,
            $"{{\"email\":\"{email}\",\"userType\":\"{userType}\"}}");
    }

    public static AuditLog CreatePasswordChange(
        Guid userId,
        string ipAddress,
        string userAgent,
        bool success,
        string? errorMessage = null)
    {
        return new AuditLog(
            "PASSWORD_CHANGE",
            "USER_SECURITY",
            AuditLogLevel.Information,
            ipAddress,
            userAgent,
            success,
            userId,
            errorMessage: errorMessage);
    }

    public static AuditLog CreateMfaEvent(
        Guid userId,
        string mfaAction,
        string deviceType,
        string ipAddress,
        string userAgent,
        bool success,
        string? errorMessage = null)
    {
        return new AuditLog(
            $"MFA_{mfaAction.ToUpper()}",
            "USER_SECURITY",
            success ? AuditLogLevel.Information : AuditLogLevel.Warning,
            ipAddress,
            userAgent,
            success,
            userId,
            $"{{\"deviceType\":\"{deviceType}\"}}",
            errorMessage);
    }

    public static AuditLog CreateTokenEvent(
        string action,
        string? clientId,
        Guid? userId,
        string ipAddress,
        string userAgent,
        bool success,
        string? errorMessage = null,
        string? sessionId = null)
    {
        return new AuditLog(
            $"TOKEN_{action.ToUpper()}",
            "TOKEN_MANAGEMENT",
            success ? AuditLogLevel.Information : AuditLogLevel.Warning,
            ipAddress,
            userAgent,
            success,
            userId,
            clientId != null ? $"{{\"clientId\":\"{clientId}\"}}" : null,
            errorMessage,
            sessionId,
            clientId);
    }

    public static AuditLog CreateAdminAction(
        Guid adminUserId,
        string action,
        string resource,
        string ipAddress,
        string userAgent,
        string? targetData = null,
        bool success = true,
        string? errorMessage = null)
    {
        return new AuditLog(
            $"ADMIN_{action.ToUpper()}",
            resource,
            AuditLogLevel.Information,
            ipAddress,
            userAgent,
            success,
            adminUserId,
            targetData,
            errorMessage);
    }
}

public enum AuditLogLevel
{
    Information = 0,
    Warning = 1,
    Error = 2,
    Critical = 3
}
