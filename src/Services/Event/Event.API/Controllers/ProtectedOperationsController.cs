using Event.API.Attributes;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace Event.API.Controllers;

/// <summary>
/// Example controller demonstrating various rate limiting strategies
/// </summary>
[ApiController]
[Route("api/v1/protected")]
[ApiVersion("1.0")]
public class ProtectedOperationsController : ControllerBase
{
    private readonly ILogger<ProtectedOperationsController> _logger;

    public ProtectedOperationsController(ILogger<ProtectedOperationsController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Standard rate limited operation
    /// </summary>
    [HttpPost("standard")]
    [RateLimit(50, "1m")] // 50 requests per minute
    public async Task<ActionResult<object>> StandardOperation([FromBody] StandardRequest request)
    {
        _logger.LogInformation("Processing standard operation for {Data}", request.Data);
        
        // Simulate some processing
        await Task.Delay(100);
        
        return Ok(new
        {
            message = "Standard operation completed",
            data = request.Data,
            processedAt = DateTime.UtcNow
        });
    }

    /// <summary>
    /// High-frequency operation with burst protection
    /// </summary>
    [HttpPost("high-frequency")]
    [BurstProtection(10, "10s", 100, "1h")] // 10 per 10 seconds, 100 per hour
    public async Task<ActionResult<object>> HighFrequencyOperation([FromBody] StandardRequest request)
    {
        _logger.LogInformation("Processing high-frequency operation for {Data}", request.Data);
        
        // Simulate rapid processing
        await Task.Delay(10);
        
        return Ok(new
        {
            message = "High-frequency operation completed",
            data = request.Data,
            processedAt = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Critical operation with strict per-IP limits
    /// </summary>
    [HttpPost("critical")]
    [RateLimit(5, "1m", PerIP = true)] // 5 requests per minute per IP
    public async Task<ActionResult<object>> CriticalOperation([FromBody] CriticalRequest request)
    {
        _logger.LogInformation("Processing critical operation for amount {Amount}", request.Amount);
        
        // Simulate critical processing
        await Task.Delay(500);
        
        return Ok(new
        {
            message = "Critical operation completed",
            amount = request.Amount,
            processedAt = DateTime.UtcNow,
            transactionId = Guid.NewGuid()
        });
    }

    /// <summary>
    /// Progressive rate limiting - becomes more restrictive with violations
    /// </summary>
    [HttpPost("progressive")]
    [ProgressiveRateLimit(100, 50, 10)] // 100 -> 50 -> 10 requests per hour
    public async Task<ActionResult<object>> ProgressiveOperation([FromBody] StandardRequest request)
    {
        _logger.LogInformation("Processing progressive operation for {Data}", request.Data);
        
        await Task.Delay(50);
        
        return Ok(new
        {
            message = "Progressive operation completed",
            data = request.Data,
            processedAt = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Per-organization rate limited operation
    /// </summary>
    [HttpPost("organization")]
    [RateLimit(200, "1h", PerOrganization = true)] // 200 requests per hour per organization
    public async Task<ActionResult<object>> OrganizationOperation([FromBody] StandardRequest request)
    {
        var organizationId = HttpContext.User?.FindFirst("org_id")?.Value ?? "unknown";
        
        _logger.LogInformation("Processing organization operation for org {OrgId} with data {Data}", 
            organizationId, request.Data);
        
        await Task.Delay(200);
        
        return Ok(new
        {
            message = "Organization operation completed",
            organizationId,
            data = request.Data,
            processedAt = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Unrestricted operation (bypasses rate limiting)
    /// </summary>
    [HttpGet("unrestricted")]
    [NoRateLimit]
    public ActionResult<object> UnrestrictedOperation()
    {
        _logger.LogInformation("Processing unrestricted operation");
        
        return Ok(new
        {
            message = "Unrestricted operation completed",
            processedAt = DateTime.UtcNow,
            note = "This operation bypasses all rate limiting"
        });
    }

    /// <summary>
    /// Batch operation with special rate limiting
    /// </summary>
    [HttpPost("batch")]
    [RateLimit(10, "1m", ErrorMessage = "Batch operations are limited to 10 per minute")]
    public async Task<ActionResult<object>> BatchOperation([FromBody] BatchRequest request)
    {
        _logger.LogInformation("Processing batch operation with {Count} items", request.Items.Count);
        
        if (request.Items.Count > 100)
        {
            return BadRequest("Batch size cannot exceed 100 items");
        }
        
        // Simulate batch processing
        await Task.Delay(request.Items.Count * 10);
        
        var results = request.Items.Select((item, index) => new
        {
            index,
            item,
            processed = true,
            processedAt = DateTime.UtcNow
        }).ToList();
        
        return Ok(new
        {
            message = "Batch operation completed",
            totalItems = request.Items.Count,
            results,
            batchId = Guid.NewGuid()
        });
    }

    /// <summary>
    /// Resource-intensive operation with very strict limits
    /// </summary>
    [HttpPost("resource-intensive")]
    [RateLimit(3, "1h", PerIP = true, ErrorMessage = "Resource-intensive operations are limited to 3 per hour per IP")]
    public async Task<ActionResult<object>> ResourceIntensiveOperation([FromBody] ResourceIntensiveRequest request)
    {
        _logger.LogInformation("Processing resource-intensive operation with complexity {Complexity}", 
            request.ComplexityLevel);
        
        // Simulate resource-intensive processing based on complexity
        var processingTime = request.ComplexityLevel * 1000; // milliseconds
        await Task.Delay(processingTime);
        
        return Ok(new
        {
            message = "Resource-intensive operation completed",
            complexityLevel = request.ComplexityLevel,
            processingTime,
            processedAt = DateTime.UtcNow,
            jobId = Guid.NewGuid()
        });
    }

    /// <summary>
    /// Gets current rate limit status for the calling client
    /// </summary>
    [HttpGet("rate-limit-status")]
    [NoRateLimit]
    public ActionResult<object> GetRateLimitStatus()
    {
        var clientId = GetClientId();
        var ipAddress = GetClientIpAddress();
        
        return Ok(new
        {
            clientId,
            ipAddress,
            message = "Use admin endpoints for detailed rate limit status",
            note = "This endpoint shows basic client identification used for rate limiting"
        });
    }

    /// <summary>
    /// Simulates payment processing with ultra-strict rate limiting
    /// </summary>
    [HttpPost("payment")]
    [RateLimit(1, "1s", PerIP = true)] // Maximum 1 payment per second per IP
    public async Task<ActionResult<object>> ProcessPayment([FromBody] PaymentRequest request)
    {
        _logger.LogInformation("Processing payment of {Amount} {Currency}", 
            request.Amount, request.Currency);
        
        // Simulate payment processing
        await Task.Delay(2000);
        
        return Ok(new
        {
            message = "Payment processed successfully",
            amount = request.Amount,
            currency = request.Currency,
            transactionId = Guid.NewGuid(),
            processedAt = DateTime.UtcNow
        });
    }

    #region Private Methods

    private string GetClientId()
    {
        return Request.Headers["X-Client-Id"].FirstOrDefault() ??
               Request.Headers["X-API-Key"].FirstOrDefault() ??
               HttpContext.User?.FindFirst("client_id")?.Value ??
               HttpContext.User?.FindFirst("sub")?.Value ??
               $"ip:{GetClientIpAddress()}";
    }

    private string GetClientIpAddress()
    {
        return Request.Headers["X-Real-IP"].FirstOrDefault() ??
               Request.Headers["X-Forwarded-For"].FirstOrDefault()?.Split(',')[0] ??
               HttpContext.Connection.RemoteIpAddress?.ToString() ??
               "unknown";
    }

    #endregion
}

#region Request Models

public class StandardRequest
{
    [Required]
    public string Data { get; set; } = string.Empty;
}

public class CriticalRequest
{
    [Required]
    [Range(0.01, 1000000)]
    public decimal Amount { get; set; }

    public string? Reference { get; set; }
}

public class BatchRequest
{
    [Required]
    [MinLength(1)]
    [MaxLength(100)]
    public List<string> Items { get; set; } = new();
}

public class ResourceIntensiveRequest
{
    [Required]
    [Range(1, 10)]
    public int ComplexityLevel { get; set; }

    public string? Description { get; set; }
}

public class PaymentRequest
{
    [Required]
    [Range(0.01, double.MaxValue)]
    public decimal Amount { get; set; }

    [Required]
    [StringLength(3, MinimumLength = 3)]
    public string Currency { get; set; } = "USD";

    public string? PaymentMethod { get; set; }
}

#endregion
