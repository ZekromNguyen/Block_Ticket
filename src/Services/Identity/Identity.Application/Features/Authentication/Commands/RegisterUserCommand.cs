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
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly UserDomainService _userDomainService;
    // private readonly IPublishEndpoint _publishEndpoint; // TODO: Re-enable after MassTransit setup
    private readonly ILogger<RegisterUserCommandHandler> _logger;

    public RegisterUserCommandHandler(
        IUserRepository userRepository,
        IAuditLogRepository auditLogRepository,
        UserDomainService userDomainService,
        // IPublishEndpoint publishEndpoint, // TODO: Re-enable after MassTransit setup
        ILogger<RegisterUserCommandHandler> logger)
    {
        _userRepository = userRepository;
        _auditLogRepository = auditLogRepository;
        _userDomainService = userDomainService;
        // _publishEndpoint = publishEndpoint; // TODO: Re-enable after MassTransit setup
        _logger = logger;
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

            // Save user
            await _userRepository.AddAsync(user, cancellationToken);

            // Create audit log
            var auditLog = AuditLog.CreateRegistration(
                user.Id,
                user.Email.Value,
                user.UserType.ToString(),
                request.IpAddress ?? "Unknown",
                request.UserAgent ?? "Unknown");

            await _auditLogRepository.AddAsync(auditLog, cancellationToken);

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
}
