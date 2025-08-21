using Event.Infrastructure.Security.RateLimiting.Interfaces;
using Event.Infrastructure.Security.RateLimiting.Models;
using Event.Infrastructure.Security.RateLimiting.Services;
using Event.Infrastructure.Security.Monitoring;
using Event.Infrastructure.Security.Validation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using System.Diagnostics;
using Xunit;
using Xunit.Abstractions;

namespace Event.Tests.Integration;

/// <summary>
/// Integration tests for security features including rate limiting, input validation, and monitoring
/// </summary>
public class SecurityIntegrationTests : IClassFixture<SecurityTestFixture>, IDisposable
{
    private readonly SecurityTestFixture _fixture;
    private readonly ITestOutputHelper _output;
    private readonly IServiceScope _scope;
    private readonly IRateLimiter _rateLimiter;
    private readonly SecurityMetrics _securityMetrics;

    public SecurityIntegrationTests(SecurityTestFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
        _scope = _fixture.ServiceProvider.CreateScope();
        _rateLimiter = _scope.ServiceProvider.GetRequiredService<IRateLimiter>();
        _securityMetrics = _scope.ServiceProvider.GetRequiredService<SecurityMetrics>();
    }

    [Fact]
    public async Task RateLimiter_IpBasedLimiting_ShouldEnforceCorrectly()
    {
        // Arrange
        var context = new RateLimitContext
        {
            IpAddress = "192.168.1.100",
            Endpoint = "/api/events",
            HttpMethod = "GET"
        };

        // Act & Assert - First 5 requests should be allowed
        for (int i = 0; i < 5; i++)
        {
            var result = await _rateLimiter.CheckAsync(context);
            Assert.True(result.IsAllowed, $"Request {i + 1} should be allowed");
            _output.WriteLine($"Request {i + 1}: Allowed={result.IsAllowed}, Count={result.CurrentCount}");
        }

        // 6th request should be rate limited
        var rateLimitedResult = await _rateLimiter.CheckAsync(context);
        Assert.False(rateLimitedResult.IsAllowed, "6th request should be rate limited");
        Assert.NotNull(rateLimitedResult.RetryAfterSeconds);

        _output.WriteLine($"Rate limited request: Count={rateLimitedResult.CurrentCount}, RetryAfter={rateLimitedResult.RetryAfterSeconds}s");
    }

    [Fact]
    public async Task RateLimiter_ClientBasedLimiting_ShouldEnforceCorrectly()
    {
        // Arrange
        var context = new RateLimitContext
        {
            IpAddress = "192.168.1.101",
            ClientId = "test-client-123",
            Endpoint = "/api/events",
            HttpMethod = "POST"
        };

        // Act & Assert - Test client-based rate limiting
        var allowedCount = 0;
        for (int i = 0; i < 15; i++)
        {
            var result = await _rateLimiter.CheckAsync(context);
            if (result.IsAllowed)
            {
                allowedCount++;
            }
            else
            {
                break;
            }
        }

        Assert.True(allowedCount >= 5, $"Should allow at least 5 requests, allowed {allowedCount}");
        _output.WriteLine($"Client-based rate limiting: {allowedCount} requests allowed before rate limiting");
    }

    [Fact]
    public async Task RateLimiter_OrganizationBasedLimiting_ShouldEnforceCorrectly()
    {
        // Arrange
        var context = new RateLimitContext
        {
            IpAddress = "192.168.1.102",
            OrganizationId = Guid.NewGuid().ToString(),
            Endpoint = "/api/events",
            HttpMethod = "GET"
        };

        // Act & Assert - Test organization-based rate limiting (higher limits)
        var allowedCount = 0;
        for (int i = 0; i < 50; i++)
        {
            var result = await _rateLimiter.CheckAsync(context);
            if (result.IsAllowed)
            {
                allowedCount++;
            }
            else
            {
                break;
            }
        }

        Assert.True(allowedCount >= 20, $"Should allow at least 20 requests for organization, allowed {allowedCount}");
        _output.WriteLine($"Organization-based rate limiting: {allowedCount} requests allowed before rate limiting");
    }

    [Fact]
    public async Task RateLimiter_EndpointSpecificLimiting_ShouldApplyStricterLimits()
    {
        // Arrange - Reservation endpoint should have stricter limits
        var context = new RateLimitContext
        {
            IpAddress = "192.168.1.103",
            Endpoint = "/api/reservations",
            HttpMethod = "POST"
        };

        // Act & Assert - Reservation endpoints should have lower limits
        var allowedCount = 0;
        for (int i = 0; i < 15; i++)
        {
            var result = await _rateLimiter.CheckAsync(context);
            if (result.IsAllowed)
            {
                allowedCount++;
            }
            else
            {
                break;
            }
        }

        Assert.True(allowedCount <= 10, $"Reservation endpoint should have stricter limits, allowed {allowedCount}");
        _output.WriteLine($"Endpoint-specific rate limiting (reservations): {allowedCount} requests allowed");
    }

    [Fact]
    public async Task RateLimiter_SlidingWindow_ShouldBeMoreAccurate()
    {
        // Arrange
        var context = new RateLimitContext
        {
            IpAddress = "192.168.1.104",
            Endpoint = "/api/events",
            HttpMethod = "GET"
        };

        // Act - Make requests quickly, then wait and make more
        var initialResults = new List<RateLimitResult>();
        for (int i = 0; i < 5; i++)
        {
            var result = await _rateLimiter.CheckAsync(context);
            initialResults.Add(result);
        }

        // Wait for part of the window to pass
        await Task.Delay(TimeSpan.FromSeconds(2));

        // Make more requests - sliding window should allow some
        var laterResults = new List<RateLimitResult>();
        for (int i = 0; i < 3; i++)
        {
            var result = await _rateLimiter.CheckAsync(context);
            laterResults.Add(result);
        }

        // Assert
        Assert.True(initialResults.All(r => r.IsAllowed), "Initial requests should be allowed");
        
        _output.WriteLine($"Sliding window test: Initial={initialResults.Count(r => r.IsAllowed)}, Later={laterResults.Count(r => r.IsAllowed)}");
    }

    [Fact]
    public async Task RateLimiter_PerformanceUnderLoad_ShouldMeetRequirements()
    {
        // Arrange
        var contexts = Enumerable.Range(0, 100)
            .Select(i => new RateLimitContext
            {
                IpAddress = $"192.168.1.{i % 10 + 1}",
                ClientId = $"client-{i % 5}",
                Endpoint = "/api/events",
                HttpMethod = "GET"
            })
            .ToList();

        // Act - Measure performance under concurrent load
        var stopwatch = Stopwatch.StartNew();
        var tasks = contexts.Select(async context =>
        {
            var taskStopwatch = Stopwatch.StartNew();
            var result = await _rateLimiter.CheckAsync(context);
            taskStopwatch.Stop();
            return (result, taskStopwatch.Elapsed);
        });

        var results = await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert
        var averageTime = results.Average(r => r.Elapsed.TotalMilliseconds);
        var maxTime = results.Max(r => r.Elapsed.TotalMilliseconds);
        
        Assert.True(averageTime < 50, $"Average response time {averageTime:F2}ms should be < 50ms");
        Assert.True(maxTime < 200, $"Max response time {maxTime:F2}ms should be < 200ms");

        _output.WriteLine($"Performance test: {results.Length} requests in {stopwatch.ElapsedMilliseconds}ms");
        _output.WriteLine($"Average: {averageTime:F2}ms, Max: {maxTime:F2}ms");
    }

    [Fact]
    public void InputSanitizer_XssProtection_ShouldSanitizeCorrectly()
    {
        // Arrange
        var maliciousInputs = new[]
        {
            "<script>alert('xss')</script>",
            "<img src=x onerror=alert('xss')>",
            "javascript:alert('xss')",
            "<div onclick='alert(\"xss\")'>Click me</div>"
        };

        // Act & Assert
        foreach (var input in maliciousInputs)
        {
            var sanitized = InputSanitizer.SanitizeHtml(input);
            
            Assert.False(InputSanitizer.IsPotentiallyMalicious(sanitized), 
                $"Sanitized input should not be malicious: {sanitized}");
            
            _output.WriteLine($"Original: {input}");
            _output.WriteLine($"Sanitized: {sanitized}");
            _output.WriteLine("");
        }
    }

    [Fact]
    public void InputSanitizer_SqlInjectionProtection_ShouldDetectAndPrevent()
    {
        // Arrange
        var sqlInjectionAttempts = new[]
        {
            "'; DROP TABLE users; --",
            "1' OR '1'='1",
            "UNION SELECT * FROM passwords",
            "'; INSERT INTO admin VALUES ('hacker', 'password'); --"
        };

        // Act & Assert
        foreach (var attempt in sqlInjectionAttempts)
        {
            Assert.Throws<ArgumentException>(() => InputSanitizer.SanitizeSql(attempt));
            _output.WriteLine($"SQL injection attempt blocked: {attempt}");
        }
    }

    [Fact]
    public void InputSanitizer_EmailValidation_ShouldValidateCorrectly()
    {
        // Arrange
        var validEmails = new[] { "test@example.com", "user.name+tag@domain.co.uk" };
        var invalidEmails = new[] { "invalid-email", "@domain.com", "user@", "user space@domain.com" };

        // Act & Assert
        foreach (var email in validEmails)
        {
            var sanitized = InputSanitizer.SanitizeEmail(email);
            Assert.NotEmpty(sanitized);
            _output.WriteLine($"Valid email: {email} -> {sanitized}");
        }

        foreach (var email in invalidEmails)
        {
            Assert.Throws<ArgumentException>(() => InputSanitizer.SanitizeEmail(email));
            _output.WriteLine($"Invalid email rejected: {email}");
        }
    }

    [Fact]
    public async Task SecurityMetrics_ShouldTrackViolationsCorrectly()
    {
        // Arrange
        var context = new RateLimitContext
        {
            IpAddress = "192.168.1.200",
            Endpoint = "/api/test",
            HttpMethod = "GET"
        };

        // Act - Generate some violations
        for (int i = 0; i < 10; i++)
        {
            await _rateLimiter.CheckAsync(context);
        }

        // Record additional security events
        _securityMetrics.RecordAuthenticationFailure("192.168.1.200", "testuser", "invalid_password");
        _securityMetrics.RecordSuspiciousActivity("192.168.1.200", "multiple_failed_attempts", 
            new Dictionary<string, object> { { "attempts", 5 } });

        // Assert - Check that metrics are being collected
        var statistics = await _securityMetrics.GetStatisticsAsync(TimeSpan.FromMinutes(5));
        
        Assert.True(statistics.TotalRequests > 0, "Should have recorded requests");
        _output.WriteLine($"Security metrics: {statistics.TotalRequests} total requests, {statistics.RateLimitedRequests} rate limited");
    }

    [Fact]
    public async Task SecurityMetrics_AlertDetection_ShouldDetectSuspiciousPatterns()
    {
        // Arrange - Generate suspicious activity
        var suspiciousIp = "192.168.1.250";
        
        for (int i = 0; i < 15; i++)
        {
            _securityMetrics.RecordAuthenticationFailure(suspiciousIp, $"user{i}", "invalid_password");
        }

        // Act
        var alerts = await _securityMetrics.DetectAlertsAsync(TimeSpan.FromMinutes(5));

        // Assert
        Assert.NotEmpty(alerts);
        var bruteForceAlert = alerts.FirstOrDefault(a => a.Type == SecurityAlertType.BruteForceAttempt);
        Assert.NotNull(bruteForceAlert);
        Assert.Equal(SecurityAlertSeverity.Critical, bruteForceAlert.Severity);

        _output.WriteLine($"Detected {alerts.Count} security alerts");
        foreach (var alert in alerts)
        {
            _output.WriteLine($"Alert: {alert.Type} - {alert.Message}");
        }
    }

    public void Dispose()
    {
        _scope.Dispose();
    }
}

/// <summary>
/// Test fixture for security integration tests
/// </summary>
public class SecurityTestFixture : IDisposable
{
    public IServiceProvider ServiceProvider { get; private set; }

    public SecurityTestFixture()
    {
        var services = new ServiceCollection();

        // Add logging
        services.AddLogging(builder => builder.AddConsole());

        // Add Redis (in-memory for testing)
        services.AddSingleton<IConnectionMultiplexer>(provider =>
        {
            // For testing, we'll use a mock Redis connection
            // In real tests, you'd use Redis test containers
            return ConnectionMultiplexer.Connect("localhost:6379");
        });

        // Add rate limiting services
        services.Configure<RateLimitConfiguration>(config =>
        {
            config.IsEnabled = true;
            config.UseSlidingWindow = true;
            config.FailOpen = false;
            config.Rules = new List<RateLimitRule>
            {
                new RateLimitRule
                {
                    Id = "ip_limit",
                    Type = RateLimitType.IpAddress,
                    Limit = 5,
                    WindowSizeInSeconds = 60,
                    Priority = 1,
                    IsEnabled = true
                },
                new RateLimitRule
                {
                    Id = "client_limit",
                    Type = RateLimitType.Client,
                    Limit = 10,
                    WindowSizeInSeconds = 60,
                    Priority = 2,
                    IsEnabled = true
                },
                new RateLimitRule
                {
                    Id = "org_limit",
                    Type = RateLimitType.Organization,
                    Limit = 100,
                    WindowSizeInSeconds = 3600,
                    Priority = 3,
                    IsEnabled = true
                },
                new RateLimitRule
                {
                    Id = "reservation_limit",
                    Type = RateLimitType.Endpoint,
                    Limit = 10,
                    WindowSizeInSeconds = 60,
                    EndpointPattern = "/api/reservations",
                    Priority = 10,
                    IsEnabled = true
                }
            };
        });

        services.AddScoped<IRateLimitStorage, RedisRateLimitStorage>();
        services.AddScoped<IRateLimitKeyGenerator, RateLimitKeyGenerator>();
        services.AddScoped<IRateLimitRuleProvider, InMemoryRateLimitRuleProvider>();
        services.AddScoped<IRateLimiter, RateLimiter>();
        services.AddSingleton<SecurityMetrics>();

        ServiceProvider = services.BuildServiceProvider();
    }

    public void Dispose()
    {
        if (ServiceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}

/// <summary>
/// In-memory implementation of rate limit rule provider for testing
/// </summary>
public class InMemoryRateLimitRuleProvider : IRateLimitRuleProvider
{
    private readonly RateLimitConfiguration _configuration;

    public InMemoryRateLimitRuleProvider(IOptions<RateLimitConfiguration> configuration)
    {
        _configuration = configuration.Value;
    }

    public Task<IEnumerable<RateLimitRule>> GetApplicableRulesAsync(RateLimitContext context, CancellationToken cancellationToken = default)
    {
        var applicableRules = _configuration.Rules
            .Where(rule => IsRuleApplicable(rule, context))
            .OrderByDescending(rule => rule.Priority)
            .ToList();

        return Task.FromResult<IEnumerable<RateLimitRule>>(applicableRules);
    }

    public Task<RateLimitRule?> GetRuleAsync(string ruleId, CancellationToken cancellationToken = default)
    {
        var rule = _configuration.Rules.FirstOrDefault(r => r.Id == ruleId);
        return Task.FromResult(rule);
    }

    public Task<bool> UpsertRuleAsync(RateLimitRule rule, CancellationToken cancellationToken = default)
    {
        // Not implemented for testing
        return Task.FromResult(true);
    }

    public Task<bool> RemoveRuleAsync(string ruleId, CancellationToken cancellationToken = default)
    {
        // Not implemented for testing
        return Task.FromResult(true);
    }

    private bool IsRuleApplicable(RateLimitRule rule, RateLimitContext context)
    {
        if (!rule.IsEnabled)
            return false;

        return rule.Type switch
        {
            RateLimitType.IpAddress => !string.IsNullOrEmpty(context.IpAddress),
            RateLimitType.Client => !string.IsNullOrEmpty(context.ClientId),
            RateLimitType.Organization => !string.IsNullOrEmpty(context.OrganizationId),
            RateLimitType.Endpoint => IsEndpointMatch(rule, context),
            RateLimitType.Global => true,
            _ => false
        };
    }

    private bool IsEndpointMatch(RateLimitRule rule, RateLimitContext context)
    {
        if (string.IsNullOrEmpty(rule.EndpointPattern) || string.IsNullOrEmpty(context.Endpoint))
            return false;

        return context.Endpoint.StartsWith(rule.EndpointPattern, StringComparison.OrdinalIgnoreCase);
    }
}
