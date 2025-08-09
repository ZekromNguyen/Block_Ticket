using Identity.Domain.Repositories;
using Identity.Domain.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Identity.Infrastructure.Services;

public class RiskAnalysisService : IRiskAnalysisService
{
    private readonly ISecurityService _securityService;
    private readonly ISecurityEventRepository _securityEventRepository;
    private readonly IConfiguration _configuration;
    private readonly ILogger<RiskAnalysisService> _logger;

    public RiskAnalysisService(
        ISecurityService securityService,
        ISecurityEventRepository securityEventRepository,
        IConfiguration configuration,
        ILogger<RiskAnalysisService> logger)
    {
        _securityService = securityService;
        _securityEventRepository = securityEventRepository;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<RiskAssessment> AssessLoginRiskAsync(Guid? userId, string ipAddress, string? userAgent, string? location, CancellationToken cancellationToken = default)
    {
        var assessment = new RiskAssessment();
        var factors = new List<RiskFactor>();

        try
        {
            // Check IP reputation
            if (await _securityService.IsIpAddressSuspiciousAsync(ipAddress, cancellationToken))
            {
                factors.Add(new RiskFactor
                {
                    Type = RiskFactorTypes.SuspiciousIp,
                    Description = "IP address is flagged as suspicious",
                    Weight = 0.3,
                    Score = 30.0
                });
            }

            // Check for unknown device
            if (userId.HasValue && !string.IsNullOrEmpty(userAgent))
            {
                var deviceFingerprint = await _securityService.GenerateDeviceFingerprintAsync(userAgent);
                if (!await _securityService.IsDeviceKnownAsync(userId.Value, deviceFingerprint, cancellationToken))
                {
                    factors.Add(new RiskFactor
                    {
                        Type = RiskFactorTypes.UnknownDevice,
                        Description = "Login from unknown device",
                        Weight = 0.25,
                        Score = 25.0
                    });
                }
            }

            // Check for unusual location
            if (userId.HasValue && !string.IsNullOrEmpty(location))
            {
                if (await _securityService.IsLocationUnusualForUserAsync(userId.Value, location, cancellationToken))
                {
                    factors.Add(new RiskFactor
                    {
                        Type = RiskFactorTypes.UnusualLocation,
                        Description = "Login from unusual location",
                        Weight = 0.2,
                        Score = 20.0
                    });
                }
            }

            // Check recent failed attempts
            if (userId.HasValue)
            {
                var recentFailures = await GetRecentFailedAttemptsAsync(userId.Value, TimeSpan.FromHours(1), cancellationToken);
                if (recentFailures > 0)
                {
                    var score = Math.Min(recentFailures * 5.0, 20.0);
                    factors.Add(new RiskFactor
                    {
                        Type = RiskFactorTypes.MultipleFailedAttempts,
                        Description = $"{recentFailures} failed login attempts in the last hour",
                        Weight = 0.15,
                        Score = score
                    });
                }
            }

            // Check time of day
            var hour = DateTime.UtcNow.Hour;
            if (hour < 6 || hour > 22) // Outside normal business hours
            {
                factors.Add(new RiskFactor
                {
                    Type = RiskFactorTypes.TimeOfDay,
                    Description = "Login outside normal business hours",
                    Weight = 0.1,
                    Score = 10.0
                });
            }

            // Check login velocity
            if (userId.HasValue)
            {
                var recentLogins = await GetRecentLoginsAsync(userId.Value, TimeSpan.FromMinutes(5), cancellationToken);
                if (recentLogins > 3)
                {
                    factors.Add(new RiskFactor
                    {
                        Type = RiskFactorTypes.HighVelocity,
                        Description = "High login velocity detected",
                        Weight = 0.2,
                        Score = 25.0
                    });
                }
            }

            // Calculate overall risk score
            assessment.Score = factors.Sum(f => f.Score * f.Weight);
            assessment.Level = DetermineRiskLevel(assessment.Score);
            assessment.Factors = factors;
            assessment.Recommendations = GenerateRecommendations(assessment);

            _logger.LogDebug("Login risk assessment completed: Score={Score}, Level={Level}", 
                assessment.Score, assessment.Level);

            return assessment;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assessing login risk for user {UserId}", userId);
            
            // Return high risk assessment on error
            return new RiskAssessment
            {
                Score = 75.0,
                Level = RiskLevel.High,
                Factors = new List<RiskFactor>
                {
                    new RiskFactor
                    {
                        Type = "ASSESSMENT_ERROR",
                        Description = "Error occurred during risk assessment",
                        Weight = 1.0,
                        Score = 75.0
                    }
                },
                Recommendations = new List<string> { "Require additional verification due to assessment error" }
            };
        }
    }

    public async Task<RiskAssessment> AssessPasswordChangeRiskAsync(Guid userId, string ipAddress, string? userAgent, CancellationToken cancellationToken = default)
    {
        var assessment = new RiskAssessment();
        var factors = new List<RiskFactor>();

        try
        {
            // Check if IP is suspicious
            if (await _securityService.IsIpAddressSuspiciousAsync(ipAddress, cancellationToken))
            {
                factors.Add(new RiskFactor
                {
                    Type = RiskFactorTypes.SuspiciousIp,
                    Description = "Password change from suspicious IP",
                    Weight = 0.4,
                    Score = 40.0
                });
            }

            // Check for unknown device
            if (!string.IsNullOrEmpty(userAgent))
            {
                var deviceFingerprint = await _securityService.GenerateDeviceFingerprintAsync(userAgent);
                if (!await _securityService.IsDeviceKnownAsync(userId, deviceFingerprint, cancellationToken))
                {
                    factors.Add(new RiskFactor
                    {
                        Type = RiskFactorTypes.UnknownDevice,
                        Description = "Password change from unknown device",
                        Weight = 0.3,
                        Score = 30.0
                    });
                }
            }

            // Check recent security events
            var recentEvents = await _securityEventRepository.GetEventsAsync(userId, DateTime.UtcNow.AddHours(-24), null, cancellationToken);
            var suspiciousEvents = recentEvents.Where(e => e.Severity >= Domain.Entities.SecurityEventSeverity.High).Count();
            
            if (suspiciousEvents > 0)
            {
                factors.Add(new RiskFactor
                {
                    Type = "RECENT_SECURITY_EVENTS",
                    Description = $"{suspiciousEvents} high-severity security events in the last 24 hours",
                    Weight = 0.3,
                    Score = Math.Min(suspiciousEvents * 15.0, 30.0)
                });
            }

            assessment.Score = factors.Sum(f => f.Score * f.Weight);
            assessment.Level = DetermineRiskLevel(assessment.Score);
            assessment.Factors = factors;
            assessment.Recommendations = GenerateRecommendations(assessment);

            return assessment;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assessing password change risk for user {UserId}", userId);
            return CreateErrorAssessment();
        }
    }

    public async Task<RiskAssessment> AssessMfaSetupRiskAsync(Guid userId, string ipAddress, string? userAgent, CancellationToken cancellationToken = default)
    {
        var assessment = new RiskAssessment();
        var factors = new List<RiskFactor>();

        try
        {
            // MFA setup is generally lower risk, but still check for suspicious indicators
            if (await _securityService.IsIpAddressSuspiciousAsync(ipAddress, cancellationToken))
            {
                factors.Add(new RiskFactor
                {
                    Type = RiskFactorTypes.SuspiciousIp,
                    Description = "MFA setup from suspicious IP",
                    Weight = 0.3,
                    Score = 30.0
                });
            }

            // Check for recent failed login attempts (might indicate compromise)
            var recentFailures = await GetRecentFailedAttemptsAsync(userId, TimeSpan.FromHours(1), cancellationToken);
            if (recentFailures > 5)
            {
                factors.Add(new RiskFactor
                {
                    Type = RiskFactorTypes.MultipleFailedAttempts,
                    Description = "MFA setup after multiple failed login attempts",
                    Weight = 0.4,
                    Score = 40.0
                });
            }

            assessment.Score = factors.Sum(f => f.Score * f.Weight);
            assessment.Level = DetermineRiskLevel(assessment.Score);
            assessment.Factors = factors;
            assessment.Recommendations = GenerateRecommendations(assessment);

            return assessment;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assessing MFA setup risk for user {UserId}", userId);
            return CreateErrorAssessment();
        }
    }

    public async Task<bool> ShouldRequireAdditionalVerificationAsync(RiskAssessment riskAssessment, CancellationToken cancellationToken = default)
    {
        await Task.Delay(1, cancellationToken); // Simulate async operation
        
        var threshold = _configuration.GetValue<double>("Security:AdditionalVerificationThreshold", 50.0);
        return riskAssessment.Score >= threshold || riskAssessment.Level >= RiskLevel.High;
    }

    private static RiskLevel DetermineRiskLevel(double score)
    {
        return score switch
        {
            >= 75.0 => RiskLevel.Critical,
            >= 50.0 => RiskLevel.High,
            >= 25.0 => RiskLevel.Medium,
            _ => RiskLevel.Low
        };
    }

    private static List<string> GenerateRecommendations(RiskAssessment assessment)
    {
        var recommendations = new List<string>();

        if (assessment.Level >= RiskLevel.High)
        {
            recommendations.Add("Require multi-factor authentication");
            recommendations.Add("Send security alert to user");
        }

        if (assessment.Level >= RiskLevel.Medium)
        {
            recommendations.Add("Log detailed security event");
            recommendations.Add("Monitor user activity closely");
        }

        if (assessment.Factors.Any(f => f.Type == RiskFactorTypes.UnknownDevice))
        {
            recommendations.Add("Send device verification email");
        }

        if (assessment.Factors.Any(f => f.Type == RiskFactorTypes.UnusualLocation))
        {
            recommendations.Add("Send location verification email");
        }

        if (assessment.Factors.Any(f => f.Type == RiskFactorTypes.SuspiciousIp))
        {
            recommendations.Add("Consider blocking IP address");
        }

        return recommendations;
    }

    private static RiskAssessment CreateErrorAssessment()
    {
        return new RiskAssessment
        {
            Score = 75.0,
            Level = RiskLevel.High,
            Factors = new List<RiskFactor>
            {
                new RiskFactor
                {
                    Type = "ASSESSMENT_ERROR",
                    Description = "Error occurred during risk assessment",
                    Weight = 1.0,
                    Score = 75.0
                }
            },
            Recommendations = new List<string> { "Require additional verification due to assessment error" }
        };
    }

    private async Task<int> GetRecentFailedAttemptsAsync(Guid userId, TimeSpan timeWindow, CancellationToken cancellationToken)
    {
        var from = DateTime.UtcNow.Subtract(timeWindow);
        return await _securityEventRepository.GetEventCountAsync(userId, Domain.Entities.SecurityEventTypes.LoginFailure, from, null, cancellationToken);
    }

    private async Task<int> GetRecentLoginsAsync(Guid userId, TimeSpan timeWindow, CancellationToken cancellationToken)
    {
        var from = DateTime.UtcNow.Subtract(timeWindow);
        return await _securityEventRepository.GetEventCountAsync(userId, Domain.Entities.SecurityEventTypes.LoginSuccess, from, null, cancellationToken);
    }
}
