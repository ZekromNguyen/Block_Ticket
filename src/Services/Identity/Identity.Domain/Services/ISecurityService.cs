using Identity.Domain.Entities;

namespace Identity.Domain.Services;

public interface ISecurityService
{
    // Security Event Management
    Task LogSecurityEventAsync(SecurityEvent securityEvent, CancellationToken cancellationToken = default);
    Task<IEnumerable<SecurityEvent>> GetSecurityEventsAsync(Guid? userId = null, DateTime? from = null, DateTime? to = null, CancellationToken cancellationToken = default);
    Task<IEnumerable<SecurityEvent>> GetUnresolvedSecurityEventsAsync(CancellationToken cancellationToken = default);
    Task ResolveSecurityEventAsync(Guid eventId, string resolvedBy, string resolution, CancellationToken cancellationToken = default);

    // Account Lockout Management
    Task<bool> IsAccountLockedAsync(Guid userId, CancellationToken cancellationToken = default);
    Task LockAccountAsync(Guid userId, string reason, int failedAttempts, string ipAddress, CancellationToken cancellationToken = default);
    Task UnlockAccountAsync(Guid userId, string unlockedBy, CancellationToken cancellationToken = default);
    Task<AccountLockout?> GetAccountLockoutAsync(Guid userId, CancellationToken cancellationToken = default);
    Task CleanupExpiredLockoutsAsync(CancellationToken cancellationToken = default);

    // Suspicious Activity Detection
    Task<double> CalculateRiskScoreAsync(Guid? userId, string ipAddress, string? userAgent, string activityType, CancellationToken cancellationToken = default);
    Task LogSuspiciousActivityAsync(SuspiciousActivity activity, CancellationToken cancellationToken = default);
    Task<IEnumerable<SuspiciousActivity>> GetSuspiciousActivitiesAsync(Guid? userId = null, DateTime? from = null, DateTime? to = null, CancellationToken cancellationToken = default);
    Task ResolveSuspiciousActivityAsync(Guid activityId, string resolution, string resolvedBy, CancellationToken cancellationToken = default);

    // Rate Limiting and Throttling
    Task<bool> IsRateLimitExceededAsync(string key, int limit, TimeSpan window, CancellationToken cancellationToken = default);
    Task IncrementRateLimitCounterAsync(string key, TimeSpan window, CancellationToken cancellationToken = default);
    Task<int> GetRateLimitCountAsync(string key, CancellationToken cancellationToken = default);

    // IP and Location Analysis
    Task<bool> IsIpAddressSuspiciousAsync(string ipAddress, CancellationToken cancellationToken = default);
    Task<string?> GetLocationFromIpAsync(string ipAddress, CancellationToken cancellationToken = default);
    Task<bool> IsLocationUnusualForUserAsync(Guid userId, string location, CancellationToken cancellationToken = default);
    Task BlockIpAddressAsync(string ipAddress, string reason, TimeSpan? duration = null, CancellationToken cancellationToken = default);

    // Device Fingerprinting
    Task<string> GenerateDeviceFingerprintAsync(string userAgent, string? additionalData = null);
    Task<bool> IsDeviceKnownAsync(Guid userId, string deviceFingerprint, CancellationToken cancellationToken = default);
    Task RegisterDeviceAsync(Guid userId, string deviceFingerprint, string? deviceName = null, CancellationToken cancellationToken = default);

    // Threat Intelligence
    Task<bool> IsIpAddressInThreatListAsync(string ipAddress, CancellationToken cancellationToken = default);
    Task<bool> IsUserAgentSuspiciousAsync(string userAgent, CancellationToken cancellationToken = default);
    Task UpdateThreatIntelligenceAsync(CancellationToken cancellationToken = default);
}

public interface IRiskAnalysisService
{
    Task<RiskAssessment> AssessLoginRiskAsync(Guid? userId, string ipAddress, string? userAgent, string? location, CancellationToken cancellationToken = default);
    Task<RiskAssessment> AssessPasswordChangeRiskAsync(Guid userId, string ipAddress, string? userAgent, CancellationToken cancellationToken = default);
    Task<RiskAssessment> AssessMfaSetupRiskAsync(Guid userId, string ipAddress, string? userAgent, CancellationToken cancellationToken = default);
    Task<bool> ShouldRequireAdditionalVerificationAsync(RiskAssessment riskAssessment, CancellationToken cancellationToken = default);
}

public class RiskAssessment
{
    public double Score { get; set; }
    public RiskLevel Level { get; set; }
    public List<RiskFactor> Factors { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
    public DateTime AssessedAt { get; set; } = DateTime.UtcNow;
}

public class RiskFactor
{
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public double Weight { get; set; }
    public double Score { get; set; }
}

public enum RiskLevel
{
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4
}

public static class RiskFactorTypes
{
    public const string UnknownDevice = "UNKNOWN_DEVICE";
    public const string UnusualLocation = "UNUSUAL_LOCATION";
    public const string SuspiciousIp = "SUSPICIOUS_IP";
    public const string HighVelocity = "HIGH_VELOCITY";
    public const string TimeOfDay = "TIME_OF_DAY";
    public const string MultipleFailedAttempts = "MULTIPLE_FAILED_ATTEMPTS";
    public const string ThreatIntelligence = "THREAT_INTELLIGENCE";
    public const string BehaviorAnomaly = "BEHAVIOR_ANOMALY";
}
