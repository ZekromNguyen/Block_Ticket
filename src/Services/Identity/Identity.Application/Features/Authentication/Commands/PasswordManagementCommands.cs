using Identity.Application.Common.Configuration;
using Identity.Application.Common.Interfaces;
using Identity.Application.Common.Models;
using Identity.Domain.Entities;
using Identity.Domain.Repositories;
using Identity.Domain.Services;
using Identity.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Identity.Application.Features.Authentication.Commands;

public record ForgotPasswordCommand(
    string Email,
    string? IpAddress = null,
    string? UserAgent = null) : ICommand<Result>;

public class ForgotPasswordCommandHandler : ICommandHandler<ForgotPasswordCommand, Result>
{
    private readonly IUserRepository _userRepository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly ITokenService _tokenService;
    private readonly IEmailService _emailService;
    private readonly ILogger<ForgotPasswordCommandHandler> _logger;
    private readonly ApplicationSettings _applicationSettings;

    public ForgotPasswordCommandHandler(
        IUserRepository userRepository,
        IAuditLogRepository auditLogRepository,
        ITokenService tokenService,
        IEmailService emailService,
        ILogger<ForgotPasswordCommandHandler> logger,
        IOptions<ApplicationSettings> applicationSettings)
    {
        _userRepository = userRepository;
        _auditLogRepository = auditLogRepository;
        _tokenService = tokenService;
        _emailService = emailService;
        _logger = logger;
        _applicationSettings = applicationSettings.Value;
    }

    public async Task<Result> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var email = new Email(request.Email);
            var user = await _userRepository.GetByEmailAsync(email, cancellationToken);

            // Always return success to prevent email enumeration attacks
            if (user == null)
            {
                _logger.LogWarning("Password reset requested for non-existent email {Email}", request.Email);
                return Result.Success();
            }

            // Generate password reset token
            var resetToken = _tokenService.GeneratePasswordResetToken(user.Id);

            // Send password reset email
            var resetLink = $"{_applicationSettings.PasswordResetUrl}?token={resetToken}&email={email.Value}";

            _logger.LogInformation("🔗 Password reset URL: {ResetLink}", resetLink);

            await _emailService.SendPasswordResetAsync(email.Value, resetToken, resetLink);

            // Create audit log
            var auditLog = Domain.Entities.AuditLog.CreateAdminAction(
                user.Id,
                "PASSWORD_RESET_REQUESTED",
                "USER_SECURITY",
                request.IpAddress ?? "Unknown",
                request.UserAgent ?? "Unknown",
                $"{{\"email\":\"{email.Value}\"}}");

            await _auditLogRepository.AddAsync(auditLog, cancellationToken);

            _logger.LogInformation("Password reset email sent to {Email}", email.Value);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing forgot password request for {Email}", request.Email);
            return Result.Failure("An error occurred while processing your request");
        }
    }
}

public record ResetPasswordCommand(
    string Token,
    string Email,
    string NewPassword,
    string? IpAddress = null,
    string? UserAgent = null) : ICommand<Result>;

public class ResetPasswordCommandHandler : ICommandHandler<ResetPasswordCommand, Result>
{
    private readonly IUserRepository _userRepository;
    private readonly IUserSessionRepository _sessionRepository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly ITokenService _tokenService;
    private readonly IPasswordService _passwordService;
    private readonly IPasswordHistoryService _passwordHistoryService;
    private readonly ILogger<ResetPasswordCommandHandler> _logger;

    public ResetPasswordCommandHandler(
        IUserRepository userRepository,
        IUserSessionRepository sessionRepository,
        IAuditLogRepository auditLogRepository,
        ITokenService tokenService,
        IPasswordService passwordService,
        IPasswordHistoryService passwordHistoryService,
        ILogger<ResetPasswordCommandHandler> logger)
    {
        _userRepository = userRepository;
        _sessionRepository = sessionRepository;
        _auditLogRepository = auditLogRepository;
        _tokenService = tokenService;
        _passwordService = passwordService;
        _passwordHistoryService = passwordHistoryService;
        _logger = logger;
    }

    public async Task<Result> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var email = new Email(request.Email);
            var user = await _userRepository.GetByEmailAsync(email, cancellationToken);

            if (user == null)
            {
                return Result.Failure("Invalid reset token");
            }

            // Validate reset token
            if (!_tokenService.ValidatePasswordResetToken(request.Token, user.Id))
            {
                var failedAuditLog = Domain.Entities.AuditLog.CreateAdminAction(
                    user.Id,
                    "PASSWORD_RESET_FAILED",
                    "USER_SECURITY",
                    request.IpAddress ?? "Unknown",
                    request.UserAgent ?? "Unknown",
                    errorMessage: "Invalid reset token");

                await _auditLogRepository.AddAsync(failedAuditLog, cancellationToken);

                return Result.Failure("Invalid or expired reset token");
            }

            // Validate new password strength
            if (!_passwordService.IsPasswordStrong(request.NewPassword))
            {
                return Result.Failure("Password must contain at least 8 characters, including uppercase, lowercase, digit, and special character");
            }

            // Hash new password
            var newPasswordHash = _passwordService.HashPassword(request.NewPassword);

            // Validate password history (check if password was used before)
            var isPasswordValid = await _passwordHistoryService.IsPasswordValidAsync(
                user.Id, 
                newPasswordHash, 
                cancellationToken);

            if (!isPasswordValid)
            {
                var historyFailedAuditLog = Domain.Entities.AuditLog.CreateAdminAction(
                    user.Id,
                    "PASSWORD_RESET_FAILED",
                    "USER_SECURITY",
                    request.IpAddress ?? "Unknown",
                    request.UserAgent ?? "Unknown",
                    errorMessage: "Password reuse attempted during reset");

                await _auditLogRepository.AddAsync(historyFailedAuditLog, cancellationToken);
                return Result.Failure("Password cannot be reused. Please choose a password that hasn't been used recently.");
            }

            // Change password with history tracking
            try
            {
                await _passwordHistoryService.ChangePasswordWithHistoryAsync(
                    user.Id, 
                    newPasswordHash, 
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to reset password with history for user {UserId}", user.Id);
                return Result.Failure("An error occurred while resetting your password");
            }

            // End all sessions for security
            user.EndAllSessions();
            await _sessionRepository.EndAllUserSessionsAsync(user.Id, cancellationToken);

            await _userRepository.UpdateAsync(user, cancellationToken);

            // Create success audit log
            var successAuditLog = Domain.Entities.AuditLog.CreateAdminAction(
                user.Id,
                "PASSWORD_RESET_COMPLETED",
                "USER_SECURITY",
                request.IpAddress ?? "Unknown",
                request.UserAgent ?? "Unknown");

            await _auditLogRepository.AddAsync(successAuditLog, cancellationToken);

            _logger.LogInformation("Password reset completed for user {UserId}", user.Id);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting password for email {Email}", request.Email);
            return Result.Failure("An error occurred while resetting your password");
        }
    }
}

public record ConfirmEmailCommand(
    string Token,
    string Email,
    string? IpAddress = null,
    string? UserAgent = null) : ICommand<Result>;

public class ConfirmEmailCommandHandler : ICommandHandler<ConfirmEmailCommand, Result>
{
    private readonly IUserRepository _userRepository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly ITokenService _tokenService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ConfirmEmailCommandHandler> _logger;

    public ConfirmEmailCommandHandler(
        IUserRepository userRepository,
        IAuditLogRepository auditLogRepository,
        ITokenService tokenService,
        IUnitOfWork unitOfWork,
        ILogger<ConfirmEmailCommandHandler> logger)
    {
        _userRepository = userRepository;
        _auditLogRepository = auditLogRepository;
        _tokenService = tokenService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(ConfirmEmailCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var email = new Email(request.Email);
            var user = await _userRepository.GetByEmailAsync(email, cancellationToken);

            if (user == null)
            {
                return Result.Failure("Invalid confirmation token");
            }

            if (user.EmailConfirmed)
            {
                return Result.Failure("Email is already confirmed");
            }

            // Validate confirmation token
            if (!_tokenService.ValidateEmailConfirmationToken(request.Token, user.Id))
            {
                var failedAuditLog = Domain.Entities.AuditLog.CreateAdminAction(
                    user.Id,
                    "EMAIL_CONFIRMATION_FAILED",
                    "USER_MANAGEMENT",
                    request.IpAddress ?? "Unknown",
                    request.UserAgent ?? "Unknown",
                    errorMessage: "Invalid confirmation token");

                await _auditLogRepository.AddAsync(failedAuditLog, cancellationToken);

                return Result.Failure("Invalid or expired confirmation token");
            }

            // Confirm email
            user.ConfirmEmail();
            await _userRepository.UpdateAsync(user, cancellationToken);

            // Create success audit log
            var successAuditLog = Domain.Entities.AuditLog.CreateAdminAction(
                user.Id,
                "EMAIL_CONFIRMED",
                "USER_MANAGEMENT",
                request.IpAddress ?? "Unknown",
                request.UserAgent ?? "Unknown");

            await _auditLogRepository.AddAsync(successAuditLog, cancellationToken);

            // Save all changes to database
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Email confirmed for user {UserId}", user.Id);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error confirming email for {Email}", request.Email);
            return Result.Failure("An error occurred while confirming your email");
        }
    }
}

public record ChangePasswordCommand(
    Guid UserId,
    string CurrentPassword,
    string NewPassword,
    string? IpAddress = null,
    string? UserAgent = null) : ICommand<Result>;

public class ChangePasswordCommandHandler : ICommandHandler<ChangePasswordCommand, Result>
{
    private readonly IUserRepository _userRepository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IUserSessionRepository _sessionRepository;
    private readonly IPasswordService _passwordService;
    private readonly IPasswordHistoryService _passwordHistoryService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ChangePasswordCommandHandler> _logger;

    public ChangePasswordCommandHandler(
        IUserRepository userRepository,
        IAuditLogRepository auditLogRepository,
        IUserSessionRepository sessionRepository,
        IPasswordService passwordService,
        IPasswordHistoryService passwordHistoryService,
        IUnitOfWork unitOfWork,
        ILogger<ChangePasswordCommandHandler> logger)
    {
        _userRepository = userRepository;
        _auditLogRepository = auditLogRepository;
        _sessionRepository = sessionRepository;
        _passwordService = passwordService;
        _passwordHistoryService = passwordHistoryService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
            if (user == null)
            {
                _logger.LogWarning("Password change attempted for non-existent user {UserId}", request.UserId);
                return Result.Failure("User not found");
            }

            // Verify current password
            if (!_passwordService.VerifyPassword(request.CurrentPassword, user.PasswordHash))
            {
                _logger.LogWarning("Password change failed: Invalid current password for user {UserId}", request.UserId);

                // Create failed audit log
                var failedAuditLog = Domain.Entities.AuditLog.CreateAdminAction(
                    user.Id,
                    "PASSWORD_CHANGE_FAILED",
                    "USER_SECURITY",
                    request.IpAddress ?? "Unknown",
                    request.UserAgent ?? "Unknown",
                    errorMessage: "Invalid current password");

                await _auditLogRepository.AddAsync(failedAuditLog, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                return Result.Failure("Current password is incorrect");
            }

            // Validate new password strength
            if (!_passwordService.IsPasswordStrong(request.NewPassword))
            {
                _logger.LogWarning("Password change failed: Weak password for user {UserId}", request.UserId);
                return Result.Failure("Password must contain at least 8 characters, including uppercase, lowercase, digit, and special character");
            }

            // Check if new password is different from current password
            if (_passwordService.VerifyPassword(request.NewPassword, user.PasswordHash))
            {
                _logger.LogWarning("Password change failed: New password same as current for user {UserId}", request.UserId);
                return Result.Failure("New password must be different from current password");
            }

            // Hash new password
            var newPasswordHash = _passwordService.HashPassword(request.NewPassword);

            // Validate password history (check if password was used before)
            var isPasswordValid = await _passwordHistoryService.IsPasswordValidAsync(
                user.Id, 
                newPasswordHash, 
                cancellationToken);

            if (!isPasswordValid)
            {
                _logger.LogWarning("Password change failed: Password in history for user {UserId}", request.UserId);

                // Create failed audit log for password reuse
                var historyFailedAuditLog = Domain.Entities.AuditLog.CreateAdminAction(
                    user.Id,
                    "PASSWORD_CHANGE_FAILED",
                    "USER_SECURITY",
                    request.IpAddress ?? "Unknown",
                    request.UserAgent ?? "Unknown",
                    errorMessage: "Password reuse attempted");

                await _auditLogRepository.AddAsync(historyFailedAuditLog, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                return Result.Failure("Password cannot be reused. Please choose a password that hasn't been used recently.");
            }

            // Change password with history tracking
            try
            {
                await _passwordHistoryService.ChangePasswordWithHistoryAsync(
                    user.Id, 
                    newPasswordHash, 
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to change password with history for user {UserId}", request.UserId);
                return Result.Failure("An error occurred while changing your password");
            }

            // End all other sessions for security (except current session)
            await _sessionRepository.EndAllUserSessionsAsync(user.Id, cancellationToken);

            await _userRepository.UpdateAsync(user, cancellationToken);

            // Create success audit log
            var successAuditLog = Domain.Entities.AuditLog.CreateAdminAction(
                user.Id,
                "PASSWORD_CHANGED",
                "USER_SECURITY",
                request.IpAddress ?? "Unknown",
                request.UserAgent ?? "Unknown");

            await _auditLogRepository.AddAsync(successAuditLog, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Password changed successfully for user {UserId}", request.UserId);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing password for user {UserId}", request.UserId);
            return Result.Failure("An error occurred while changing your password");
        }
    }
}
