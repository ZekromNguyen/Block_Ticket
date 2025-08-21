using Event.Application.Common.Interfaces;
using Event.Domain.Entities;
using Event.Domain.Enums;
using Event.Domain.Interfaces;
using Event.Domain.ValueObjects;
using Event.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using Xunit;
using Xunit.Abstractions;

namespace Event.Tests.Integration;

/// <summary>
/// Performance tests for Row-Level Security implementation
/// </summary>
public class RowLevelSecurityPerformanceTests : IClassFixture<RlsTestFixture>, IDisposable
{
    private readonly RlsTestFixture _fixture;
    private readonly ITestOutputHelper _output;
    private readonly IServiceScope _scope;
    private readonly EventDbContext _dbContext;
    private readonly IOrganizationContextProvider _organizationContextProvider;
    private readonly IEventRepository _eventRepository;

    // Test data
    private readonly Guid _org1Id = Guid.NewGuid();
    private readonly Guid _org2Id = Guid.NewGuid();
    private readonly Guid _user1Id = Guid.NewGuid();
    private readonly Guid _user2Id = Guid.NewGuid();

    public RowLevelSecurityPerformanceTests(RlsTestFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
        _scope = _fixture.ServiceProvider.CreateScope();
        _dbContext = _scope.ServiceProvider.GetRequiredService<EventDbContext>();
        _organizationContextProvider = _scope.ServiceProvider.GetRequiredService<IOrganizationContextProvider>();
        _eventRepository = _scope.ServiceProvider.GetRequiredService<IEventRepository>();
    }

    [Fact]
    public async Task QueryPerformance_WithRLS_ShouldMaintainAcceptablePerformance()
    {
        // Arrange - Create test data
        const int eventsPerOrg = 100;
        await CreateTestDataAsync(eventsPerOrg);

        _organizationContextProvider.SetOrganizationContext(_org1Id, _user1Id);

        // Warm up
        await _eventRepository.GetByOrganizationAsync();

        // Act - Measure query performance
        var stopwatch = Stopwatch.StartNew();
        var events = await _eventRepository.GetByOrganizationAsync();
        stopwatch.Stop();

        // Assert
        Assert.Equal(eventsPerOrg, events.Count());
        Assert.True(stopwatch.ElapsedMilliseconds < 1000, 
            $"Query took {stopwatch.ElapsedMilliseconds}ms, expected < 1000ms");

        _output.WriteLine($"Query performance: {stopwatch.ElapsedMilliseconds}ms for {eventsPerOrg} events");
    }

    [Fact]
    public async Task BulkOperations_WithRLS_ShouldScaleWell()
    {
        // Arrange
        const int batchSize = 50;
        _organizationContextProvider.SetOrganizationContext(_org1Id, _user1Id);

        var events = new List<EventAggregate>();
        for (int i = 0; i < batchSize; i++)
        {
            var eventAggregate = EventAggregate.CreateNew(
                $"Bulk Event {i}",
                "Bulk test event",
                _user1Id,
                Guid.NewGuid(),
                DateTime.UtcNow.AddDays(30 + i),
                new TimeZoneId("UTC"));
            events.Add(eventAggregate);
        }

        // Act - Measure bulk insert performance
        var stopwatch = Stopwatch.StartNew();
        
        foreach (var eventAggregate in events)
        {
            await _eventRepository.AddAsync(eventAggregate);
        }
        await _dbContext.SaveChangesAsync();
        
        stopwatch.Stop();

        // Assert
        Assert.True(stopwatch.ElapsedMilliseconds < 5000, 
            $"Bulk insert took {stopwatch.ElapsedMilliseconds}ms, expected < 5000ms");

        _output.WriteLine($"Bulk insert performance: {stopwatch.ElapsedMilliseconds}ms for {batchSize} events");
    }

    [Fact]
    public async Task ConcurrentAccess_WithDifferentOrganizations_ShouldMaintainIsolation()
    {
        // Arrange
        const int eventsPerOrg = 20;
        await CreateTestDataAsync(eventsPerOrg);

        // Act - Simulate concurrent access from different organizations
        var tasks = new List<Task<(int org1Count, int org2Count, long elapsedMs)>>();

        for (int i = 0; i < 10; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                using var scope = _fixture.ServiceProvider.CreateScope();
                var contextProvider = scope.ServiceProvider.GetRequiredService<IOrganizationContextProvider>();
                var eventRepo = scope.ServiceProvider.GetRequiredService<IEventRepository>();

                var stopwatch = Stopwatch.StartNew();

                // Query as org1
                contextProvider.SetOrganizationContext(_org1Id, _user1Id);
                var org1Events = await eventRepo.GetByOrganizationAsync();

                // Query as org2
                contextProvider.SetOrganizationContext(_org2Id, _user2Id);
                var org2Events = await eventRepo.GetByOrganizationAsync();

                stopwatch.Stop();

                return (org1Events.Count(), org2Events.Count(), stopwatch.ElapsedMilliseconds);
            }));
        }

        var results = await Task.WhenAll(tasks);

        // Assert
        foreach (var (org1Count, org2Count, elapsedMs) in results)
        {
            Assert.Equal(eventsPerOrg, org1Count);
            Assert.Equal(eventsPerOrg, org2Count);
            Assert.True(elapsedMs < 2000, $"Concurrent query took {elapsedMs}ms, expected < 2000ms");
        }

        var avgTime = results.Average(r => r.elapsedMs);
        _output.WriteLine($"Concurrent access average time: {avgTime:F2}ms per thread");
    }

    [Fact]
    public async Task ComplexQuery_WithRLS_ShouldUseIndexesEffectively()
    {
        // Arrange
        const int eventsPerOrg = 50;
        await CreateTestDataAsync(eventsPerOrg);

        _organizationContextProvider.SetOrganizationContext(_org1Id, _user1Id);

        // Act - Complex query with multiple filters
        var stopwatch = Stopwatch.StartNew();
        
        var complexQuery = await _eventRepository.FindAsync(e => 
            e.Status == EventStatus.Published &&
            e.EventDate >= DateTime.UtcNow &&
            e.EventDate <= DateTime.UtcNow.AddDays(60));
        
        stopwatch.Stop();

        // Assert
        Assert.True(stopwatch.ElapsedMilliseconds < 500, 
            $"Complex query took {stopwatch.ElapsedMilliseconds}ms, expected < 500ms");

        _output.WriteLine($"Complex query performance: {stopwatch.ElapsedMilliseconds}ms");
    }

    [Fact]
    public async Task CountOperations_WithRLS_ShouldBeOptimized()
    {
        // Arrange
        const int eventsPerOrg = 100;
        await CreateTestDataAsync(eventsPerOrg);

        _organizationContextProvider.SetOrganizationContext(_org1Id, _user1Id);

        // Act - Measure count performance
        var stopwatch = Stopwatch.StartNew();
        var count = await _eventRepository.CountAsync();
        stopwatch.Stop();

        // Assert
        Assert.Equal(eventsPerOrg, count);
        Assert.True(stopwatch.ElapsedMilliseconds < 100, 
            $"Count query took {stopwatch.ElapsedMilliseconds}ms, expected < 100ms");

        _output.WriteLine($"Count query performance: {stopwatch.ElapsedMilliseconds}ms");
    }

    [Fact]
    public async Task ExistsOperations_WithRLS_ShouldBeOptimized()
    {
        // Arrange
        const int eventsPerOrg = 100;
        await CreateTestDataAsync(eventsPerOrg);

        _organizationContextProvider.SetOrganizationContext(_org1Id, _user1Id);

        // Get a random event ID from org1
        var org1Events = await _eventRepository.GetByOrganizationAsync();
        var testEventId = org1Events.First().Id;

        // Act - Measure exists performance
        var stopwatch = Stopwatch.StartNew();
        var exists = await _eventRepository.ExistsAsync(e => e.Id == testEventId);
        stopwatch.Stop();

        // Assert
        Assert.True(exists);
        Assert.True(stopwatch.ElapsedMilliseconds < 50, 
            $"Exists query took {stopwatch.ElapsedMilliseconds}ms, expected < 50ms");

        _output.WriteLine($"Exists query performance: {stopwatch.ElapsedMilliseconds}ms");
    }

    [Fact]
    public async Task UpdateOperations_WithRLS_ShouldMaintainPerformance()
    {
        // Arrange
        const int eventsToUpdate = 20;
        await CreateTestDataAsync(eventsToUpdate);

        _organizationContextProvider.SetOrganizationContext(_org1Id, _user1Id);
        var events = (await _eventRepository.GetByOrganizationAsync()).Take(eventsToUpdate).ToList();

        // Act - Measure update performance
        var stopwatch = Stopwatch.StartNew();
        
        foreach (var eventAggregate in events)
        {
            eventAggregate.UpdateBasicInfo($"Updated {eventAggregate.Title}", eventAggregate.Description);
            _eventRepository.Update(eventAggregate);
        }
        await _dbContext.SaveChangesAsync();
        
        stopwatch.Stop();

        // Assert
        Assert.True(stopwatch.ElapsedMilliseconds < 2000, 
            $"Bulk update took {stopwatch.ElapsedMilliseconds}ms, expected < 2000ms");

        _output.WriteLine($"Bulk update performance: {stopwatch.ElapsedMilliseconds}ms for {eventsToUpdate} events");
    }

    [Fact]
    public async Task MemoryUsage_WithRLS_ShouldBeReasonable()
    {
        // Arrange
        const int eventsPerOrg = 200;
        
        var initialMemory = GC.GetTotalMemory(true);
        
        // Act
        await CreateTestDataAsync(eventsPerOrg);
        
        _organizationContextProvider.SetOrganizationContext(_org1Id, _user1Id);
        var events = await _eventRepository.GetByOrganizationAsync();
        
        var finalMemory = GC.GetTotalMemory(false);
        var memoryUsed = finalMemory - initialMemory;

        // Assert
        Assert.Equal(eventsPerOrg, events.Count());
        
        // Memory usage should be reasonable (less than 50MB for 200 events)
        var memoryUsedMB = memoryUsed / (1024.0 * 1024.0);
        Assert.True(memoryUsedMB < 50, $"Memory usage: {memoryUsedMB:F2}MB, expected < 50MB");

        _output.WriteLine($"Memory usage: {memoryUsedMB:F2}MB for {eventsPerOrg} events");
    }

    private async Task CreateTestDataAsync(int eventsPerOrg)
    {
        // Create events for org1
        _organizationContextProvider.SetOrganizationContext(_org1Id, _user1Id);
        for (int i = 0; i < eventsPerOrg; i++)
        {
            var eventAggregate = EventAggregate.CreateNew(
                $"Org1 Event {i}",
                $"Test event {i} for organization 1",
                _user1Id,
                Guid.NewGuid(),
                DateTime.UtcNow.AddDays(30 + i),
                new TimeZoneId("UTC"));

            // Set some events as published for testing
            if (i % 3 == 0)
            {
                var statusProperty = typeof(EventAggregate).GetProperty("Status");
                statusProperty?.SetValue(eventAggregate, EventStatus.Published);
            }

            await _eventRepository.AddAsync(eventAggregate);
        }

        // Create events for org2
        _organizationContextProvider.SetOrganizationContext(_org2Id, _user2Id);
        for (int i = 0; i < eventsPerOrg; i++)
        {
            var eventAggregate = EventAggregate.CreateNew(
                $"Org2 Event {i}",
                $"Test event {i} for organization 2",
                _user2Id,
                Guid.NewGuid(),
                DateTime.UtcNow.AddDays(30 + i),
                new TimeZoneId("UTC"));

            // Set some events as published for testing
            if (i % 3 == 0)
            {
                var statusProperty = typeof(EventAggregate).GetProperty("Status");
                statusProperty?.SetValue(eventAggregate, EventStatus.Published);
            }

            await _eventRepository.AddAsync(eventAggregate);
        }

        await _dbContext.SaveChangesAsync();
    }

    public void Dispose()
    {
        _organizationContextProvider.ClearOrganizationContext();
        _scope.Dispose();
    }
}
