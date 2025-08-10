using Identity.Application.Common.Models;
using Identity.Application.DTOs;

namespace Identity.Application.Services;

public interface IAuthenticationService
{
    Task<Result<UserDto>> RegisterAsync(CreateUserDto createUserDto, string? ipAddress = null, string? userAgent = null);
    Task<Result<LoginResultDto>> LoginAsync(LoginDto loginDto, string? ipAddress = null, string? userAgent = null);
    Task<Result<LoginResultDto>> RefreshTokenAsync(RefreshTokenDto refreshTokenDto, string? ipAddress = null, string? userAgent = null);
    Task<Result> LogoutAsync(Guid userId, string? sessionId = null);
    Task<Result> LogoutAllSessionsAsync(Guid userId);
    Task<Result> ConfirmEmailAsync(string token, string email);
    Task<Result> ResendEmailConfirmationAsync(string email);
    Task<Result> ForgotPasswordAsync(ForgotPasswordDto forgotPasswordDto, string? ipAddress = null, string? userAgent = null);
    Task<Result> ResetPasswordAsync(ResetPasswordDto resetPasswordDto, string? ipAddress = null, string? userAgent = null);
    Task<Result> ChangePasswordAsync(Guid userId, ChangePasswordDto changePasswordDto, string? ipAddress = null, string? userAgent = null);
}
