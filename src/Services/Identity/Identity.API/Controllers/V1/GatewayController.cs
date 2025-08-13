using Identity.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;

namespace Identity.API.Controllers.V1;

/// <summary>
/// API Gateway integration endpoints for token validation and user identity
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/gateway")]
[Produces("application/json")]
public class GatewayController : ControllerBase
{
    private readonly IOAuthService _oauthService;
    private readonly IRoleService _roleService;
    private readonly ILogger<GatewayController> _logger;

    public GatewayController(
        IOAuthService oauthService,
        IRoleService roleService,
        ILogger<GatewayController> logger)
    {
        _oauthService = oauthService;
        _roleService = roleService;
        _logger = logger;
    }

    /// <summary>
    /// Validate access token and return user identity information
    /// </summary>
    /// <param name="token">Access token to validate</param>
    /// <returns>User identity and permissions</returns>
    [HttpPost("validate-token")]
    [EnableRateLimiting("GatewayPolicy")]
    [ProducesResponseType(typeof(TokenValidationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ValidateToken([FromBody] TokenValidationRequest request)
    {
        if (string.IsNullOrEmpty(request.Token))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid Request",
                Detail = "Token is required",
                Status = StatusCodes.Status400BadRequest
            });
        }

        try
        {
            // Validate token using OAuth service
            var isValid = await _oauthService.IntrospectTokenAsync(request.Token);
            
            if (!isValid.IsSuccess || !isValid.Value)
            {
                return Unauthorized(new ProblemDetails
                {
                    Title = "Invalid Token",
                    Detail = "Token is invalid or expired",
                    Status = StatusCodes.Status401Unauthorized
                });
            }

            // Extract user information from token (this would be implemented in OAuth service)
            var userInfo = await _oauthService.GetUserInfoAsync(request.Token);
            
            if (!userInfo.IsSuccess)
            {
                return Unauthorized(new ProblemDetails
                {
                    Title = "Token Validation Failed",
                    Detail = userInfo.Error,
                    Status = StatusCodes.Status401Unauthorized
                });
            }

            var user = userInfo.Value!;

            // Get user permissions and roles from database (more reliable than token claims)
            var userPermissions = await GetUserPermissionsAsync(user.Id);
            var userRoles = await GetUserRolesAsync(user.Id);

            var response = new TokenValidationResponse
            {
                IsValid = true,
                UserId = user.Id,
                Email = user.Email,
                Name = user.Name,
                Roles = userRoles,
                Permissions = userPermissions,
                Scopes = request.RequiredScopes?.Where(scope => user.Scopes.Contains(scope)).ToArray() ?? Array.Empty<string>(),
                ExpiresAt = user.ExpiresAt
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating token");
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Token Validation Error",
                Detail = "An error occurred while validating the token",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// Get user identity by user ID (for internal API Gateway use)
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>User identity information</returns>
    [HttpGet("user/{userId}")]
    [Authorize(Roles = "api_gateway")]
    [EnableRateLimiting("GatewayPolicy")]
    [ProducesResponseType(typeof(UserIdentityResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUserIdentity(Guid userId)
    {
        try
        {
            // This would be implemented in a user service
            // For now, return a placeholder response
            _logger.LogInformation("User identity requested for user: {UserId}", userId);

            // Get user roles and permissions
            var userRoles = await _roleService.GetUserRolesAsync(userId);
            var userPermissions = await GetUserPermissionsAsync(userId);

            if (!userRoles.IsSuccess)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "User Not Found",
                    Detail = "User identity not found",
                    Status = StatusCodes.Status404NotFound
                });
            }

            var response = new UserIdentityResponse
            {
                UserId = userId,
                Roles = userRoles.Value!.Where(r => r.IsActive && !r.IsExpired).Select(r => r.RoleName).ToArray(),
                Permissions = userPermissions,
                IsActive = true,
                LastLoginAt = DateTime.UtcNow // This would come from user data
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user identity for user: {UserId}", userId);
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "User Identity Error",
                Detail = "An error occurred while retrieving user identity",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// Check if user has specific permission (for API Gateway authorization)
    /// </summary>
    /// <param name="request">Permission check request</param>
    /// <returns>Permission check result</returns>
    [HttpPost("check-permission")]
    [Authorize(Roles = "api_gateway,super_admin")]
    [EnableRateLimiting("GatewayPolicy")]
    [ProducesResponseType(typeof(PermissionCheckResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CheckPermission([FromBody] PermissionCheckRequest request)
    {
        if (request.UserId == Guid.Empty || string.IsNullOrEmpty(request.Resource) || string.IsNullOrEmpty(request.Action))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid Request",
                Detail = "UserId, Resource, and Action are required",
                Status = StatusCodes.Status400BadRequest
            });
        }

        try
        {
            var checkPermissionDto = new Identity.Application.DTOs.CheckPermissionDto
            {
                UserId = request.UserId,
                Resource = request.Resource,
                Action = request.Action,
                Scope = request.Scope
            };

            var result = await _roleService.CheckPermissionAsync(checkPermissionDto);

            if (!result.IsSuccess)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
                {
                    Title = "Permission Check Error",
                    Detail = result.Error,
                    Status = StatusCodes.Status500InternalServerError
                });
            }

            var response = new PermissionCheckResponse
            {
                HasPermission = result.Value!.HasPermission,
                GrantingRoles = result.Value.GrantingRoles,
                Reason = result.Value.Reason
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking permission for user: {UserId}", request.UserId);
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Permission Check Error",
                Detail = "An error occurred while checking permissions",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// Get API Gateway configuration and endpoints
    /// </summary>
    /// <returns>API Gateway configuration</returns>
    [HttpGet("config")]
    [Authorize(Roles = "api_gateway,admin,super_admin")]
    [ProducesResponseType(typeof(GatewayConfigResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public IActionResult GetGatewayConfig()
    {
        var config = new GatewayConfigResponse
        {
            TokenValidationEndpoint = "/api/v1/gateway/validate-token",
            UserIdentityEndpoint = "/api/v1/gateway/user/{userId}",
            PermissionCheckEndpoint = "/api/v1/gateway/check-permission",
            HealthCheckEndpoint = "/health",
            SupportedScopes = new[]
            {
                "openid", "profile", "email", "offline_access",
                "api:read", "api:write", "events:read", "events:write",
                "tickets:read", "tickets:write", "wallet:read", "wallet:write"
            },
            RateLimits = new Dictionary<string, int>
            {
                { "validate-token", 1000 },
                { "user-identity", 500 },
                { "check-permission", 2000 }
            }
        };

        return Ok(config);
    }

    private async Task<string[]> GetUserPermissionsAsync(Guid userId)
    {
        try
        {
            var userRoles = await _roleService.GetUserRolesAsync(userId);
            if (!userRoles.IsSuccess)
            {
                return Array.Empty<string>();
            }

            var permissions = new List<string>();
            foreach (var role in userRoles.Value!.Where(r => r.IsActive && !r.IsExpired))
            {
                var roleDetails = await _roleService.GetRoleAsync(role.RoleName);
                if (roleDetails.IsSuccess)
                {
                    var rolePermissions = roleDetails.Value!.Permissions
                        .Where(p => p.IsActive)
                        .Select(p => $"{p.Resource}:{p.Action}")
                        .ToArray();
                    permissions.AddRange(rolePermissions);
                }
            }

            return permissions.Distinct().ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user permissions for user: {UserId}", userId);
            return Array.Empty<string>();
        }
    }

    private async Task<string[]> GetUserRolesAsync(Guid userId)
    {
        try
        {
            var userRoles = await _roleService.GetUserRolesAsync(userId);
            if (!userRoles.IsSuccess)
            {
                return Array.Empty<string>();
            }

            return userRoles.Value!
                .Where(r => r.IsActive && !r.IsExpired)
                .Select(r => r.RoleName)
                .ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user roles for user: {UserId}", userId);
            return Array.Empty<string>();
        }
    }
}

// Request/Response DTOs
public record TokenValidationRequest
{
    public string Token { get; init; } = string.Empty;
    public string[]? RequiredScopes { get; init; }
}

public record TokenValidationResponse
{
    public bool IsValid { get; init; }
    public Guid UserId { get; init; }
    public string Email { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string[] Roles { get; init; } = Array.Empty<string>();
    public string[] Permissions { get; init; } = Array.Empty<string>();
    public string[] Scopes { get; init; } = Array.Empty<string>();
    public DateTime? ExpiresAt { get; init; }
}

public record UserIdentityResponse
{
    public Guid UserId { get; init; }
    public string[] Roles { get; init; } = Array.Empty<string>();
    public string[] Permissions { get; init; } = Array.Empty<string>();
    public bool IsActive { get; init; }
    public DateTime? LastLoginAt { get; init; }
}

public record PermissionCheckRequest
{
    public Guid UserId { get; init; }
    public string Resource { get; init; } = string.Empty;
    public string Action { get; init; } = string.Empty;
    public string? Scope { get; init; }
}

public record PermissionCheckResponse
{
    public bool HasPermission { get; init; }
    public string[] GrantingRoles { get; init; } = Array.Empty<string>();
    public string? Reason { get; init; }
}

public record GatewayConfigResponse
{
    public string TokenValidationEndpoint { get; init; } = string.Empty;
    public string UserIdentityEndpoint { get; init; } = string.Empty;
    public string PermissionCheckEndpoint { get; init; } = string.Empty;
    public string HealthCheckEndpoint { get; init; } = string.Empty;
    public string[] SupportedScopes { get; init; } = Array.Empty<string>();
    public Dictionary<string, int> RateLimits { get; init; } = new();
}
