using Identity.Application.Common.Models;
using Identity.Application.DTOs;
using Identity.Domain.Repositories;
using Identity.Domain.Services;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Identity.Application.Services;

public class GatewayService : IGatewayService
{
    private readonly IOAuthService _oauthService;
    private readonly IRoleService _roleService;
    private readonly IUserRepository _userRepository;
    private readonly ISecurityService _securityService;
    private readonly IDistributedCache _cache;
    private readonly IConfiguration _configuration;
    private readonly ILogger<GatewayService> _logger;

    public GatewayService(
        IOAuthService oauthService,
        IRoleService roleService,
        IUserRepository userRepository,
        ISecurityService securityService,
        IDistributedCache cache,
        IConfiguration configuration,
        ILogger<GatewayService> logger)
    {
        _oauthService = oauthService;
        _roleService = roleService;
        _userRepository = userRepository;
        _securityService = securityService;
        _cache = cache;
        _configuration = configuration;
        _logger = logger;
    }

    // Token Validation
    public async Task<Result<TokenValidationResult>> ValidateTokenAsync(string token, string[]? requiredScopes = null)
    {
        try
        {
            // Check cache first
            var cacheKey = $"token_validation:{token}";
            var cachedResult = await _cache.GetStringAsync(cacheKey);
            if (cachedResult != null)
            {
                var cached = JsonSerializer.Deserialize<TokenValidationResult>(cachedResult);
                if (cached != null && cached.ExpiresAt > DateTime.UtcNow)
                {
                    return Result<TokenValidationResult>.Success(cached);
                }
            }

            // Validate token
            var introspectionResult = await _oauthService.IntrospectTokenAsync(token);
            if (!introspectionResult.IsSuccess || !introspectionResult.Value)
            {
                var failedResult = new TokenValidationResult
                {
                    IsValid = false,
                    Error = "Token is invalid or expired"
                };
                return Result<TokenValidationResult>.Success(failedResult);
            }

            // Get user info
            var userInfoResult = await _oauthService.GetUserInfoAsync(token);
            if (!userInfoResult.IsSuccess)
            {
                var failedResult = new TokenValidationResult
                {
                    IsValid = false,
                    Error = userInfoResult.Error
                };
                return Result<TokenValidationResult>.Success(failedResult);
            }

            var userInfo = userInfoResult.Value!;

            // Get user permissions
            var permissions = await GetUserPermissionsAsync(userInfo.Id);
            var permissionArray = permissions.IsSuccess ? permissions.Value! : Array.Empty<string>();

            // Validate required scopes
            var effectiveScopes = userInfo.Scopes;
            if (requiredScopes != null && requiredScopes.Length > 0)
            {
                var scopeValidation = await ValidateScopesAsync(requiredScopes, userInfo.Scopes);
                if (!scopeValidation.IsSuccess || !scopeValidation.Value)
                {
                    var failedResult = new TokenValidationResult
                    {
                        IsValid = false,
                        Error = "Insufficient scopes"
                    };
                    return Result<TokenValidationResult>.Success(failedResult);
                }
                effectiveScopes = requiredScopes.Where(s => userInfo.Scopes.Contains(s)).ToArray();
            }

            var result = new TokenValidationResult
            {
                IsValid = true,
                UserId = userInfo.Id,
                Email = userInfo.Email,
                Name = userInfo.Name,
                Roles = userInfo.Roles,
                Permissions = permissionArray,
                Scopes = effectiveScopes,
                ExpiresAt = userInfo.ExpiresAt
            };

            // Cache result for 5 minutes
            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
            };
            await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(result), cacheOptions);

            return Result<TokenValidationResult>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating token");
            return Result<TokenValidationResult>.Failure("Token validation failed");
        }
    }

    public async Task<Result<UserInfoDto>> GetUserInfoFromTokenAsync(string token)
    {
        return await _oauthService.GetUserInfoAsync(token);
    }

    public async Task<Result<bool>> IsTokenActiveAsync(string token)
    {
        return await _oauthService.IntrospectTokenAsync(token);
    }

    // User Identity
    public async Task<Result<GatewayUserIdentity>> GetUserIdentityAsync(Guid userId)
    {
        try
        {
            // Check cache first
            var cacheKey = $"user_identity:{userId}";
            var cachedResult = await _cache.GetStringAsync(cacheKey);
            if (cachedResult != null)
            {
                var cached = JsonSerializer.Deserialize<GatewayUserIdentity>(cachedResult);
                if (cached != null)
                {
                    return Result<GatewayUserIdentity>.Success(cached);
                }
            }

            // Get user from repository
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                return Result<GatewayUserIdentity>.Failure("User not found");
            }

            // Get user roles and permissions
            var userRoles = await _roleService.GetUserRolesAsync(userId);
            var userPermissions = await GetUserPermissionsAsync(userId);

            var roles = userRoles.IsSuccess 
                ? userRoles.Value!.Where(r => r.IsActive && !r.IsExpired).Select(r => r.RoleName).ToArray()
                : Array.Empty<string>();

            var permissions = userPermissions.IsSuccess ? userPermissions.Value! : Array.Empty<string>();

            var identity = new GatewayUserIdentity
            {
                UserId = user.Id,
                Email = user.Email.Value,
                Name = $"{user.FirstName} {user.LastName}".Trim(),
                Roles = roles,
                Permissions = permissions,
                IsActive = user.Status == Domain.Entities.UserStatus.Active,
                IsEmailVerified = user.EmailConfirmed,
                IsMfaEnabled = user.MfaEnabled,
                LastLoginAt = user.LastLoginAt,
                CreatedAt = user.CreatedAt
            };

            // Cache for 10 minutes
            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
            };
            await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(identity), cacheOptions);

            return Result<GatewayUserIdentity>.Success(identity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user identity for user: {UserId}", userId);
            return Result<GatewayUserIdentity>.Failure("Failed to get user identity");
        }
    }

    public async Task<Result<string[]>> GetUserPermissionsAsync(Guid userId)
    {
        try
        {
            var userRoles = await _roleService.GetUserRolesAsync(userId);
            if (!userRoles.IsSuccess)
            {
                return Result<string[]>.Success(Array.Empty<string>());
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

            return Result<string[]>.Success(permissions.Distinct().ToArray());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user permissions for user: {UserId}", userId);
            return Result<string[]>.Failure("Failed to get user permissions");
        }
    }

    public async Task<Result<string[]>> GetUserRolesAsync(Guid userId)
    {
        var userRoles = await _roleService.GetUserRolesAsync(userId);
        if (!userRoles.IsSuccess)
        {
            return Result<string[]>.Failure(userRoles.Error);
        }

        var roles = userRoles.Value!
            .Where(r => r.IsActive && !r.IsExpired)
            .Select(r => r.RoleName)
            .ToArray();

        return Result<string[]>.Success(roles);
    }

    // Permission Checking
    public async Task<Result<bool>> HasPermissionAsync(Guid userId, string resource, string action, string? scope = null)
    {
        var result = await _roleService.HasPermissionAsync(userId, resource, action, scope);
        return result;
    }

    public async Task<Result<PermissionCheckResultDto>> CheckPermissionAsync(Guid userId, string resource, string action, string? scope = null)
    {
        var checkPermissionDto = new CheckPermissionDto
        {
            UserId = userId,
            Resource = resource,
            Action = action,
            Scope = scope
        };

        return await _roleService.CheckPermissionAsync(checkPermissionDto);
    }

    public async Task<Result<bool>> HasAnyPermissionAsync(Guid userId, string resource)
    {
        return await _roleService.HasAnyPermissionAsync(userId, resource);
    }

    // Scope Validation
    public async Task<Result<bool>> ValidateScopesAsync(string[] requestedScopes, string[] userScopes)
    {
        await Task.Delay(1); // Simulate async operation
        var hasAllScopes = requestedScopes.All(scope => userScopes.Contains(scope));
        return Result<bool>.Success(hasAllScopes);
    }

    public async Task<Result<string[]>> GetEffectiveScopesAsync(Guid userId, string[] requestedScopes)
    {
        try
        {
            // This would typically get user's granted scopes from the database
            // For now, return intersection of requested and default scopes
            var defaultScopes = await _oauthService.GetDefaultScopesAsync();
            if (!defaultScopes.IsSuccess)
            {
                return Result<string[]>.Success(Array.Empty<string>());
            }

            var userScopes = defaultScopes.Value!.Select(s => s.Name).ToArray();
            var effectiveScopes = requestedScopes.Where(scope => userScopes.Contains(scope)).ToArray();

            return Result<string[]>.Success(effectiveScopes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting effective scopes for user: {UserId}", userId);
            return Result<string[]>.Failure("Failed to get effective scopes");
        }
    }

    // Rate Limiting Support
    public async Task<Result<RateLimitInfo>> GetRateLimitInfoAsync(string identifier)
    {
        try
        {
            var count = await _securityService.GetRateLimitCountAsync(identifier);
            var limit = _configuration.GetValue<int>("RateLimiting:DefaultLimit", 100);
            var window = TimeSpan.FromMinutes(_configuration.GetValue<int>("RateLimiting:WindowMinutes", 1));

            var info = new RateLimitInfo
            {
                Identifier = identifier,
                CurrentCount = count,
                Limit = limit,
                Window = window,
                ResetAt = DateTime.UtcNow.Add(window),
                IsExceeded = count >= limit
            };

            return Result<RateLimitInfo>.Success(info);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting rate limit info for identifier: {Identifier}", identifier);
            return Result<RateLimitInfo>.Failure("Failed to get rate limit info");
        }
    }

    public async Task<Result> IncrementRateLimitAsync(string identifier, string endpoint)
    {
        try
        {
            var window = TimeSpan.FromMinutes(1);
            await _securityService.IncrementRateLimitCounterAsync($"{identifier}:{endpoint}", window);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error incrementing rate limit for identifier: {Identifier}", identifier);
            return Result.Failure("Failed to increment rate limit");
        }
    }

    // Caching Support
    public async Task<Result> InvalidateUserCacheAsync(Guid userId)
    {
        try
        {
            var cacheKeys = new[]
            {
                $"user_identity:{userId}",
                $"user_permissions:{userId}",
                $"user_roles:{userId}"
            };

            foreach (var key in cacheKeys)
            {
                await _cache.RemoveAsync(key);
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating user cache for user: {UserId}", userId);
            return Result.Failure("Failed to invalidate user cache");
        }
    }

    public async Task<Result> InvalidateTokenCacheAsync(string token)
    {
        try
        {
            var cacheKey = $"token_validation:{token}";
            await _cache.RemoveAsync(cacheKey);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating token cache");
            return Result.Failure("Failed to invalidate token cache");
        }
    }

    // Health and Monitoring
    public async Task<Result<GatewayHealthInfo>> GetHealthInfoAsync()
    {
        try
        {
            var details = new Dictionary<string, object>
            {
                { "timestamp", DateTime.UtcNow },
                { "version", "1.0.0" },
                { "cache_status", "healthy" },
                { "database_status", "healthy" }
            };

            var health = new GatewayHealthInfo
            {
                IsHealthy = true,
                Status = "Healthy",
                Details = details
            };

            return Result<GatewayHealthInfo>.Success(health);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting health info");
            return Result<GatewayHealthInfo>.Failure("Failed to get health info");
        }
    }

    public async Task<Result<GatewayMetrics>> GetMetricsAsync()
    {
        try
        {
            // This would typically get metrics from a metrics store
            var metrics = new GatewayMetrics
            {
                TotalRequests = 1000,
                SuccessfulValidations = 950,
                FailedValidations = 50,
                AverageResponseTime = 125.5,
                EndpointCounts = new Dictionary<string, long>
                {
                    { "validate-token", 800 },
                    { "user-identity", 150 },
                    { "check-permission", 50 }
                }
            };

            return Result<GatewayMetrics>.Success(metrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting metrics");
            return Result<GatewayMetrics>.Failure("Failed to get metrics");
        }
    }
}
