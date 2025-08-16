using Identity.Application.Common.Configuration;
using Identity.Application.Common.Interfaces;
using Identity.Application.Common.Models;
using Identity.Application.DTOs;
using Identity.Domain.Entities;
using Identity.Domain.Events;
using Identity.Domain.Exceptions;
using Identity.Domain.Repositories;
using Identity.Domain.Services;
using Identity.Domain.ValueObjects;
using MassTransit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.Contracts.Events;

namespace Identity.Application.Features.Authentication.Commands;

public record RegisterUserCommand(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    string UserType,
    string? WalletAddress = null,
    string? IpAddress = null,
    string? UserAgent = null) : ICommand<Result<UserDto>>;

public class RegisterUserCommandHandler : ICommandHandler<RegisterUserCommand, Result<UserDto>>
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly UserDomainService _userDomainService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailService _emailService;
    private readonly ITokenService _tokenService;
    // private readonly IPublishEndpoint _publishEndpoint; // TODO: Re-enable after MassTransit setup
    private readonly ILogger<RegisterUserCommandHandler> _logger;
    private readonly ApplicationSettings _applicationSettings;

    public RegisterUserCommandHandler(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IAuditLogRepository auditLogRepository,
        UserDomainService userDomainService,
        IUnitOfWork unitOfWork,
        IEmailService emailService,
        ITokenService tokenService,
        // IPublishEndpoint publishEndpoint, // TODO: Re-enable after MassTransit setup
        ILogger<RegisterUserCommandHandler> logger,
        IOptions<ApplicationSettings> applicationSettings)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _auditLogRepository = auditLogRepository;
        _userDomainService = userDomainService;
        _unitOfWork = unitOfWork;
        _emailService = emailService;
        _tokenService = tokenService;
        // _publishEndpoint = publishEndpoint; // TODO: Re-enable after MassTransit setup
        _logger = logger;
        _applicationSettings = applicationSettings.Value;
    }

    public async Task<Result<UserDto>> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Parse user type
            if (!Enum.TryParse<UserType>(request.UserType, true, out var userType))
            {
                return Result<UserDto>.Failure("Invalid user type");
            }

            // Create value objects
            var email = new Email(request.Email);
            var walletAddress = !string.IsNullOrEmpty(request.WalletAddress) 
                ? new WalletAddress(request.WalletAddress) 
                : null;

            // Register user through domain service
            var user = await _userDomainService.RegisterUserAsync(
                email, 
                request.Password, 
                request.FirstName, 
                request.LastName, 
                userType, 
                walletAddress, 
                cancellationToken);

            // Assign default role based on user type BEFORE adding to repository
            await AssignDefaultRoleAsync(user, userType, cancellationToken);

            // Set the final UpdatedAt timestamp after all modifications
            user.UpdatedAt = DateTime.UtcNow;

            // Save user (after all modifications are complete)
            await _userRepository.AddAsync(user, cancellationToken);

            // Create audit log
            var auditLog = AuditLog.CreateRegistration(
                user.Id,
                user.Email.Value,
                user.UserType.ToString(),
                request.IpAddress ?? "Unknown",
                request.UserAgent ?? "Unknown");

            await _auditLogRepository.AddAsync(auditLog, cancellationToken);

            // Save all changes in a single transaction
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Send email confirmation
            try
            {
                var confirmationToken = _tokenService.GenerateEmailConfirmationToken(user.Id);
                var confirmationUrl = _applicationSettings.EmailConfirmationUrl;

                _logger.LogInformation("🔗 Email confirmation URL: {ConfirmationUrl}?token={Token}",
                    confirmationUrl, confirmationToken);

                await _emailService.SendEmailConfirmationAsync(user.Email.Value, confirmationToken, confirmationUrl);
                _logger.LogInformation("Email confirmation sent to {Email}", user.Email.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email confirmation to {Email}", user.Email.Value);
                // Don't fail the registration if email sending fails
            }

            // Publish domain events (temporarily disabled)
            // TODO: Re-enable after MassTransit setup
            // foreach (var domainEvent in user.DomainEvents)
            // {
            //     if (domainEvent is UserRegisteredDomainEvent userRegisteredEvent)
            //     {
            //         await _publishEndpoint.Publish(new UserRegistered(
            //             userRegisteredEvent.UserId,
            //             userRegisteredEvent.Email,
            //             userRegisteredEvent.UserType,
            //             DateTime.UtcNow), cancellationToken);
            //     }
            // }

            user.ClearDomainEvents();

            _logger.LogInformation("User {Email} registered successfully with ID {UserId}", 
                user.Email.Value, user.Id);

            // Map to DTO
            var userDto = new UserDto
            {
                Id = user.Id,
                Email = user.Email.Value,
                FirstName = user.FirstName,
                LastName = user.LastName,
                WalletAddress = user.WalletAddress?.Value,
                UserType = user.UserType.ToString(),
                Status = user.Status.ToString(),
                EmailConfirmed = user.EmailConfirmed,
                MfaEnabled = user.MfaEnabled,
                LastLoginAt = user.LastLoginAt,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt
            };

            return Result<UserDto>.Success(userDto);
        }
        catch (UserAlreadyExistsException ex)
        {
            _logger.LogWarning("Registration failed: {Error}", ex.Message);
            return Result<UserDto>.Failure(ex.Message);
        }
        catch (WeakPasswordException ex)
        {
            _logger.LogWarning("Registration failed: {Error}", ex.Message);
            return Result<UserDto>.Failure(ex.Message);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Registration failed: {Error}", ex.Message);
            return Result<UserDto>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during user registration");
            return Result<UserDto>.Failure("An unexpected error occurred during registration");
        }
    }

    private async Task AssignDefaultRoleAsync(User user, UserType userType, CancellationToken cancellationToken)
    {
        try
        {
            // Map UserType to role name
            string roleName = userType switch
            {
                UserType.Fan => "Fan",
                UserType.Promoter => "Promoter",
                UserType.Admin => "Admin",
                _ => "fan" // Default to fan role
            };

            // Find the role by name
            var role = await _roleRepository.GetByNameAsync(roleName, cancellationToken);
            if (role != null)
            {
                // Assign the role to the user without updating timestamp to avoid concurrency issues
                user.AssignRole(role.Id, "system", null, updateTimestamp: false);
                // Note: No need to call UpdateAsync here - the UnitOfWork will handle all changes

                _logger.LogInformation("Assigned role '{RoleName}' to user {UserId} during registration",
                    roleName, user.Id);
            }
            else
            {
                _logger.LogWarning("Default role '{RoleName}' not found for user type {UserType}. User {UserId} registered without role.",
                    roleName, userType, user.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to assign default role to user {UserId} during registration", user.Id);
            // Don't throw - registration should still succeed even if role assignment fails
        }
    }
}
