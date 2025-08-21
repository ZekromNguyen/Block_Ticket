using Event.Application.Common.Interfaces;
using Event.Domain.Entities;
using Event.Domain.Enums;
using Event.Domain.Interfaces;
using Event.Domain.ValueObjects;
using Event.Infrastructure.Persistence;
using Event.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace Event.Tests.Integration;

/// <summary>
/// Integration tests for Row-Level Security (RLS) implementation
/// </summary>
public class RowLevelSecurityTests : IClassFixture<RlsTestFixture>, IDisposable
{
    private readonly RlsTestFixture _fixture;
    private readonly ITestOutputHelper _output;
    private readonly IServiceScope _scope;
    private readonly EventDbContext _dbContext;
    private readonly IOrganizationContextProvider _organizationContextProvider;
    private readonly IEventRepository _eventRepository;

    // Test organizations
    private readonly Guid _org1Id = Guid.NewGuid();
    private readonly Guid _org2Id = Guid.NewGuid();
    private readonly Guid _user1Id = Guid.NewGuid();
    private readonly Guid _user2Id = Guid.NewGuid();

    public RowLevelSecurityTests(RlsTestFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
        _scope = _fixture.ServiceProvider.CreateScope();
        _dbContext = _scope.ServiceProvider.GetRequiredService<EventDbContext>();
        _organizationContextProvider = _scope.ServiceProvider.GetRequiredService<IOrganizationContextProvider>();
        _eventRepository = _scope.ServiceProvider.GetRequiredService<IEventRepository>();
    }

    [Fact]
    public async Task CreateEvent_WithOrganizationContext_ShouldSetCorrectOrganizationId()
    {
        // Arrange
        _organizationContextProvider.SetOrganizationContext(_org1Id, _user1Id);

        var eventAggregate = EventAggregate.CreateNew(
            "Test Event",
            "Test Description",
            _user1Id, // promoterId
            Guid.NewGuid(), // venueId
            DateTime.UtcNow.AddDays(30),
            new TimeZoneId("UTC"));

        // Act
        var result = await _eventRepository.AddAsync(eventAggregate);
        await _dbContext.SaveChangesAsync();

        // Assert
        Assert.Equal(_org1Id, result.OrganizationId);
        _output.WriteLine($"Event created with OrganizationId: {result.OrganizationId}");
    }

    [Fact]
    public async Task GetEvent_FromSameOrganization_ShouldReturnEvent()
    {
        // Arrange
        var eventId = await CreateTestEventAsync(_org1Id, _user1Id);
        _organizationContextProvider.SetOrganizationContext(_org1Id, _user1Id);

        // Act
        var result = await _eventRepository.GetByIdAsync(eventId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(eventId, result.Id);
        Assert.Equal(_org1Id, result.OrganizationId);
        _output.WriteLine($"Successfully retrieved event from same organization");
    }

    [Fact]
    public async Task GetEvent_FromDifferentOrganization_ShouldThrowUnauthorized()
    {
        // Arrange
        var eventId = await CreateTestEventAsync(_org1Id, _user1Id);
        _organizationContextProvider.SetOrganizationContext(_org2Id, _user2Id); // Different org

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
        {
            await _eventRepository.GetByIdAsync(eventId);
        });

        _output.WriteLine($"Correctly blocked access to event from different organization");
    }

    [Fact]
    public async Task GetEventsByOrganization_ShouldOnlyReturnOwnEvents()
    {
        // Arrange
        var org1Event1 = await CreateTestEventAsync(_org1Id, _user1Id, "Org1 Event 1");
        var org1Event2 = await CreateTestEventAsync(_org1Id, _user1Id, "Org1 Event 2");
        var org2Event1 = await CreateTestEventAsync(_org2Id, _user2Id, "Org2 Event 1");

        _organizationContextProvider.SetOrganizationContext(_org1Id, _user1Id);

        // Act
        var org1Events = await _eventRepository.GetByOrganizationAsync();

        // Assert
        Assert.Equal(2, org1Events.Count());
        Assert.All(org1Events, e => Assert.Equal(_org1Id, e.OrganizationId));
        Assert.Contains(org1Events, e => e.Id == org1Event1);
        Assert.Contains(org1Events, e => e.Id == org1Event2);
        Assert.DoesNotContain(org1Events, e => e.Id == org2Event1);

        _output.WriteLine($"Organization 1 can see {org1Events.Count()} events (expected: 2)");
    }

    [Fact]
    public async Task UpdateEvent_FromDifferentOrganization_ShouldThrowUnauthorized()
    {
        // Arrange
        var eventId = await CreateTestEventAsync(_org1Id, _user1Id);
        _organizationContextProvider.SetOrganizationContext(_org2Id, _user2Id); // Different org

        var eventToUpdate = await _dbContext.Events.FirstAsync(e => e.Id == eventId);

        // Act & Assert
        Assert.Throws<UnauthorizedAccessException>(() =>
        {
            _eventRepository.Update(eventToUpdate);
        });

        _output.WriteLine($"Correctly blocked update of event from different organization");
    }

    [Fact]
    public async Task DeleteEvent_FromDifferentOrganization_ShouldThrowUnauthorized()
    {
        // Arrange
        var eventId = await CreateTestEventAsync(_org1Id, _user1Id);
        _organizationContextProvider.SetOrganizationContext(_org2Id, _user2Id); // Different org

        var eventToDelete = await _dbContext.Events.FirstAsync(e => e.Id == eventId);

        // Act & Assert
        Assert.Throws<UnauthorizedAccessException>(() =>
        {
            _eventRepository.Delete(eventToDelete);
        });

        _output.WriteLine($"Correctly blocked deletion of event from different organization");
    }

    [Fact]
    public async Task QueryEvents_WithGlobalFilter_ShouldOnlyReturnOwnEvents()
    {
        // Arrange
        await CreateTestEventAsync(_org1Id, _user1Id, "Org1 Event");
        await CreateTestEventAsync(_org2Id, _user2Id, "Org2 Event");

        _organizationContextProvider.SetOrganizationContext(_org1Id, _user1Id);

        // Act - Direct EF query should also be filtered
        var events = await _dbContext.Events.ToListAsync();

        // Assert
        Assert.Single(events);
        Assert.Equal(_org1Id, events.First().OrganizationId);

        _output.WriteLine($"Global query filter working: {events.Count} events returned for organization 1");
    }

    [Fact]
    public async Task CountEvents_WithOrganizationFilter_ShouldReturnCorrectCount()
    {
        // Arrange
        await CreateTestEventAsync(_org1Id, _user1Id, "Org1 Event 1");
        await CreateTestEventAsync(_org1Id, _user1Id, "Org1 Event 2");
        await CreateTestEventAsync(_org2Id, _user2Id, "Org2 Event 1");

        _organizationContextProvider.SetOrganizationContext(_org1Id, _user1Id);

        // Act
        var count = await _eventRepository.CountAsync();

        // Assert
        Assert.Equal(2, count);

        _output.WriteLine($"Count query returned {count} events for organization 1 (expected: 2)");
    }

    [Fact]
    public async Task ExistsCheck_WithOrganizationFilter_ShouldRespectTenantBoundaries()
    {
        // Arrange
        var org1EventId = await CreateTestEventAsync(_org1Id, _user1Id, "Org1 Event");
        var org2EventId = await CreateTestEventAsync(_org2Id, _user2Id, "Org2 Event");

        _organizationContextProvider.SetOrganizationContext(_org1Id, _user1Id);

        // Act
        var org1EventExists = await _eventRepository.ExistsAsync(e => e.Id == org1EventId);
        var org2EventExists = await _eventRepository.ExistsAsync(e => e.Id == org2EventId);

        // Assert
        Assert.True(org1EventExists);
        Assert.False(org2EventExists); // Should not see events from other organizations

        _output.WriteLine($"Exists check: Org1 event exists={org1EventExists}, Org2 event exists={org2EventExists}");
    }

    [Fact]
    public async Task FindEvents_WithPredicate_ShouldCombineWithOrganizationFilter()
    {
        // Arrange
        await CreateTestEventAsync(_org1Id, _user1Id, "Published Event", EventStatus.Published);
        await CreateTestEventAsync(_org1Id, _user1Id, "Draft Event", EventStatus.Draft);
        await CreateTestEventAsync(_org2Id, _user2Id, "Published Event Org2", EventStatus.Published);

        _organizationContextProvider.SetOrganizationContext(_org1Id, _user1Id);

        // Act
        var publishedEvents = await _eventRepository.FindAsync(e => e.Status == EventStatus.Published);

        // Assert
        Assert.Single(publishedEvents);
        Assert.Equal(_org1Id, publishedEvents.First().OrganizationId);
        Assert.Equal(EventStatus.Published, publishedEvents.First().Status);

        _output.WriteLine($"Find query returned {publishedEvents.Count()} published events for organization 1");
    }

    private async Task<Guid> CreateTestEventAsync(Guid organizationId, Guid userId, string title = "Test Event", EventStatus status = EventStatus.Draft)
    {
        _organizationContextProvider.SetOrganizationContext(organizationId, userId);

        var eventAggregate = EventAggregate.CreateNew(
            title,
            "Test Description",
            userId,
            Guid.NewGuid(),
            DateTime.UtcNow.AddDays(30),
            new TimeZoneId("UTC"));

        if (status != EventStatus.Draft)
        {
            // Use reflection to set status for testing
            var statusProperty = typeof(EventAggregate).GetProperty("Status");
            statusProperty?.SetValue(eventAggregate, status);
        }

        await _eventRepository.AddAsync(eventAggregate);
        await _dbContext.SaveChangesAsync();

        return eventAggregate.Id;
    }

    public void Dispose()
    {
        _organizationContextProvider.ClearOrganizationContext();
        _scope.Dispose();
    }
}

/// <summary>
/// Test fixture for RLS tests
/// </summary>
public class RlsTestFixture : IDisposable
{
    public IServiceProvider ServiceProvider { get; private set; }

    public RlsTestFixture()
    {
        var services = new ServiceCollection();

        // Add logging
        services.AddLogging(builder => builder.AddConsole());

        // Add HTTP context accessor
        services.AddHttpContextAccessor();

        // Add organization context provider
        services.AddScoped<IOrganizationContextProvider, OrganizationContextProvider>();

        // Add in-memory database
        services.AddDbContext<EventDbContext>((serviceProvider, options) =>
        {
            options.UseInMemoryDatabase($"RlsTestDb_{Guid.NewGuid()}");
            options.EnableSensitiveDataLogging();
        });

        // Add repositories
        services.AddScoped<IEventRepository, Event.Infrastructure.Persistence.Repositories.EventRepository>();

        // Add audit interceptor
        services.AddScoped<Event.Infrastructure.Persistence.Interceptors.AuditInterceptor>();

        ServiceProvider = services.BuildServiceProvider();

        // Initialize database
        using var scope = ServiceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<EventDbContext>();
        dbContext.Database.EnsureCreated();
    }

    public void Dispose()
    {
        if (ServiceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}
