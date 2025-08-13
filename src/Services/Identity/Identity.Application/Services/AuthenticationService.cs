using Identity.Application.Common.Models;
using Identity.Application.DTOs;
using Identity.Application.Features.Authentication.Commands;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Identity.Application.Services;

public class AuthenticationService : IAuthenticationService
{
    private readonly IMediator _mediator;
    private readonly ILogger<AuthenticationService> _logger;

    public AuthenticationService(IMediator mediator, ILogger<AuthenticationService> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task<Result<UserDto>> RegisterAsync(CreateUserDto createUserDto, string? ipAddress = null, string? userAgent = null)
    {
        var command = new RegisterUserCommand(
            createUserDto.Email,
            createUserDto.Password,
            createUserDto.FirstName,
            createUserDto.LastName,
            createUserDto.UserType,
            createUserDto.WalletAddress,
            ipAddress,
            userAgent);

        return await _mediator.Send(command);
    }

    public async Task<Result<LoginResultDto>> LoginAsync(LoginDto loginDto, string? ipAddress = null, string? userAgent = null)
    {
        var command = new LoginCommand(
            loginDto.Email,
            loginDto.Password,
            loginDto.MfaCode,
            loginDto.DeviceInfo,
            ipAddress,
            userAgent,
            loginDto.RememberMe);

        return await _mediator.Send(command);
    }

    public async Task<Result<LoginResultDto>> RefreshTokenAsync(RefreshTokenDto refreshTokenDto, string? ipAddress = null, string? userAgent = null)
    {
        var command = new RefreshTokenCommand(
            refreshTokenDto.RefreshToken,
            ipAddress,
            userAgent);

        return await _mediator.Send(command);
    }

    public async Task<Result> LogoutAsync(Guid userId, string? sessionId = null)
    {
        var command = new LogoutCommand(userId, sessionId);
        return await _mediator.Send(command);
    }

    public async Task<Result> LogoutAllSessionsAsync(Guid userId)
    {
        var command = new LogoutAllSessionsCommand(userId);
        return await _mediator.Send(command);
    }

    public async Task<Result> ConfirmEmailAsync(string token, string email)
    {
        var command = new ConfirmEmailCommand(token, email);
        return await _mediator.Send(command);
    }

    public async Task<Result> ResendEmailConfirmationAsync(string email)
    {
        var command = new ResendEmailConfirmationCommand(email);
        return await _mediator.Send(command);
    }

    public async Task<Result> ForgotPasswordAsync(ForgotPasswordDto forgotPasswordDto, string? ipAddress = null, string? userAgent = null)
    {
        var command = new ForgotPasswordCommand(forgotPasswordDto.Email, ipAddress, userAgent);
        return await _mediator.Send(command);
    }

    public async Task<Result> ResetPasswordAsync(ResetPasswordDto resetPasswordDto, string? ipAddress = null, string? userAgent = null)
    {
        var command = new ResetPasswordCommand(
            resetPasswordDto.Token,
            resetPasswordDto.Email,
            resetPasswordDto.NewPassword,
            ipAddress,
            userAgent);
        return await _mediator.Send(command);
    }

    public async Task<Result> ChangePasswordAsync(Guid userId, ChangePasswordDto changePasswordDto, string? ipAddress = null, string? userAgent = null)
    {
        // Validate that new password and confirm password match
        if (changePasswordDto.NewPassword != changePasswordDto.ConfirmPassword)
        {
            _logger.LogWarning("Password change failed: Password confirmation mismatch for user {UserId}", userId);
            return Result.Failure("New password and confirmation password do not match");
        }

        var command = new ChangePasswordCommand(
            userId,
            changePasswordDto.CurrentPassword,
            changePasswordDto.NewPassword,
            ipAddress,
            userAgent);

        return await _mediator.Send(command);
    }
}
