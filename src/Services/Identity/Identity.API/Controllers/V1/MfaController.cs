using Identity.Application.DTOs;
using Identity.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;

namespace Identity.API.Controllers.V1;

/// <summary>
/// Multi-Factor Authentication management endpoints
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/mfa")]
[Authorize]
[Produces("application/json")]
public class MfaController : ControllerBase
{
    private readonly IMfaApplicationService _mfaService;
    private readonly ILogger<MfaController> _logger;

    public MfaController(
        IMfaApplicationService mfaService,
        ILogger<MfaController> logger)
    {
        _mfaService = mfaService;
        _logger = logger;
    }

    /// <summary>
    /// Get user's MFA devices
    /// </summary>
    /// <returns>List of user's MFA devices</returns>
    [HttpGet("devices")]
    [ProducesResponseType(typeof(IEnumerable<MfaDeviceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMfaDevices()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var result = await _mfaService.GetUserMfaDevicesAsync(userId.Value);

        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        return BadRequest(new ProblemDetails
        {
            Title = "Failed to retrieve MFA devices",
            Detail = result.Error,
            Status = StatusCodes.Status400BadRequest
        });
    }

    /// <summary>
    /// Setup TOTP (Time-based One-Time Password) authentication
    /// </summary>
    /// <returns>TOTP setup information including QR code</returns>
    [HttpPost("totp/setup")]
    [EnableRateLimiting("MfaPolicy")]
    [ProducesResponseType(typeof(SetupTotpDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> SetupTotp()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = HttpContext.Request.Headers.UserAgent.ToString();

        var result = await _mfaService.SetupTotpAsync(userId.Value, ipAddress, userAgent);

        if (result.IsSuccess)
        {
            _logger.LogInformation("TOTP setup initiated for user: {UserId}", userId);
            return Ok(result.Value);
        }

        _logger.LogWarning("TOTP setup failed for user: {UserId}, Error: {Error}", userId, result.Error);
        return BadRequest(new ProblemDetails
        {
            Title = "TOTP Setup Failed",
            Detail = result.Error,
            Status = StatusCodes.Status400BadRequest
        });
    }

    /// <summary>
    /// Verify and complete TOTP setup
    /// </summary>
    /// <param name="verifyTotpDto">TOTP verification details</param>
    /// <returns>Setup completion confirmation</returns>
    [HttpPost("totp/verify")]
    [EnableRateLimiting("MfaPolicy")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> VerifyTotpSetup([FromBody] VerifyTotpSetupDto verifyTotpDto)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = HttpContext.Request.Headers.UserAgent.ToString();

        var result = await _mfaService.VerifyTotpSetupAsync(userId.Value, verifyTotpDto, ipAddress, userAgent);

        if (result.IsSuccess)
        {
            _logger.LogInformation("TOTP setup completed for user: {UserId}", userId);
            return Ok(new { message = "TOTP setup completed successfully" });
        }

        _logger.LogWarning("TOTP verification failed for user: {UserId}, Error: {Error}", userId, result.Error);
        return BadRequest(new ProblemDetails
        {
            Title = "TOTP Verification Failed",
            Detail = result.Error,
            Status = StatusCodes.Status400BadRequest
        });
    }

    /// <summary>
    /// Setup Email OTP authentication
    /// </summary>
    /// <param name="setupEmailOtpDto">Email OTP setup details</param>
    /// <returns>Setup confirmation</returns>
    [HttpPost("email/setup")]
    [EnableRateLimiting("MfaPolicy")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> SetupEmailOtp([FromBody] SetupEmailOtpDto setupEmailOtpDto)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = HttpContext.Request.Headers.UserAgent.ToString();

        var result = await _mfaService.SetupEmailOtpAsync(userId.Value, setupEmailOtpDto, ipAddress, userAgent);

        if (result.IsSuccess)
        {
            _logger.LogInformation("Email OTP setup initiated for user: {UserId}", userId);
            return Ok(new { message = "Email OTP setup initiated. Check your email for verification code." });
        }

        _logger.LogWarning("Email OTP setup failed for user: {UserId}, Error: {Error}", userId, result.Error);
        return BadRequest(new ProblemDetails
        {
            Title = "Email OTP Setup Failed",
            Detail = result.Error,
            Status = StatusCodes.Status400BadRequest
        });
    }

    /// <summary>
    /// Verify Email OTP setup
    /// </summary>
    /// <param name="verifyEmailOtpDto">Email OTP verification details</param>
    /// <returns>Setup completion confirmation</returns>
    [HttpPost("email/verify")]
    [EnableRateLimiting("MfaPolicy")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> VerifyEmailOtp([FromBody] VerifyEmailOtpDto verifyEmailOtpDto)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = HttpContext.Request.Headers.UserAgent.ToString();

        var result = await _mfaService.VerifyEmailOtpAsync(userId.Value, verifyEmailOtpDto, ipAddress, userAgent);

        if (result.IsSuccess)
        {
            _logger.LogInformation("Email OTP setup completed for user: {UserId}", userId);
            return Ok(new { message = "Email OTP setup completed successfully" });
        }

        _logger.LogWarning("Email OTP verification failed for user: {UserId}, Error: {Error}", userId, result.Error);
        return BadRequest(new ProblemDetails
        {
            Title = "Email OTP Verification Failed",
            Detail = result.Error,
            Status = StatusCodes.Status400BadRequest
        });
    }

    /// <summary>
    /// Initiate WebAuthn setup
    /// </summary>
    /// <returns>WebAuthn challenge for setup</returns>
    [HttpPost("webauthn/setup")]
    [EnableRateLimiting("MfaPolicy")]
    [ProducesResponseType(typeof(WebAuthnChallengeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> InitiateWebAuthnSetup()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = HttpContext.Request.Headers.UserAgent.ToString();

        var result = await _mfaService.InitiateWebAuthnSetupAsync(userId.Value, ipAddress, userAgent);

        if (result.IsSuccess)
        {
            _logger.LogInformation("WebAuthn setup initiated for user: {UserId}", userId);
            return Ok(result.Value);
        }

        _logger.LogWarning("WebAuthn setup failed for user: {UserId}, Error: {Error}", userId, result.Error);
        return BadRequest(new ProblemDetails
        {
            Title = "WebAuthn Setup Failed",
            Detail = result.Error,
            Status = StatusCodes.Status400BadRequest
        });
    }

    /// <summary>
    /// Complete WebAuthn setup
    /// </summary>
    /// <param name="setupWebAuthnDto">WebAuthn setup completion details</param>
    /// <returns>Setup completion confirmation</returns>
    [HttpPost("webauthn/complete")]
    [EnableRateLimiting("MfaPolicy")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CompleteWebAuthnSetup([FromBody] SetupWebAuthnDto setupWebAuthnDto)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = HttpContext.Request.Headers.UserAgent.ToString();

        var result = await _mfaService.CompleteWebAuthnSetupAsync(userId.Value, setupWebAuthnDto, ipAddress, userAgent);

        if (result.IsSuccess)
        {
            _logger.LogInformation("WebAuthn setup completed for user: {UserId}", userId);
            return Ok(new { message = "WebAuthn setup completed successfully" });
        }

        _logger.LogWarning("WebAuthn setup completion failed for user: {UserId}, Error: {Error}", userId, result.Error);
        return BadRequest(new ProblemDetails
        {
            Title = "WebAuthn Setup Completion Failed",
            Detail = result.Error,
            Status = StatusCodes.Status400BadRequest
        });
    }

    /// <summary>
    /// Generate backup codes
    /// </summary>
    /// <returns>Generated backup codes</returns>
    [HttpPost("backup-codes/generate")]
    [EnableRateLimiting("MfaPolicy")]
    [ProducesResponseType(typeof(GenerateBackupCodesDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GenerateBackupCodes()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = HttpContext.Request.Headers.UserAgent.ToString();

        var result = await _mfaService.GenerateBackupCodesAsync(userId.Value, ipAddress, userAgent);

        if (result.IsSuccess)
        {
            _logger.LogInformation("Backup codes generated for user: {UserId}", userId);
            return Ok(result.Value);
        }

        _logger.LogWarning("Backup codes generation failed for user: {UserId}, Error: {Error}", userId, result.Error);
        return BadRequest(new ProblemDetails
        {
            Title = "Backup Codes Generation Failed",
            Detail = result.Error,
            Status = StatusCodes.Status400BadRequest
        });
    }

    /// <summary>
    /// Remove MFA device
    /// </summary>
    /// <param name="deviceId">MFA device ID to remove</param>
    /// <returns>Removal confirmation</returns>
    [HttpDelete("devices/{deviceId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveMfaDevice(Guid deviceId)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = HttpContext.Request.Headers.UserAgent.ToString();

        var result = await _mfaService.RemoveMfaDeviceAsync(userId.Value, deviceId, ipAddress, userAgent);

        if (result.IsSuccess)
        {
            _logger.LogInformation("MFA device removed for user: {UserId}, Device: {DeviceId}", userId, deviceId);
            return Ok(new { message = "MFA device removed successfully" });
        }

        _logger.LogWarning("MFA device removal failed for user: {UserId}, Device: {DeviceId}, Error: {Error}", userId, deviceId, result.Error);
        return BadRequest(new ProblemDetails
        {
            Title = "MFA Device Removal Failed",
            Detail = result.Error,
            Status = StatusCodes.Status400BadRequest
        });
    }

    /// <summary>
    /// Disable MFA for user
    /// </summary>
    /// <returns>Disable confirmation</returns>
    [HttpPost("disable")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DisableMfa()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = HttpContext.Request.Headers.UserAgent.ToString();

        var result = await _mfaService.DisableMfaAsync(userId.Value, ipAddress, userAgent);

        if (result.IsSuccess)
        {
            _logger.LogInformation("MFA disabled for user: {UserId}", userId);
            return Ok(new { message = "MFA disabled successfully" });
        }

        _logger.LogWarning("MFA disable failed for user: {UserId}, Error: {Error}", userId, result.Error);
        return BadRequest(new ProblemDetails
        {
            Title = "MFA Disable Failed",
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
