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
        // TODO: Implement RefreshTokenCommand
        _logger.LogInformation("RefreshTokenAsync called");
        return Result<LoginResultDto>.Failure("Not implemented yet");
    }

    public async Task<Result> LogoutAsync(Guid userId, string? sessionId = null)
    {
        // TODO: Implement LogoutCommand
        _logger.LogInformation("LogoutAsync called for user {UserId}", userId);
        return Result.Failure("Not implemented yet");
    }

    public async Task<Result> LogoutAllSessionsAsync(Guid userId)
    {
        // TODO: Implement LogoutAllSessionsCommand
        _logger.LogInformation("LogoutAllSessionsAsync called for user {UserId}", userId);
        return Result.Failure("Not implemented yet");
    }

    public async Task<Result> ConfirmEmailAsync(string token, string email)
    {
        var command = new ConfirmEmailCommand(token, email);
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
        // TODO: Implement ChangePasswordCommand
        _logger.LogInformation("ChangePasswordAsync called for user {UserId}", userId);
        return Result.Failure("Not implemented yet");
    }
}
