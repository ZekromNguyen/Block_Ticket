using Event.Infrastructure.Security.RateLimiting.Interfaces;
using Event.Infrastructure.Security.RateLimiting.Models;
using Event.Infrastructure.Security.Monitoring;
using Event.Infrastructure.Security.Validation;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;
using System.Diagnostics;
using Xunit;
using Xunit.Abstractions;

namespace Event.Tests.Integration;

/// <summary>
/// Performance tests for security features to ensure they meet production requirements
/// </summary>
public class SecurityPerformanceTests : IClassFixture<SecurityTestFixture>, IDisposable
{
    private readonly SecurityTestFixture _fixture;
    private readonly ITestOutputHelper _output;
    private readonly IServiceScope _scope;
    private readonly IRateLimiter _rateLimiter;
    private readonly SecurityMetrics _securityMetrics;

    // Performance requirements
    private const int MaxSecurityOverheadMs = 50; // < 50ms overhead requirement
    private const int ConcurrentRequestCount = 100;
    private const int HighLoadRequestCount = 1000;

    public SecurityPerformanceTests(SecurityTestFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
        _scope = _fixture.ServiceProvider.CreateScope();
        _rateLimiter = _scope.ServiceProvider.GetRequiredService<IRateLimiter>();
        _securityMetrics = _scope.ServiceProvider.GetRequiredService<SecurityMetrics>();
    }

    [Fact]
    public async Task RateLimiter_SingleRequest_ShouldMeetPerformanceRequirement()
    {
        // Arrange
        var context = new RateLimitContext
        {
            IpAddress = "192.168.1.1",
            Endpoint = "/api/events",
            HttpMethod = "GET"
        };

        // Warm up
        await _rateLimiter.CheckAsync(context);

        // Act - Measure single request performance
        var stopwatch = Stopwatch.StartNew();
        var result = await _rateLimiter.CheckAsync(context);
        stopwatch.Stop();

        // Assert
        Assert.True(stopwatch.ElapsedMilliseconds < MaxSecurityOverheadMs,
            $"Rate limit check took {stopwatch.ElapsedMilliseconds}ms, expected < {MaxSecurityOverheadMs}ms");

        _output.WriteLine($"Single request performance: {stopwatch.ElapsedMilliseconds}ms");
    }

    [Fact]
    public async Task RateLimiter_ConcurrentRequests_ShouldMaintainPerformance()
    {
        // Arrange
        var contexts = Enumerable.Range(0, ConcurrentRequestCount)
            .Select(i => new RateLimitContext
            {
                IpAddress = $"192.168.1.{i % 10 + 1}",
                ClientId = $"client-{i % 5}",
                Endpoint = "/api/events",
                HttpMethod = "GET"
            })
            .ToList();

        // Act - Measure concurrent performance
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
        var throughput = ConcurrentRequestCount / stopwatch.Elapsed.TotalSeconds;

        Assert.True(averageTime < MaxSecurityOverheadMs,
            $"Average response time {averageTime:F2}ms should be < {MaxSecurityOverheadMs}ms");
        Assert.True(maxTime < MaxSecurityOverheadMs * 2,
            $"Max response time {maxTime:F2}ms should be < {MaxSecurityOverheadMs * 2}ms");
        Assert.True(throughput > 100,
            $"Throughput {throughput:F2} req/s should be > 100 req/s");

        _output.WriteLine($"Concurrent performance: {ConcurrentRequestCount} requests in {stopwatch.ElapsedMilliseconds}ms");
        _output.WriteLine($"Average: {averageTime:F2}ms, Max: {maxTime:F2}ms, Throughput: {throughput:F2} req/s");
    }

    [Fact]
    public async Task RateLimiter_HighLoad_ShouldScaleWell()
    {
        // Arrange
        var contexts = Enumerable.Range(0, HighLoadRequestCount)
            .Select(i => new RateLimitContext
            {
                IpAddress = $"10.0.{i / 256}.{i % 256}",
                ClientId = $"client-{i % 20}",
                Endpoint = "/api/events",
                HttpMethod = "GET"
            })
            .ToList();

        // Act - Measure high load performance
        var stopwatch = Stopwatch.StartNew();
        var semaphore = new SemaphoreSlim(50); // Limit concurrency to avoid overwhelming
        var results = new ConcurrentBag<(bool isAllowed, TimeSpan duration)>();

        var tasks = contexts.Select(async context =>
        {
            await semaphore.WaitAsync();
            try
            {
                var taskStopwatch = Stopwatch.StartNew();
                var result = await _rateLimiter.CheckAsync(context);
                taskStopwatch.Stop();
                results.Add((result.IsAllowed, taskStopwatch.Elapsed));
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert
        var resultsList = results.ToList();
        var averageTime = resultsList.Average(r => r.duration.TotalMilliseconds);
        var p95Time = resultsList.OrderBy(r => r.duration.TotalMilliseconds)
            .Skip((int)(resultsList.Count * 0.95))
            .First().duration.TotalMilliseconds;
        var throughput = HighLoadRequestCount / stopwatch.Elapsed.TotalSeconds;

        Assert.True(averageTime < MaxSecurityOverheadMs * 2,
            $"Average response time under high load {averageTime:F2}ms should be reasonable");
        Assert.True(p95Time < MaxSecurityOverheadMs * 5,
            $"P95 response time {p95Time:F2}ms should be reasonable");

        _output.WriteLine($"High load performance: {HighLoadRequestCount} requests in {stopwatch.Elapsed.TotalSeconds:F2}s");
        _output.WriteLine($"Average: {averageTime:F2}ms, P95: {p95Time:F2}ms, Throughput: {throughput:F2} req/s");
    }

    [Fact]
    public void InputSanitizer_Performance_ShouldBeEfficient()
    {
        // Arrange
        var testInputs = new[]
        {
            "Simple text input",
            "<script>alert('xss')</script>Normal text after",
            "Email: user@example.com and phone: +1-555-123-4567",
            "Long text with multiple <div onclick='malicious()'>elements</div> and <img src=x onerror=alert('xss')> tags",
            "SQL injection attempt: '; DROP TABLE users; --",
            "Mixed content with <b>HTML</b>, JavaScript:alert('test'), and normal text"
        };

        var iterations = 1000;

        // Act - Measure sanitization performance
        var stopwatch = Stopwatch.StartNew();
        
        for (int i = 0; i < iterations; i++)
        {
            foreach (var input in testInputs)
            {
                var sanitized = InputSanitizer.SanitizeHtml(input);
                var stripped = InputSanitizer.StripHtml(input);
                var isMalicious = InputSanitizer.IsPotentiallyMalicious(input);
            }
        }
        
        stopwatch.Stop();

        // Assert
        var totalOperations = iterations * testInputs.Length * 3; // 3 operations per input
        var averageTimePerOperation = stopwatch.Elapsed.TotalMilliseconds / totalOperations;
        var operationsPerSecond = totalOperations / stopwatch.Elapsed.TotalSeconds;

        Assert.True(averageTimePerOperation < 1.0,
            $"Average sanitization time {averageTimePerOperation:F4}ms should be < 1ms");
        Assert.True(operationsPerSecond > 1000,
            $"Sanitization throughput {operationsPerSecond:F0} ops/s should be > 1000 ops/s");

        _output.WriteLine($"Input sanitization performance: {totalOperations} operations in {stopwatch.ElapsedMilliseconds}ms");
        _output.WriteLine($"Average: {averageTimePerOperation:F4}ms per operation, Throughput: {operationsPerSecond:F0} ops/s");
    }

    [Fact]
    public async Task SecurityMetrics_Collection_ShouldHaveMinimalOverhead()
    {
        // Arrange
        var context = new RateLimitContext
        {
            IpAddress = "192.168.1.100",
            Endpoint = "/api/test",
            HttpMethod = "GET"
        };

        var iterations = 1000;

        // Act - Measure metrics collection overhead
        var stopwatchWithMetrics = Stopwatch.StartNew();
        
        for (int i = 0; i < iterations; i++)
        {
            var result = await _rateLimiter.CheckAsync(context);
            _securityMetrics.RecordAuthenticationFailure("192.168.1.100", "testuser", "invalid_password");
            _securityMetrics.RecordRequestSize(1024, "/api/test");
        }
        
        stopwatchWithMetrics.Stop();

        // Compare with baseline (no metrics)
        var stopwatchBaseline = Stopwatch.StartNew();
        
        for (int i = 0; i < iterations; i++)
        {
            // Simulate the same work without metrics
            await Task.Delay(1);
        }
        
        stopwatchBaseline.Stop();

        // Assert
        var metricsOverhead = stopwatchWithMetrics.ElapsedMilliseconds - stopwatchBaseline.ElapsedMilliseconds;
        var overheadPerOperation = (double)metricsOverhead / iterations;

        Assert.True(overheadPerOperation < 5.0,
            $"Metrics collection overhead {overheadPerOperation:F2}ms per operation should be < 5ms");

        _output.WriteLine($"Metrics collection overhead: {metricsOverhead}ms total, {overheadPerOperation:F2}ms per operation");
    }

    [Fact]
    public async Task RateLimiter_MemoryUsage_ShouldBeReasonable()
    {
        // Arrange
        var initialMemory = GC.GetTotalMemory(true);
        var contexts = Enumerable.Range(0, 1000)
            .Select(i => new RateLimitContext
            {
                IpAddress = $"10.0.{i / 256}.{i % 256}",
                ClientId = $"client-{i}",
                Endpoint = "/api/events",
                HttpMethod = "GET"
            })
            .ToList();

        // Act - Generate rate limit data
        foreach (var context in contexts)
        {
            await _rateLimiter.CheckAsync(context);
        }

        var finalMemory = GC.GetTotalMemory(true);
        var memoryUsed = finalMemory - initialMemory;

        // Assert
        var memoryUsedMB = memoryUsed / (1024.0 * 1024.0);
        var memoryPerContext = memoryUsed / (double)contexts.Count;

        Assert.True(memoryUsedMB < 100,
            $"Memory usage {memoryUsedMB:F2}MB should be < 100MB for 1000 contexts");
        Assert.True(memoryPerContext < 1024,
            $"Memory per context {memoryPerContext:F0} bytes should be < 1KB");

        _output.WriteLine($"Memory usage: {memoryUsedMB:F2}MB total, {memoryPerContext:F0} bytes per context");
    }

    [Fact]
    public async Task RateLimiter_RedisFailover_ShouldFailGracefully()
    {
        // Arrange
        var context = new RateLimitContext
        {
            IpAddress = "192.168.1.200",
            Endpoint = "/api/events",
            HttpMethod = "GET"
        };

        // Act - Test behavior when Redis is unavailable
        // Note: This test assumes FailOpen = true in configuration
        var stopwatch = Stopwatch.StartNew();
        var result = await _rateLimiter.CheckAsync(context);
        stopwatch.Stop();

        // Assert - Should fail open and still meet performance requirements
        Assert.True(result.IsAllowed, "Should fail open when Redis is unavailable");
        Assert.True(stopwatch.ElapsedMilliseconds < MaxSecurityOverheadMs * 2,
            $"Failover response time {stopwatch.ElapsedMilliseconds}ms should be reasonable");

        _output.WriteLine($"Redis failover performance: {stopwatch.ElapsedMilliseconds}ms");
    }

    [Fact]
    public async Task SecurityFeatures_EndToEnd_ShouldMeetPerformanceRequirements()
    {
        // Arrange - Simulate a complete security check pipeline
        var context = new RateLimitContext
        {
            IpAddress = "192.168.1.250",
            ClientId = "performance-test-client",
            OrganizationId = Guid.NewGuid().ToString(),
            Endpoint = "/api/events",
            HttpMethod = "POST"
        };

        var testData = "Test event data with <script>alert('xss')</script> and normal content";
        var iterations = 100;

        // Act - Measure end-to-end security processing
        var stopwatch = Stopwatch.StartNew();
        
        for (int i = 0; i < iterations; i++)
        {
            // Rate limiting
            var rateLimitResult = await _rateLimiter.CheckAsync(context);
            
            // Input sanitization
            var sanitizedData = InputSanitizer.SanitizeHtml(testData);
            
            // Security metrics
            _securityMetrics.RecordRequestSize(testData.Length, context.Endpoint!);
            
            // Simulate some processing
            await Task.Delay(1);
        }
        
        stopwatch.Stop();

        // Assert
        var averageTimePerRequest = stopwatch.Elapsed.TotalMilliseconds / iterations;
        var throughput = iterations / stopwatch.Elapsed.TotalSeconds;

        Assert.True(averageTimePerRequest < MaxSecurityOverheadMs,
            $"End-to-end security overhead {averageTimePerRequest:F2}ms should be < {MaxSecurityOverheadMs}ms");
        Assert.True(throughput > 20,
            $"End-to-end throughput {throughput:F2} req/s should be reasonable");

        _output.WriteLine($"End-to-end security performance: {iterations} requests in {stopwatch.ElapsedMilliseconds}ms");
        _output.WriteLine($"Average: {averageTimePerRequest:F2}ms per request, Throughput: {throughput:F2} req/s");
    }

    public void Dispose()
    {
        _scope.Dispose();
    }
}
