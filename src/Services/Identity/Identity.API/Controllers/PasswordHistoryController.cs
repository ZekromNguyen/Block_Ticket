using Identity.Application.DTOs;
using Identity.Domain.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Identity.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PasswordHistoryController : ControllerBase
{
    private readonly IPasswordHistoryService _passwordHistoryService;
    private readonly ILogger<PasswordHistoryController> _logger;

    public PasswordHistoryController(
        IPasswordHistoryService passwordHistoryService,
        ILogger<PasswordHistoryController> logger)
    {
        _passwordHistoryService = passwordHistoryService;
        _logger = logger;
    }

    /// <summary>
    /// Triggers password history cleanup for the current user
    /// </summary>
    [HttpPost("cleanup")]
    public async Task<IActionResult> CleanupPasswordHistory()
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized();
            }

            await _passwordHistoryService.CleanupPasswordHistoryAsync(userId);
            
            _logger.LogInformation("Password history cleanup completed for user {UserId}", userId);
            return Ok(new { message = "Password history cleanup completed successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during password history cleanup");
            return StatusCode(500, new { message = "An error occurred during password history cleanup" });
        }
    }

    /// <summary>
    /// Admin endpoint to trigger global password history cleanup
    /// </summary>
    [HttpPost("cleanup/all")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> CleanupAllPasswordHistory()
    {
        try
        {
            await _passwordHistoryService.CleanupAllPasswordHistoryAsync();
            
            _logger.LogInformation("Global password history cleanup completed");
            return Ok(new { message = "Global password history cleanup completed successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during global password history cleanup");
            return StatusCode(500, new { message = "An error occurred during global password history cleanup" });
        }
    }
}
