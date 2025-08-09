using Identity.Application.DTOs;
using Identity.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Identity.API.Controllers.V1;

/// <summary>
/// OAuth 2.0 client and scope management endpoints
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/oauth")]
[Authorize(Roles = "admin,super_admin")]
[Produces("application/json")]
public class OAuthController : ControllerBase
{
    private readonly IOAuthService _oauthService;
    private readonly ILogger<OAuthController> _logger;

    public OAuthController(
        IOAuthService oauthService,
        ILogger<OAuthController> logger)
    {
        _oauthService = oauthService;
        _logger = logger;
    }

    #region Client Management

    /// <summary>
    /// Get all OAuth clients
    /// </summary>
    /// <param name="activeOnly">Filter to active clients only</param>
    /// <returns>List of OAuth clients</returns>
    [HttpGet("clients")]
    [ProducesResponseType(typeof(IEnumerable<OAuthClientDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetClients([FromQuery] bool activeOnly = false)
    {
        var result = await _oauthService.GetClientsAsync(activeOnly);

        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        return BadRequest(new ProblemDetails
        {
            Title = "Failed to retrieve OAuth clients",
            Detail = result.Error,
            Status = StatusCodes.Status400BadRequest
        });
    }

    /// <summary>
    /// Get OAuth client by ID
    /// </summary>
    /// <param name="clientId">OAuth client ID</param>
    /// <returns>OAuth client details</returns>
    [HttpGet("clients/{clientId}")]
    [ProducesResponseType(typeof(OAuthClientDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetClient(string clientId)
    {
        var result = await _oauthService.GetClientAsync(clientId);

        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        if (result.Error.Contains("not found"))
        {
            return NotFound(new ProblemDetails
            {
                Title = "OAuth Client Not Found",
                Detail = result.Error,
                Status = StatusCodes.Status404NotFound
            });
        }

        return BadRequest(new ProblemDetails
        {
            Title = "Failed to retrieve OAuth client",
            Detail = result.Error,
            Status = StatusCodes.Status400BadRequest
        });
    }

    /// <summary>
    /// Register new OAuth client
    /// </summary>
    /// <param name="createClientDto">OAuth client registration details</param>
    /// <returns>Created OAuth client with client secret</returns>
    [HttpPost("clients")]
    [EnableRateLimiting("AdminPolicy")]
    [ProducesResponseType(typeof(OAuthClientDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RegisterClient([FromBody] CreateOAuthClientDto createClientDto)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = HttpContext.Request.Headers.UserAgent.ToString();

        var result = await _oauthService.RegisterClientAsync(createClientDto, ipAddress, userAgent);

        if (result.IsSuccess)
        {
            _logger.LogInformation("OAuth client registered: {ClientId}", createClientDto.ClientId);
            return CreatedAtAction(nameof(GetClient), new { clientId = createClientDto.ClientId }, result.Value);
        }

        if (result.Error.Contains("already exists"))
        {
            return Conflict(new ProblemDetails
            {
                Title = "OAuth Client Already Exists",
                Detail = result.Error,
                Status = StatusCodes.Status409Conflict
            });
        }

        _logger.LogWarning("OAuth client registration failed: {ClientId}, Error: {Error}", createClientDto.ClientId, result.Error);
        return BadRequest(new ProblemDetails
        {
            Title = "OAuth Client Registration Failed",
            Detail = result.Error,
            Status = StatusCodes.Status400BadRequest
        });
    }

    /// <summary>
    /// Update OAuth client
    /// </summary>
    /// <param name="clientId">OAuth client ID</param>
    /// <param name="updateClientDto">OAuth client update details</param>
    /// <returns>Updated OAuth client</returns>
    [HttpPut("clients/{clientId}")]
    [EnableRateLimiting("AdminPolicy")]
    [ProducesResponseType(typeof(OAuthClientDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateClient(string clientId, [FromBody] UpdateOAuthClientDto updateClientDto)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = HttpContext.Request.Headers.UserAgent.ToString();

        var result = await _oauthService.UpdateClientAsync(clientId, updateClientDto, ipAddress, userAgent);

        if (result.IsSuccess)
        {
            _logger.LogInformation("OAuth client updated: {ClientId}", clientId);
            return Ok(result.Value);
        }

        if (result.Error.Contains("not found"))
        {
            return NotFound(new ProblemDetails
            {
                Title = "OAuth Client Not Found",
                Detail = result.Error,
                Status = StatusCodes.Status404NotFound
            });
        }

        _logger.LogWarning("OAuth client update failed: {ClientId}, Error: {Error}", clientId, result.Error);
        return BadRequest(new ProblemDetails
        {
            Title = "OAuth Client Update Failed",
            Detail = result.Error,
            Status = StatusCodes.Status400BadRequest
        });
    }

    /// <summary>
    /// Delete OAuth client
    /// </summary>
    /// <param name="clientId">OAuth client ID</param>
    /// <returns>Deletion confirmation</returns>
    [HttpDelete("clients/{clientId}")]
    [EnableRateLimiting("AdminPolicy")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteClient(string clientId)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = HttpContext.Request.Headers.UserAgent.ToString();

        var result = await _oauthService.DeleteClientAsync(clientId, ipAddress, userAgent);

        if (result.IsSuccess)
        {
            _logger.LogInformation("OAuth client deleted: {ClientId}", clientId);
            return Ok(new { message = "OAuth client deleted successfully" });
        }

        if (result.Error.Contains("not found"))
        {
            return NotFound(new ProblemDetails
            {
                Title = "OAuth Client Not Found",
                Detail = result.Error,
                Status = StatusCodes.Status404NotFound
            });
        }

        _logger.LogWarning("OAuth client deletion failed: {ClientId}, Error: {Error}", clientId, result.Error);
        return BadRequest(new ProblemDetails
        {
            Title = "OAuth Client Deletion Failed",
            Detail = result.Error,
            Status = StatusCodes.Status400BadRequest
        });
    }

    #endregion

    #region Scope Management

    /// <summary>
    /// Get all OAuth scopes
    /// </summary>
    /// <param name="discoveryOnly">Filter to discovery scopes only</param>
    /// <returns>List of OAuth scopes</returns>
    [HttpGet("scopes")]
    [ProducesResponseType(typeof(IEnumerable<ScopeDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetScopes([FromQuery] bool discoveryOnly = false)
    {
        var result = await _oauthService.GetScopesAsync(discoveryOnly);

        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        return BadRequest(new ProblemDetails
        {
            Title = "Failed to retrieve OAuth scopes",
            Detail = result.Error,
            Status = StatusCodes.Status400BadRequest
        });
    }

    /// <summary>
    /// Create new OAuth scope
    /// </summary>
    /// <param name="createScopeDto">OAuth scope creation details</param>
    /// <returns>Created OAuth scope</returns>
    [HttpPost("scopes")]
    [EnableRateLimiting("AdminPolicy")]
    [ProducesResponseType(typeof(ScopeDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateScope([FromBody] CreateScopeDto createScopeDto)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = HttpContext.Request.Headers.UserAgent.ToString();

        var result = await _oauthService.CreateScopeAsync(createScopeDto, ipAddress, userAgent);

        if (result.IsSuccess)
        {
            _logger.LogInformation("OAuth scope created: {ScopeName}", createScopeDto.Name);
            return CreatedAtAction(nameof(GetScopes), new { }, result.Value);
        }

        if (result.Error.Contains("already exists"))
        {
            return Conflict(new ProblemDetails
            {
                Title = "OAuth Scope Already Exists",
                Detail = result.Error,
                Status = StatusCodes.Status409Conflict
            });
        }

        _logger.LogWarning("OAuth scope creation failed: {ScopeName}, Error: {Error}", createScopeDto.Name, result.Error);
        return BadRequest(new ProblemDetails
        {
            Title = "OAuth Scope Creation Failed",
            Detail = result.Error,
            Status = StatusCodes.Status400BadRequest
        });
    }

    /// <summary>
    /// Update OAuth scope
    /// </summary>
    /// <param name="scopeName">OAuth scope name</param>
    /// <param name="updateScopeDto">OAuth scope update details</param>
    /// <returns>Updated OAuth scope</returns>
    [HttpPut("scopes/{scopeName}")]
    [EnableRateLimiting("AdminPolicy")]
    [ProducesResponseType(typeof(ScopeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateScope(string scopeName, [FromBody] UpdateScopeDto updateScopeDto)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = HttpContext.Request.Headers.UserAgent.ToString();

        var result = await _oauthService.UpdateScopeAsync(scopeName, updateScopeDto, ipAddress, userAgent);

        if (result.IsSuccess)
        {
            _logger.LogInformation("OAuth scope updated: {ScopeName}", scopeName);
            return Ok(result.Value);
        }

        if (result.Error.Contains("not found"))
        {
            return NotFound(new ProblemDetails
            {
                Title = "OAuth Scope Not Found",
                Detail = result.Error,
                Status = StatusCodes.Status404NotFound
            });
        }

        _logger.LogWarning("OAuth scope update failed: {ScopeName}, Error: {Error}", scopeName, result.Error);
        return BadRequest(new ProblemDetails
        {
            Title = "OAuth Scope Update Failed",
            Detail = result.Error,
            Status = StatusCodes.Status400BadRequest
        });
    }

    /// <summary>
    /// Delete OAuth scope
    /// </summary>
    /// <param name="scopeName">OAuth scope name</param>
    /// <returns>Deletion confirmation</returns>
    [HttpDelete("scopes/{scopeName}")]
    [EnableRateLimiting("AdminPolicy")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteScope(string scopeName)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = HttpContext.Request.Headers.UserAgent.ToString();

        var result = await _oauthService.DeleteScopeAsync(scopeName, ipAddress, userAgent);

        if (result.IsSuccess)
        {
            _logger.LogInformation("OAuth scope deleted: {ScopeName}", scopeName);
            return Ok(new { message = "OAuth scope deleted successfully" });
        }

        if (result.Error.Contains("not found"))
        {
            return NotFound(new ProblemDetails
            {
                Title = "OAuth Scope Not Found",
                Detail = result.Error,
                Status = StatusCodes.Status404NotFound
            });
        }

        _logger.LogWarning("OAuth scope deletion failed: {ScopeName}, Error: {Error}", scopeName, result.Error);
        return BadRequest(new ProblemDetails
        {
            Title = "OAuth Scope Deletion Failed",
            Detail = result.Error,
            Status = StatusCodes.Status400BadRequest
        });
    }

    #endregion
}
