using Identity.Domain.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Identity.API.Controllers;

/// <summary>
/// Controller for managing security notifications and alerts
/// </summary>
[ApiController]
[Route("api/security-notifications")]
[Authorize(Roles = "Admin,SecurityTeam")]
public class SecurityNotificationController : ControllerBase
{
    private readonly ISecurityNotificationService _notificationService;
    private readonly IDiscordNotificationService _discordService;
    private readonly ISecurityService _securityService;
    private readonly ILogger<SecurityNotificationController> _logger;

    public SecurityNotificationController(
        ISecurityNotificationService notificationService,
        IDiscordNotificationService discordService,
        ISecurityService securityService,
        ILogger<SecurityNotificationController> logger)
    {
        _notificationService = notificationService;
        _discordService = discordService;
        _securityService = securityService;
        _logger = logger;
    }

    /// <summary>
    /// Send a test notification to verify Discord integration
    /// </summary>
    /// <param name="message">The test message to send</param>
    /// <returns></returns>
    [HttpPost("test")]
    public async Task<IActionResult> SendTestNotification(
        [FromQuery] string message = "Test notification from BlockTicket Security System")
    {
        try
        {
            await _discordService.SendMessageAsync(message);
            
            _logger.LogInformation("Test notification sent to Discord");
            
            return Ok(new { 
                success = true, 
                message = "Test notification sent to Discord",
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending test notification to Discord");
            return StatusCode(500, new { 
                error = "Failed to send test notification", 
                details = ex.Message 
            });
        }
    }

    /// <summary>
    /// Send a critical security alert
    /// </summary>
    /// <param name="request">The critical alert request</param>
    /// <returns></returns>
    [HttpPost("critical-alert")]
    public async Task<IActionResult> SendCriticalAlert([FromBody] CriticalAlertRequest request)
    {
        try
        {
            await _notificationService.SendCriticalSecurityAlertAsync(
                request.Message, 
                request.Context);
            
            _logger.LogWarning("Manual critical security alert sent: {Message}", request.Message);
            
            return Ok(new { 
                success = true, 
                message = "Critical alert sent successfully",
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending critical alert: {Message}", request.Message);
            return StatusCode(500, new { 
                error = "Failed to send critical alert", 
                details = ex.Message 
            });
        }
    }

    /// <summary>
    /// Generate and send a security summary for a specific date range
    /// </summary>
    /// <param name="from">Start date (YYYY-MM-DD)</param>
    /// <param name="to">End date (YYYY-MM-DD)</param>
    /// <returns></returns>
    [HttpPost("summary")]
    public async Task<IActionResult> SendSecuritySummary(
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null)
    {
        try
        {
            var fromDate = from ?? DateTime.UtcNow.Date.AddDays(-1);
            var toDate = to ?? DateTime.UtcNow.Date;

            if (fromDate >= toDate)
            {
                return BadRequest("From date must be before to date");
            }

            if ((toDate - fromDate).TotalDays > 30)
            {
                return BadRequest("Date range cannot exceed 30 days");
            }

            await _notificationService.SendSecuritySummaryAsync(fromDate, toDate);
            
            _logger.LogInformation("Security summary sent for period {From} to {To}", fromDate, toDate);
            
            return Ok(new { 
                success = true, 
                message = $"Security summary sent for {fromDate:yyyy-MM-dd} to {toDate:yyyy-MM-dd}",
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending security summary for period {From} to {To}", from, to);
            return StatusCode(500, new { 
                error = "Failed to send security summary", 
                details = ex.Message 
            });
        }
    }

    /// <summary>
    /// Get recent security events that would trigger notifications
    /// </summary>
    /// <param name="hours">Number of hours to look back (default: 24)</param>
    /// <returns></returns>
    [HttpGet("recent-events")]
    public async Task<IActionResult> GetRecentNotifiableEvents([FromQuery] int hours = 24)
    {
        try
        {
            if (hours <= 0 || hours > 168) // Max 1 week
            {
                return BadRequest("Hours must be between 1 and 168");
            }

            var from = DateTime.UtcNow.AddHours(-hours);
            var events = await _securityService.GetSecurityEventsAsync(null, from, null);

            var notifiableEvents = events.Where(e => 
                e.Severity >= Domain.Entities.SecurityEventSeverity.Medium)
                .OrderByDescending(e => e.CreatedAt)
                .Take(100)
                .Select(e => new
                {
                    e.Id,
                    e.EventType,
                    e.EventCategory,
                    e.Severity,
                    e.Description,
                    e.IpAddress,
                    e.UserId,
                    e.CreatedAt,
                    e.IsResolved
                });

            return Ok(new
            {
                events = notifiableEvents,
                count = notifiableEvents.Count(),
                timeRange = new { from, to = DateTime.UtcNow }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving recent notifiable events");
            return StatusCode(500, new { 
                error = "Failed to retrieve recent events", 
                details = ex.Message 
            });
        }
    }

    /// <summary>
    /// Test Discord integration with a formatted security alert
    /// </summary>
    /// <returns></returns>
    [HttpPost("test-security-alert")]
    public async Task<IActionResult> SendTestSecurityAlert()
    {
        try
        {
            // Create a test security event
            var testEvent = Domain.Entities.SecurityEvent.CreateLoginAttempt(
                Guid.NewGuid(),
                "192.168.1.100",
                "Mozilla/5.0 (Test User Agent)",
                false,
                "Test security alert");

            await _discordService.SendSecurityAlertAsync(testEvent);
            
            _logger.LogInformation("Test security alert sent to Discord");
            
            return Ok(new { 
                success = true, 
                message = "Test security alert sent successfully",
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending test security alert");
            return StatusCode(500, new { 
                error = "Failed to send test security alert", 
                details = ex.Message 
            });
        }
    }

    /// <summary>
    /// Get notification configuration status
    /// </summary>
    /// <returns></returns>
    [HttpGet("status")]
    public IActionResult GetNotificationStatus()
    {
        try
        {
            return Ok(new
            {
                discordEnabled = !string.IsNullOrEmpty(HttpContext.RequestServices
                    .GetService<IConfiguration>()?["Notifications:Discord:DefaultWebhookUrl"]),
                timestamp = DateTime.UtcNow,
                version = "1.0.0"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting notification status");
            return StatusCode(500, new { 
                error = "Failed to get notification status", 
                details = ex.Message 
            });
        }
    }
}

/// <summary>
/// Request model for sending critical alerts
/// </summary>
public class CriticalAlertRequest
{
    public string Message { get; set; } = string.Empty;
    public string? Context { get; set; }
}
