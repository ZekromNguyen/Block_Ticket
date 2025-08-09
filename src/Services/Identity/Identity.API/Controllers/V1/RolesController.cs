using Identity.Application.DTOs;
using Identity.Application.Services;
using Identity.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;

namespace Identity.API.Controllers.V1;

/// <summary>
/// Role and permission management endpoints
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/roles")]
[Authorize(Roles = "admin,super_admin")]
[Produces("application/json")]
public class RolesController : ControllerBase
{
    private readonly IRoleService _roleService;
    private readonly ILogger<RolesController> _logger;

    public RolesController(
        IRoleService roleService,
        ILogger<RolesController> logger)
    {
        _roleService = roleService;
        _logger = logger;
    }

    #region Role Management

    /// <summary>
    /// Get all roles
    /// </summary>
    /// <param name="activeOnly">Filter to active roles only</param>
    /// <param name="type">Filter by role type</param>
    /// <returns>List of roles</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<RoleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetRoles([FromQuery] bool activeOnly = false, [FromQuery] RoleType? type = null)
    {
        var result = await _roleService.GetRolesAsync(activeOnly, type);

        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        return BadRequest(new ProblemDetails
        {
            Title = "Failed to retrieve roles",
            Detail = result.Error,
            Status = StatusCodes.Status400BadRequest
        });
    }

    /// <summary>
    /// Get role by name
    /// </summary>
    /// <param name="roleName">Role name</param>
    /// <returns>Role details</returns>
    [HttpGet("{roleName}")]
    [ProducesResponseType(typeof(RoleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRole(string roleName)
    {
        var result = await _roleService.GetRoleAsync(roleName);

        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        if (result.Error.Contains("not found"))
        {
            return NotFound(new ProblemDetails
            {
                Title = "Role Not Found",
                Detail = result.Error,
                Status = StatusCodes.Status404NotFound
            });
        }

        return BadRequest(new ProblemDetails
        {
            Title = "Failed to retrieve role",
            Detail = result.Error,
            Status = StatusCodes.Status400BadRequest
        });
    }

    /// <summary>
    /// Create new role
    /// </summary>
    /// <param name="createRoleDto">Role creation details</param>
    /// <returns>Created role</returns>
    [HttpPost]
    [EnableRateLimiting("AdminPolicy")]
    [ProducesResponseType(typeof(RoleDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateRole([FromBody] CreateRoleDto createRoleDto)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = HttpContext.Request.Headers.UserAgent.ToString();

        var result = await _roleService.CreateRoleAsync(createRoleDto, ipAddress, userAgent);

        if (result.IsSuccess)
        {
            _logger.LogInformation("Role created: {RoleName}", createRoleDto.Name);
            return CreatedAtAction(nameof(GetRole), new { roleName = createRoleDto.Name }, result.Value);
        }

        if (result.Error.Contains("already exists"))
        {
            return Conflict(new ProblemDetails
            {
                Title = "Role Already Exists",
                Detail = result.Error,
                Status = StatusCodes.Status409Conflict
            });
        }

        _logger.LogWarning("Role creation failed: {RoleName}, Error: {Error}", createRoleDto.Name, result.Error);
        return BadRequest(new ProblemDetails
        {
            Title = "Role Creation Failed",
            Detail = result.Error,
            Status = StatusCodes.Status400BadRequest
        });
    }

    /// <summary>
    /// Update role
    /// </summary>
    /// <param name="roleName">Role name</param>
    /// <param name="updateRoleDto">Role update details</param>
    /// <returns>Updated role</returns>
    [HttpPut("{roleName}")]
    [EnableRateLimiting("AdminPolicy")]
    [ProducesResponseType(typeof(RoleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateRole(string roleName, [FromBody] UpdateRoleDto updateRoleDto)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = HttpContext.Request.Headers.UserAgent.ToString();

        var result = await _roleService.UpdateRoleAsync(roleName, updateRoleDto, ipAddress, userAgent);

        if (result.IsSuccess)
        {
            _logger.LogInformation("Role updated: {RoleName}", roleName);
            return Ok(result.Value);
        }

        if (result.Error.Contains("not found"))
        {
            return NotFound(new ProblemDetails
            {
                Title = "Role Not Found",
                Detail = result.Error,
                Status = StatusCodes.Status404NotFound
            });
        }

        _logger.LogWarning("Role update failed: {RoleName}, Error: {Error}", roleName, result.Error);
        return BadRequest(new ProblemDetails
        {
            Title = "Role Update Failed",
            Detail = result.Error,
            Status = StatusCodes.Status400BadRequest
        });
    }

    /// <summary>
    /// Delete role
    /// </summary>
    /// <param name="roleName">Role name</param>
    /// <returns>Deletion confirmation</returns>
    [HttpDelete("{roleName}")]
    [EnableRateLimiting("AdminPolicy")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteRole(string roleName)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = HttpContext.Request.Headers.UserAgent.ToString();

        var result = await _roleService.DeleteRoleAsync(roleName, ipAddress, userAgent);

        if (result.IsSuccess)
        {
            _logger.LogInformation("Role deleted: {RoleName}", roleName);
            return Ok(new { message = "Role deleted successfully" });
        }

        if (result.Error.Contains("not found"))
        {
            return NotFound(new ProblemDetails
            {
                Title = "Role Not Found",
                Detail = result.Error,
                Status = StatusCodes.Status404NotFound
            });
        }

        _logger.LogWarning("Role deletion failed: {RoleName}, Error: {Error}", roleName, result.Error);
        return BadRequest(new ProblemDetails
        {
            Title = "Role Deletion Failed",
            Detail = result.Error,
            Status = StatusCodes.Status400BadRequest
        });
    }

    #endregion

    #region User Role Assignment

    /// <summary>
    /// Get user's roles
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>List of user's roles</returns>
    [HttpGet("users/{userId}")]
    [ProducesResponseType(typeof(IEnumerable<UserRoleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUserRoles(Guid userId)
    {
        var result = await _roleService.GetUserRolesAsync(userId);

        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        if (result.Error.Contains("not found"))
        {
            return NotFound(new ProblemDetails
            {
                Title = "User Not Found",
                Detail = result.Error,
                Status = StatusCodes.Status404NotFound
            });
        }

        return BadRequest(new ProblemDetails
        {
            Title = "Failed to retrieve user roles",
            Detail = result.Error,
            Status = StatusCodes.Status400BadRequest
        });
    }

    /// <summary>
    /// Assign role to user
    /// </summary>
    /// <param name="assignRoleDto">Role assignment details</param>
    /// <returns>Assignment confirmation</returns>
    [HttpPost("assign")]
    [EnableRateLimiting("AdminPolicy")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AssignRole([FromBody] AssignRoleDto assignRoleDto)
    {
        var assignedBy = GetCurrentUserEmail();
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = HttpContext.Request.Headers.UserAgent.ToString();

        var result = await _roleService.AssignRoleAsync(assignRoleDto, assignedBy, ipAddress, userAgent);

        if (result.IsSuccess)
        {
            _logger.LogInformation("Role assigned: User {UserId}, Role {RoleId}", assignRoleDto.UserId, assignRoleDto.RoleId);
            return Ok(new { message = "Role assigned successfully" });
        }

        if (result.Error.Contains("not found"))
        {
            return NotFound(new ProblemDetails
            {
                Title = "User or Role Not Found",
                Detail = result.Error,
                Status = StatusCodes.Status404NotFound
            });
        }

        _logger.LogWarning("Role assignment failed: User {UserId}, Role {RoleId}, Error: {Error}", 
            assignRoleDto.UserId, assignRoleDto.RoleId, result.Error);
        return BadRequest(new ProblemDetails
        {
            Title = "Role Assignment Failed",
            Detail = result.Error,
            Status = StatusCodes.Status400BadRequest
        });
    }

    /// <summary>
    /// Remove role from user
    /// </summary>
    /// <param name="removeRoleDto">Role removal details</param>
    /// <returns>Removal confirmation</returns>
    [HttpPost("remove")]
    [EnableRateLimiting("AdminPolicy")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveRole([FromBody] RemoveRoleDto removeRoleDto)
    {
        var removedBy = GetCurrentUserEmail();
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = HttpContext.Request.Headers.UserAgent.ToString();

        var result = await _roleService.RemoveRoleAsync(removeRoleDto, removedBy, ipAddress, userAgent);

        if (result.IsSuccess)
        {
            _logger.LogInformation("Role removed: User {UserId}, Role {RoleId}", removeRoleDto.UserId, removeRoleDto.RoleId);
            return Ok(new { message = "Role removed successfully" });
        }

        if (result.Error.Contains("not found"))
        {
            return NotFound(new ProblemDetails
            {
                Title = "User or Role Not Found",
                Detail = result.Error,
                Status = StatusCodes.Status404NotFound
            });
        }

        _logger.LogWarning("Role removal failed: User {UserId}, Role {RoleId}, Error: {Error}", 
            removeRoleDto.UserId, removeRoleDto.RoleId, result.Error);
        return BadRequest(new ProblemDetails
        {
            Title = "Role Removal Failed",
            Detail = result.Error,
            Status = StatusCodes.Status400BadRequest
        });
    }

    #endregion

    #region Permission Management

    /// <summary>
    /// Check user permission
    /// </summary>
    /// <param name="checkPermissionDto">Permission check details</param>
    /// <returns>Permission check result</returns>
    [HttpPost("check-permission")]
    [ProducesResponseType(typeof(PermissionCheckResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CheckPermission([FromBody] CheckPermissionDto checkPermissionDto)
    {
        var result = await _roleService.CheckPermissionAsync(checkPermissionDto);

        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        return BadRequest(new ProblemDetails
        {
            Title = "Permission Check Failed",
            Detail = result.Error,
            Status = StatusCodes.Status400BadRequest
        });
    }

    /// <summary>
    /// Get role hierarchy
    /// </summary>
    /// <returns>Roles ordered by priority</returns>
    [HttpGet("hierarchy")]
    [ProducesResponseType(typeof(IEnumerable<RoleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetRoleHierarchy()
    {
        var result = await _roleService.GetRoleHierarchyAsync();

        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        return BadRequest(new ProblemDetails
        {
            Title = "Failed to retrieve role hierarchy",
            Detail = result.Error,
            Status = StatusCodes.Status400BadRequest
        });
    }

    #endregion

    private string? GetCurrentUserEmail()
    {
        return HttpContext.User.FindFirst(ClaimTypes.Email)?.Value;
    }
}
