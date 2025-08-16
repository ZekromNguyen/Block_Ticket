using Identity.Domain.Events;
using Identity.Domain.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Identity.Application.Handlers.Notifications;

/// <summary>
/// Handler for user authentication events that triggers security notifications
/// </summary>
public class SecurityEventNotificationHandler : 
    INotificationHandler<UserLoggedInDomainEvent>,
    INotificationHandler<UserLoginFailedDomainEvent>,
    INotificationHandler<UserAccountLockedDomainEvent>,
    INotificationHandler<UserAccountUnlockedDomainEvent>,
    INotificationHandler<UserPasswordChangedDomainEvent>,
    INotificationHandler<UserMfaEnabledDomainEvent>,
    INotificationHandler<UserMfaDisabledDomainEvent>
{
    private readonly ISecurityNotificationService _notificationService;
    private readonly ISecurityService _securityService;
    private readonly ILogger<SecurityEventNotificationHandler> _logger;

    public SecurityEventNotificationHandler(
        ISecurityNotificationService notificationService,
        ISecurityService securityService,
        ILogger<SecurityEventNotificationHandler> logger)
    {
        _notificationService = notificationService;
        _securityService = securityService;
        _logger = logger;
    }

    public async Task Handle(UserLoggedInDomainEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            // Get the security event for this login to send notification
            var securityEvents = await _securityService.GetSecurityEventsAsync(
                notification.UserId, 
                notification.LoginAt.AddMinutes(-1), 
                notification.LoginAt.AddMinutes(1), 
                cancellationToken);

            var loginEvent = securityEvents.FirstOrDefault(e => 
                e.EventType == "LOGIN_SUCCESS" && 
                e.UserId == notification.UserId);

            if (loginEvent != null)
            {
                await _notificationService.SendSecurityEventNotificationAsync(loginEvent, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending notification for user login event {UserId}", notification.UserId);
        }
    }

    public async Task Handle(UserLoginFailedDomainEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            // Login failures are more critical, so we always want to track these
            var now = DateTime.UtcNow;
            var securityEvents = await _securityService.GetSecurityEventsAsync(
                notification.UserId, 
                now.AddMinutes(-1), 
                now.AddMinutes(1), 
                cancellationToken);

            var failedLoginEvent = securityEvents.FirstOrDefault(e => 
                e.EventType == "LOGIN_FAILURE" && 
                e.UserId == notification.UserId);

            if (failedLoginEvent != null)
            {
                await _notificationService.SendSecurityEventNotificationAsync(failedLoginEvent, cancellationToken);
            }

            // Also check for patterns of failed logins that might indicate brute force
            if (notification.FailedAttempts >= 3)
            {
                await _notificationService.SendCriticalSecurityAlertAsync(
                    $"Multiple failed login attempts detected for user {notification.Email}",
                    $"User has {notification.FailedAttempts} failed login attempts. This may indicate a brute force attack.",
                    cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending notification for failed login event {UserId}", notification.UserId);
        }
    }

    public async Task Handle(UserAccountLockedDomainEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            // Account lockouts are always critical
            await _notificationService.SendCriticalSecurityAlertAsync(
                $"Account locked for user {notification.Email}",
                $"Account will be locked until {notification.LockedUntil:yyyy-MM-dd HH:mm:ss UTC}",
                cancellationToken);

            // Also get the account lockout record and send detailed notification
            var accountLockout = await _securityService.GetAccountLockoutAsync(notification.UserId, cancellationToken);
            if (accountLockout != null)
            {
                await _notificationService.SendAccountLockoutNotificationAsync(accountLockout, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending notification for account lockout event {UserId}", notification.UserId);
        }
    }

    public async Task Handle(UserAccountUnlockedDomainEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            await _notificationService.SendCriticalSecurityAlertAsync(
                $"Account unlocked for user {notification.Email}",
                "Account has been manually unlocked and user can now log in again.",
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending notification for account unlock event {UserId}", notification.UserId);
        }
    }

    public async Task Handle(UserPasswordChangedDomainEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            // Password changes should be notified as they're security-sensitive
            var now = DateTime.UtcNow;
            var securityEvents = await _securityService.GetSecurityEventsAsync(
                notification.UserId, 
                now.AddMinutes(-5), 
                now.AddMinutes(1), 
                cancellationToken);

            var passwordChangeEvent = securityEvents.FirstOrDefault(e => 
                e.EventType == "PASSWORD_CHANGED" && 
                e.UserId == notification.UserId);

            if (passwordChangeEvent != null)
            {
                await _notificationService.SendSecurityEventNotificationAsync(passwordChangeEvent, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending notification for password change event {UserId}", notification.UserId);
        }
    }

    public async Task Handle(UserMfaEnabledDomainEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            await _notificationService.SendCriticalSecurityAlertAsync(
                $"MFA enabled for user {notification.Email}",
                "Multi-factor authentication has been enabled, improving account security.",
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending notification for MFA enabled event {UserId}", notification.UserId);
        }
    }

    public async Task Handle(UserMfaDisabledDomainEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            await _notificationService.SendCriticalSecurityAlertAsync(
                $"MFA disabled for user {notification.Email}",
                "Multi-factor authentication has been disabled. Account security may be reduced.",
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending notification for MFA disabled event {UserId}", notification.UserId);
        }
    }
}
