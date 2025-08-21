using Event.API.Attributes;
using Event.Domain.Configuration;
using Event.Application.Interfaces.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace Event.API.Controllers;

/// <summary>
/// Administrative controller for managing rate limiting
/// </summary>
[ApiController]
[Route("api/v1/admin/rate-limit")]
[ApiVersion("1.0")]
[NoRateLimit] // Admin operations should not be rate limited
public class RateLimitAdminController : ControllerBase
{
    private readonly IRateLimitService _rateLimitService;
    private readonly ILogger<RateLimitAdminController> _logger;

    public RateLimitAdminController(
        IRateLimitService rateLimitService,
        ILogger<RateLimitAdminController> logger)
    {
        _rateLimitService = rateLimitService;
        _logger = logger;
    }

    /// <summary>
    /// Gets current rate limiting configuration
    /// </summary>
    [HttpGet("config")]
    public ActionResult<RateLimitConfiguration> GetConfiguration()
    {
        var config = _rateLimitService.GetConfiguration();
        return Ok(config);
    }

    /// <summary>
    /// Gets rate limit status for a specific client
    /// </summary>
    [HttpGet("status")]
    public async Task<ActionResult<RateLimitStatus>> GetStatus(
        [FromQuery] string? clientId = null,
        [FromQuery] string? ipAddress = null,
        [FromQuery] string? endpoint = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(clientId) && string.IsNullOrEmpty(ipAddress))
        {
            return BadRequest("Either clientId or ipAddress must be provided");
        }

        var status = await _rateLimitService.GetRateLimitStatusAsync(
            clientId ?? "unknown",
            ipAddress ?? "unknown",
            endpoint ?? "*",
            cancellationToken);

        return Ok(status);
    }

    /// <summary>
    /// Gets rate limiting metrics
    /// </summary>
    [HttpGet("metrics")]
    public async Task<ActionResult<IEnumerable<RateLimitMetrics>>> GetMetrics(
        [FromQuery] int windowHours = 1,
        [FromQuery] string? clientId = null,
        [FromQuery] string? endpoint = null,
        CancellationToken cancellationToken = default)
    {
        var window = TimeSpan.FromHours(windowHours);
        var metrics = await _rateLimitService.GetMetricsAsync(window, clientId, endpoint, cancellationToken);
        return Ok(metrics);
    }

    /// <summary>
    /// Clears rate limit counters
    /// </summary>
    [HttpDelete("clear")]
    public async Task<ActionResult> ClearRateLimit(
        [FromQuery] string? clientId = null,
        [FromQuery] string? ipAddress = null,
        [FromQuery] string? endpoint = null,
        CancellationToken cancellationToken = default)
    {
        await _rateLimitService.ClearRateLimitAsync(clientId, ipAddress, endpoint, cancellationToken);

        _logger.LogInformation(
            "Rate limit cleared by admin - ClientId: {ClientId}, IP: {IpAddress}, Endpoint: {Endpoint}",
            clientId, ipAddress, endpoint);

        return Ok(new { message = "Rate limit counters cleared successfully" });
    }

    /// <summary>
    /// Adds a client to the whitelist
    /// </summary>
    [HttpPost("whitelist")]
    public async Task<ActionResult> AddToWhitelist(
        [FromBody] WhitelistRequest request,
        CancellationToken cancellationToken = default)
    {
        await _rateLimitService.AddToWhitelistAsync(
            request.ClientId,
            request.Duration,
            cancellationToken);

        _logger.LogInformation(
            "Client {ClientId} added to whitelist for {Duration} by admin",
            request.ClientId, request.Duration);

        return Ok(new { message = $"Client {request.ClientId} added to whitelist" });
    }

    /// <summary>
    /// Removes a client from the whitelist
    /// </summary>
    [HttpDelete("whitelist/{clientId}")]
    public async Task<ActionResult> RemoveFromWhitelist(
        string clientId,
        CancellationToken cancellationToken = default)
    {
        await _rateLimitService.RemoveFromWhitelistAsync(clientId, cancellationToken);

        _logger.LogInformation("Client {ClientId} removed from whitelist by admin", clientId);

        return Ok(new { message = $"Client {clientId} removed from whitelist" });
    }

    /// <summary>
    /// Checks if a client or IP is whitelisted
    /// </summary>
    [HttpGet("whitelist/check")]
    public async Task<ActionResult<object>> CheckWhitelist(
        [FromQuery] string? clientId = null,
        [FromQuery] string? ipAddress = null,
        CancellationToken cancellationToken = default)
    {
        var isWhitelisted = await _rateLimitService.IsWhitelistedAsync(clientId, ipAddress, cancellationToken);

        return Ok(new
        {
            clientId,
            ipAddress,
            isWhitelisted,
            checkedAt = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Tests rate limiting for a specific scenario
    /// </summary>
    [HttpPost("test")]
    public async Task<ActionResult<RateLimitResult>> TestRateLimit(
        [FromBody] RateLimitTestRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await _rateLimitService.CheckRateLimitAsync(
            request.ClientId,
            request.IpAddress,
            request.Endpoint,
            request.Method,
            request.OrganizationId,
            cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Gets rate limiting statistics summary
    /// </summary>
    [HttpGet("stats")]
    public async Task<ActionResult<object>> GetStatistics(
        [FromQuery] int windowHours = 24,
        CancellationToken cancellationToken = default)
    {
        var window = TimeSpan.FromHours(windowHours);
        var metrics = await _rateLimitService.GetMetricsAsync(window, cancellationToken: cancellationToken);

        var stats = new
        {
            WindowHours = windowHours,
            TotalRequests = metrics.Sum(m => m.RequestCount),
            BlockedRequests = metrics.Sum(m => m.BlockedCount),
            BlockedPercentage = metrics.Sum(m => m.RequestCount) > 0 
                ? (double)metrics.Sum(m => m.BlockedCount) / metrics.Sum(m => m.RequestCount) * 100 
                : 0,
            TopEndpoints = metrics
                .GroupBy(m => m.Endpoint)
                .Select(g => new
                {
                    Endpoint = g.Key,
                    Requests = g.Sum(m => m.RequestCount),
                    Blocked = g.Sum(m => m.BlockedCount)
                })
                .OrderByDescending(e => e.Requests)
                .Take(10),
            TopClients = metrics
                .GroupBy(m => m.ClientId)
                .Select(g => new
                {
                    ClientId = g.Key,
                    Requests = g.Sum(m => m.RequestCount),
                    Blocked = g.Sum(m => m.BlockedCount)
                })
                .OrderByDescending(c => c.Requests)
                .Take(10),
            GeneratedAt = DateTime.UtcNow
        };

        return Ok(stats);
    }

    /// <summary>
    /// Performs a bulk operation on rate limits
    /// </summary>
    [HttpPost("bulk")]
    public async Task<ActionResult> BulkOperation(
        [FromBody] BulkRateLimitRequest request,
        CancellationToken cancellationToken = default)
    {
        var results = new List<object>();

        foreach (var item in request.Items)
        {
            try
            {
                switch (request.Operation.ToLowerInvariant())
                {
                    case "clear":
                        await _rateLimitService.ClearRateLimitAsync(
                            item.ClientId, item.IpAddress, item.Endpoint, cancellationToken);
                        results.Add(new { item.ClientId, item.IpAddress, Status = "cleared" });
                        break;

                    case "whitelist":
                        if (!string.IsNullOrEmpty(item.ClientId))
                        {
                            await _rateLimitService.AddToWhitelistAsync(item.ClientId, item.Duration, cancellationToken);
                            results.Add(new { item.ClientId, Status = "whitelisted" });
                        }
                        break;

                    case "remove_whitelist":
                        if (!string.IsNullOrEmpty(item.ClientId))
                        {
                            await _rateLimitService.RemoveFromWhitelistAsync(item.ClientId, cancellationToken);
                            results.Add(new { item.ClientId, Status = "removed_from_whitelist" });
                        }
                        break;

                    default:
                        results.Add(new { item.ClientId, item.IpAddress, Status = "unknown_operation" });
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in bulk operation for client {ClientId}", item.ClientId);
                results.Add(new { item.ClientId, item.IpAddress, Status = "error", Error = ex.Message });
            }
        }

        return Ok(new { Operation = request.Operation, Results = results });
    }
}

/// <summary>
/// Request model for adding to whitelist
/// </summary>
public class WhitelistRequest
{
    [Required]
    public string ClientId { get; set; } = string.Empty;

    public TimeSpan? Duration { get; set; }

    public string? Reason { get; set; }
}

/// <summary>
/// Request model for testing rate limits
/// </summary>
public class RateLimitTestRequest
{
    [Required]
    public string ClientId { get; set; } = string.Empty;

    [Required]
    public string IpAddress { get; set; } = string.Empty;

    [Required]
    public string Endpoint { get; set; } = string.Empty;

    [Required]
    public string Method { get; set; } = string.Empty;

    public string? OrganizationId { get; set; }
}

/// <summary>
/// Request model for bulk operations
/// </summary>
public class BulkRateLimitRequest
{
    [Required]
    public string Operation { get; set; } = string.Empty; // clear, whitelist, remove_whitelist

    [Required]
    public List<BulkRateLimitItem> Items { get; set; } = new();
}

/// <summary>
/// Item for bulk operations
/// </summary>
public class BulkRateLimitItem
{
    public string? ClientId { get; set; }
    public string? IpAddress { get; set; }
    public string? Endpoint { get; set; }
    public TimeSpan? Duration { get; set; }
}
