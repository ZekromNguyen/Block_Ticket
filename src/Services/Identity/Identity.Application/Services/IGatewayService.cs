using Identity.Application.Common.Models;
using Identity.Application.DTOs;

namespace Identity.Application.Services;

public interface IGatewayService
{
    // Token Validation
    Task<Result<TokenValidationResult>> ValidateTokenAsync(string token, string[]? requiredScopes = null);
    Task<Result<UserInfoDto>> GetUserInfoFromTokenAsync(string token);
    Task<Result<bool>> IsTokenActiveAsync(string token);

    // User Identity
    Task<Result<GatewayUserIdentity>> GetUserIdentityAsync(Guid userId);
    Task<Result<string[]>> GetUserPermissionsAsync(Guid userId);
    Task<Result<string[]>> GetUserRolesAsync(Guid userId);

    // Permission Checking
    Task<Result<bool>> HasPermissionAsync(Guid userId, string resource, string action, string? scope = null);
    Task<Result<PermissionCheckResultDto>> CheckPermissionAsync(Guid userId, string resource, string action, string? scope = null);
    Task<Result<bool>> HasAnyPermissionAsync(Guid userId, string resource);

    // Scope Validation
    Task<Result<bool>> ValidateScopesAsync(string[] requestedScopes, string[] userScopes);
    Task<Result<string[]>> GetEffectiveScopesAsync(Guid userId, string[] requestedScopes);

    // Rate Limiting Support
    Task<Result<RateLimitInfo>> GetRateLimitInfoAsync(string identifier);
    Task<Result> IncrementRateLimitAsync(string identifier, string endpoint);

    // Caching Support
    Task<Result> InvalidateUserCacheAsync(Guid userId);
    Task<Result> InvalidateTokenCacheAsync(string token);

    // Health and Monitoring
    Task<Result<GatewayHealthInfo>> GetHealthInfoAsync();
    Task<Result<GatewayMetrics>> GetMetricsAsync();
}

public record TokenValidationResult
{
    public bool IsValid { get; init; }
    public Guid? UserId { get; init; }
    public string? Email { get; init; }
    public string? Name { get; init; }
    public string[] Roles { get; init; } = Array.Empty<string>();
    public string[] Permissions { get; init; } = Array.Empty<string>();
    public string[] Scopes { get; init; } = Array.Empty<string>();
    public DateTime? ExpiresAt { get; init; }
    public string? Error { get; init; }
}

public record GatewayUserIdentity
{
    public Guid UserId { get; init; }
    public string Email { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string[] Roles { get; init; } = Array.Empty<string>();
    public string[] Permissions { get; init; } = Array.Empty<string>();
    public bool IsActive { get; init; }
    public bool IsEmailVerified { get; init; }
    public bool IsMfaEnabled { get; init; }
    public DateTime? LastLoginAt { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record RateLimitInfo
{
    public string Identifier { get; init; } = string.Empty;
    public int CurrentCount { get; init; }
    public int Limit { get; init; }
    public TimeSpan Window { get; init; }
    public DateTime? ResetAt { get; init; }
    public bool IsExceeded { get; init; }
}

public record GatewayHealthInfo
{
    public bool IsHealthy { get; init; }
    public string Status { get; init; } = string.Empty;
    public Dictionary<string, object> Details { get; init; } = new();
    public DateTime CheckedAt { get; init; } = DateTime.UtcNow;
}

public record GatewayMetrics
{
    public long TotalRequests { get; init; }
    public long SuccessfulValidations { get; init; }
    public long FailedValidations { get; init; }
    public double AverageResponseTime { get; init; }
    public Dictionary<string, long> EndpointCounts { get; init; } = new();
    public DateTime CollectedAt { get; init; } = DateTime.UtcNow;
}
