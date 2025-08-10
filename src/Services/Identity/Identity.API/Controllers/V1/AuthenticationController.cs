using Identity.Application.DTOs;
using Identity.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;

namespace Identity.API.Controllers.V1;

/// <summary>
/// Authentication and user management endpoints
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/auth")]
[Produces("application/json")]
public class AuthenticationController : ControllerBase
{
    private readonly IAuthenticationService _authenticationService;
    private readonly ILogger<AuthenticationController> _logger;

    public AuthenticationController(
        IAuthenticationService authenticationService,
        ILogger<AuthenticationController> logger)
    {
        _authenticationService = authenticationService;
        _logger = logger;
    }

    /// <summary>
    /// Register a new user account
    /// </summary>
    /// <param name="createUserDto">User registration details</param>
    /// <returns>Created user information</returns>
    [HttpPost("register")]
    [EnableRateLimiting("AuthPolicy")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] CreateUserDto createUserDto)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = HttpContext.Request.Headers.UserAgent.ToString();

        var result = await _authenticationService.RegisterAsync(createUserDto, ipAddress, userAgent);

        if (result.IsSuccess)
        {
            _logger.LogInformation("User registered successfully: {Email}", createUserDto.Email);
            return CreatedAtAction(nameof(GetProfile), new { }, result.Value);
        }

        _logger.LogWarning("User registration failed: {Error}", result.Error);
        return BadRequest(new ProblemDetails
        {
            Title = "Registration Failed",
            Detail = result.Error,
            Status = StatusCodes.Status400BadRequest
        });
    }

    /// <summary>
    /// Authenticate user and obtain access tokens
    /// </summary>
    /// <param name="loginDto">Login credentials</param>
    /// <returns>Authentication tokens and user information</returns>
    [HttpPost("login")]
    [EnableRateLimiting("AuthPolicy")]
    [ProducesResponseType(typeof(LoginResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = HttpContext.Request.Headers.UserAgent.ToString();

        var result = await _authenticationService.LoginAsync(loginDto, ipAddress, userAgent);

        if (result.IsSuccess)
        {
            var loginResult = result.Value!;
            
            if (loginResult.RequiresMfa)
            {
                _logger.LogInformation("MFA required for user: {Email}", loginDto.Email);
                return Ok(loginResult);
            }

            _logger.LogInformation("User logged in successfully: {Email}", loginDto.Email);
            return Ok(loginResult);
        }

        _logger.LogWarning("Login failed for user: {Email}, Error: {Error}", loginDto.Email, result.Error);
        return Unauthorized(new ProblemDetails
        {
            Title = "Authentication Failed",
            Detail = result.Error,
            Status = StatusCodes.Status401Unauthorized
        });
    }

    /// <summary>
    /// Refresh access token using refresh token
    /// </summary>
    /// <param name="refreshTokenDto">Refresh token</param>
    /// <returns>New access token</returns>
    [HttpPost("refresh")]
    [EnableRateLimiting("AuthPolicy")]
    [ProducesResponseType(typeof(LoginResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenDto refreshTokenDto)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = HttpContext.Request.Headers.UserAgent.ToString();

        var result = await _authenticationService.RefreshTokenAsync(refreshTokenDto, ipAddress, userAgent);

        if (result.IsSuccess)
        {
            _logger.LogInformation("Token refreshed successfully");
            return Ok(result.Value);
        }

        _logger.LogWarning("Token refresh failed: {Error}", result.Error);
        return Unauthorized(new ProblemDetails
        {
            Title = "Token Refresh Failed",
            Detail = result.Error,
            Status = StatusCodes.Status401Unauthorized
        });
    }

    /// <summary>
    /// Logout user and invalidate tokens
    /// </summary>
    /// <returns>Success confirmation</returns>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var result = await _authenticationService.LogoutAsync(userId.Value);

        if (result.IsSuccess)
        {
            _logger.LogInformation("User logged out successfully: {UserId}", userId);
            return Ok(new { message = "Logged out successfully" });
        }

        _logger.LogWarning("Logout failed for user: {UserId}, Error: {Error}", userId, result.Error);
        return BadRequest(new ProblemDetails
        {
            Title = "Logout Failed",
            Detail = result.Error,
            Status = StatusCodes.Status400BadRequest
        });
    }

    /// <summary>
    /// Get current user profile
    /// </summary>
    /// <returns>Current user information</returns>
    [HttpGet("profile")]
    [Authorize]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProfile()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        // TODO: Implement GetUserByIdQuery through a user service
        _logger.LogInformation("Profile requested for user: {UserId}", userId);
        return Ok(new { message = "Profile endpoint not fully implemented yet" });
    }

    /// <summary>
    /// Confirm user email address
    /// </summary>
    /// <param name="token">Email confirmation token</param>
    /// <param name="email">Email address to confirm</param>
    /// <returns>Confirmation result</returns>
    [HttpPost("confirm-email")]
    [EnableRateLimiting("AuthPolicy")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ConfirmEmail([FromQuery] string token, [FromQuery] string email)
    {
        var result = await _authenticationService.ConfirmEmailAsync(token, email);

        if (result.IsSuccess)
        {
            _logger.LogInformation("Email confirmed successfully: {Email}", email);
            return Ok(new { message = "Email confirmed successfully" });
        }

        _logger.LogWarning("Email confirmation failed: {Email}, Error: {Error}", email, result.Error);
        return BadRequest(new ProblemDetails
        {
            Title = "Email Confirmation Failed",
            Detail = result.Error,
            Status = StatusCodes.Status400BadRequest
        });
    }

    /// <summary>
    /// Resend email confirmation
    /// </summary>
    /// <param name="email">Email address to resend confirmation to</param>
    /// <returns>Success confirmation</returns>
    [HttpPost("resend-email-confirmation")]
    [EnableRateLimiting("AuthPolicy")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResendEmailConfirmation([FromBody] ResendEmailConfirmationDto resendEmailDto)
    {
        var result = await _authenticationService.ResendEmailConfirmationAsync(resendEmailDto.Email);

        if (result.IsSuccess)
        {
            _logger.LogInformation("Email confirmation resent successfully: {Email}", resendEmailDto.Email);
            return Ok(new { message = "Email confirmation sent successfully" });
        }

        _logger.LogWarning("Resend email confirmation failed: {Email}, Error: {Error}", resendEmailDto.Email, result.Error);
        return BadRequest(new ProblemDetails
        {
            Title = "Resend Email Confirmation Failed",
            Detail = result.Error,
            Status = StatusCodes.Status400BadRequest
        });
    }

    /// <summary>
    /// Request password reset
    /// </summary>
    /// <param name="forgotPasswordDto">Email address for password reset</param>
    /// <returns>Success confirmation</returns>
    [HttpPost("forgot-password")]
    [EnableRateLimiting("AuthPolicy")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto forgotPasswordDto)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = HttpContext.Request.Headers.UserAgent.ToString();

        var result = await _authenticationService.ForgotPasswordAsync(forgotPasswordDto, ipAddress, userAgent);

        if (result.IsSuccess)
        {
            _logger.LogInformation("Password reset requested: {Email}", forgotPasswordDto.Email);
            return Ok(new { message = "Password reset email sent if account exists" });
        }

        _logger.LogWarning("Password reset failed: {Email}, Error: {Error}", forgotPasswordDto.Email, result.Error);
        return BadRequest(new ProblemDetails
        {
            Title = "Password Reset Failed",
            Detail = result.Error,
            Status = StatusCodes.Status400BadRequest
        });
    }

    /// <summary>
    /// Reset password using reset token
    /// </summary>
    /// <param name="resetPasswordDto">Password reset details</param>
    /// <returns>Success confirmation</returns>
    [HttpPost("reset-password")]
    [EnableRateLimiting("AuthPolicy")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto resetPasswordDto)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = HttpContext.Request.Headers.UserAgent.ToString();

        var result = await _authenticationService.ResetPasswordAsync(resetPasswordDto, ipAddress, userAgent);

        if (result.IsSuccess)
        {
            _logger.LogInformation("Password reset successfully: {Email}", resetPasswordDto.Email);
            return Ok(new { message = "Password reset successfully" });
        }

        _logger.LogWarning("Password reset failed: {Email}, Error: {Error}", resetPasswordDto.Email, result.Error);
        return BadRequest(new ProblemDetails
        {
            Title = "Password Reset Failed",
            Detail = result.Error,
            Status = StatusCodes.Status400BadRequest
        });
    }

    /// <summary>
    /// Change user password
    /// </summary>
    /// <param name="changePasswordDto">Password change details</param>
    /// <returns>Success confirmation</returns>
    [HttpPost("change-password")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto changePasswordDto)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = HttpContext.Request.Headers.UserAgent.ToString();

        var result = await _authenticationService.ChangePasswordAsync(userId.Value, changePasswordDto, ipAddress, userAgent);

        if (result.IsSuccess)
        {
            _logger.LogInformation("Password changed successfully: {UserId}", userId);
            return Ok(new { message = "Password changed successfully" });
        }

        _logger.LogWarning("Password change failed: {UserId}, Error: {Error}", userId, result.Error);
        return BadRequest(new ProblemDetails
        {
            Title = "Password Change Failed",
            Detail = result.Error,
            Status = StatusCodes.Status400BadRequest
        });
    }

    private Guid? GetCurrentUserId()
    {
        var userIdClaim = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }
}
