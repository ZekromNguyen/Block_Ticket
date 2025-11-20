using System.Net;
using System.Net.Http.Json;
using Event.Application.Features.Events.Commands.CreateEvent;
using Event.Application.Common.Models;
using Event.Domain.Enums;
using Event.Application.Features.TicketTypes.Commands.CreateTicketType;


using FluentAssertions;
using Xunit;
using Microsoft.AspNetCore.Mvc.Testing;

using Microsoft.Extensions.DependencyInjection;
using Event.Infrastructure.Persistence;

namespace Event.API.IntegrationTests;

public class EventsControllerTests
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory<Program> _factory;

    public EventsControllerTests()
    {
        _factory = new CustomWebApplicationFactory<Program>();
        _client = _factory.CreateClient();

        // Seed the database for each test
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<EventDbContext>();
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();
        SeedData.InitializeDbForTests(context);
    }

    [Fact]
    public async Task CreateEvent_WithValidData_ReturnsCreated()
    {
        // Arrange
        var request = new CreateEventRequest
        {
            Title = "Test Event",
            Description = "Test Description",
            Slug = "test-event-valid",
            OrganizationId = TestConstants.TestOrganizationId,
            PromoterId = TestConstants.TestPromoterId,
            VenueId = TestConstants.TestVenueId,
            EventDate = DateTime.UtcNow.AddDays(10),
            TimeZone = "UTC",
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/events", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdEvent = await response.Content.ReadFromJsonAsync<EventDto>();
        createdEvent.Should().NotBeNull();
        if (createdEvent != null)
        {
                                                response.Headers.Location.ToString().Should().MatchEquivalentOf($"*/api/v1.0/events/{createdEvent.Id}");
        }
    }


    [Fact]
    public async Task GetEvent_WithExistingId_ReturnsEvent()
    {
        // Arrange
        var createRequest = new CreateEventRequest
        {
            Title = "Get Test Event",
            Description = "Test Description for Get",
            Slug = "get-test-event",
            OrganizationId = TestConstants.TestOrganizationId,
            PromoterId = TestConstants.TestPromoterId,
            VenueId = TestConstants.TestVenueId,
            EventDate = DateTime.UtcNow.AddDays(20),
            TimeZone = "UTC",
        };
        var createResponse = await _client.PostAsJsonAsync("/api/v1/events", createRequest);
        var createdEvent = await createResponse.Content.ReadFromJsonAsync<EventDto>();
        createdEvent.Should().NotBeNull();

        // Act
        var getResponse = await _client.GetAsync($"/api/v1/events/{createdEvent.Id}");

        // Assert
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var fetchedEvent = await getResponse.Content.ReadFromJsonAsync<EventDto>();
        fetchedEvent.Should().NotBeNull();
        fetchedEvent.Id.Should().Be(createdEvent.Id);
        fetchedEvent.Title.Should().Be(createRequest.Title);
    }

    [Fact]
    public async Task GetEventBySlug_WithInvalidOrganizationId_ReturnsNotFound()
    {
        // Arrange
        var slug = "get-event-by-slug-test";
        var validOrganizationId = TestConstants.TestOrganizationId;
        var invalidOrganizationId = Guid.NewGuid();

        var createRequest = new CreateEventRequest
        {
            Title = "Get Event By Slug Test",
            Description = "A test to verify getting an event by its slug.",
            Slug = slug,
            OrganizationId = validOrganizationId,
            PromoterId = TestConstants.TestPromoterId,
            VenueId = TestConstants.TestVenueId,
            EventDate = DateTime.UtcNow.AddDays(30),
            TimeZone = "UTC",
        };

        var createResponse = await _client.PostAsJsonAsync("/api/v1/events", createRequest);
        createResponse.EnsureSuccessStatusCode();

        // Act
        var getResponse = await _client.GetAsync($"/api/v1/events/by-slug/{invalidOrganizationId}/{slug}");

        // Assert
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }



    [Fact]
    public async Task CreateEvent_WithInvalidData_ReturnsBadRequest()
    {
        // Arrange
        var request = new CreateEventRequest
        {
            Title = "Test Event",
            Description = "Test Description",
            Slug = "test-event-invalid",
            OrganizationId = TestConstants.TestOrganizationId,
            PromoterId = TestConstants.TestPromoterId,
            VenueId = TestConstants.TestVenueId,
            EventDate = DateTime.UtcNow.AddDays(-10), // Invalid date
            TimeZone = "UTC",
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/events", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateEvent_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Add("X-Test-Unauthorized", "true");
        var request = new CreateEventRequest
        {
            Title = "Test Event",
            Description = "Test Description",
            Slug = "test-event-unauth",
            OrganizationId = TestConstants.TestOrganizationId,
            PromoterId = TestConstants.TestPromoterId,
            VenueId = TestConstants.TestVenueId,
            EventDate = DateTime.UtcNow.AddDays(10),
            TimeZone = "UTC",
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/events", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }


    [Fact]
    public async Task UpdateEvent_WithValidDataAndVersion_ReturnsOk()
    {
        // Arrange
        var createRequest = new CreateEventRequest
        {
            Title = "Update Test Event",
            Description = "Initial Description",
            Slug = "update-test-event",
            OrganizationId = TestConstants.TestOrganizationId,
            PromoterId = TestConstants.TestPromoterId,
            VenueId = TestConstants.TestVenueId,
            EventDate = DateTime.UtcNow.AddDays(40),
            TimeZone = "UTC",
        };
        var createResponse = await _client.PostAsJsonAsync("/api/v1/events", createRequest);
        var createdEvent = await createResponse.Content.ReadFromJsonAsync<EventDto>();
        await CreateTicketTypeForEvent(createdEvent!.Id);
        createdEvent.Should().NotBeNull();

        var updateRequest = new UpdateEventRequest
        {
            Title = "Updated Test Event Title",
            Description = "Updated Description",
            EventDate = createdEvent.EventDate.AddDays(1)
        };

        var request = new HttpRequestMessage(HttpMethod.Put, $"/api/v1/events/{createdEvent.Id}");
        request.Headers.IfMatch.Add(new System.Net.Http.Headers.EntityTagHeaderValue($"\"{createdEvent.Version}\""));
        request.Content = JsonContent.Create(updateRequest);

        // Act
        var updateResponse = await _client.SendAsync(request);

        // Assert
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updatedEvent = await updateResponse.Content.ReadFromJsonAsync<EventDto>();
        updatedEvent.Should().NotBeNull();
        updatedEvent.Title.Should().Be(updateRequest.Title);
        updatedEvent.Description.Should().Be(updateRequest.Description);
        updatedEvent.Version.Should().Be(createdEvent.Version + 1);
        updateResponse.Headers.ETag.ToString().Should().Be(updatedEvent.Version.ToString());
    }

    [Fact]
    public async Task UpdateEvent_WithInvalidVersion_ReturnsConflict()
    {
        // Arrange
        var createRequest = new CreateEventRequest
        {
            Title = "Conflict Test Event",
            Description = "Initial Description",
            Slug = "conflict-test-event",
            OrganizationId = TestConstants.TestOrganizationId,
            PromoterId = TestConstants.TestPromoterId,
            VenueId = TestConstants.TestVenueId,
            EventDate = DateTime.UtcNow.AddDays(50),
            TimeZone = "UTC",
        };
        var createResponse = await _client.PostAsJsonAsync("/api/v1/events", createRequest);
        var createdEvent = await createResponse.Content.ReadFromJsonAsync<EventDto>();
        createdEvent.Should().NotBeNull();
        await CreateTicketTypeForEvent(createdEvent!.Id);

        var updateRequest = new UpdateEventRequest
        {
            Title = "Updated Title that will fail",
            Description = "Updated Description",
            EventDate = createdEvent.EventDate
        };

        var incorrectVersion = createdEvent.Version + 1;

        var request = new HttpRequestMessage(HttpMethod.Put, $"/api/v1/events/{createdEvent.Id}");
        request.Headers.IfMatch.Add(new System.Net.Http.Headers.EntityTagHeaderValue($"\"{incorrectVersion}\""));
        request.Content = JsonContent.Create(updateRequest);

        // Act
        var updateResponse = await _client.SendAsync(request);

        // Assert
        updateResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task PublishEvent_WithValidVersion_ReturnsOk()
    {
        // Arrange
        var createRequest = new CreateEventRequest
        {
            Title = "Publish Test Event",
            Description = "Test Description",
            Slug = "publish-test-event",
            OrganizationId = TestConstants.TestOrganizationId,
            PromoterId = TestConstants.TestPromoterId,
            VenueId = TestConstants.TestVenueId,
            EventDate = DateTime.UtcNow.AddDays(60),
            TimeZone = "UTC",
        };
        var createResponse = await _client.PostAsJsonAsync("/api/v1/events", createRequest);
        var createdEvent = await createResponse.Content.ReadFromJsonAsync<EventDto>();
        createdEvent.Should().NotBeNull();
        await CreateTicketTypeForEvent(createdEvent!.Id);

        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/events/{createdEvent.Id}/publish");
        request.Headers.IfMatch.Add(new System.Net.Http.Headers.EntityTagHeaderValue($"\"{createdEvent.Version}\""));

        // Act
        var publishResponse = await _client.SendAsync(request);

        // Assert
        publishResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var publishedEvent = await publishResponse.Content.ReadFromJsonAsync<EventDto>();
        publishedEvent.Should().NotBeNull();
        publishedEvent.Status.Should().Be(EventStatus.Published);
        publishedEvent.Version.Should().Be(createdEvent.Version + 1);
    }

    [Fact]
    public async Task CancelEvent_WithValidVersion_ReturnsOk()
    {
        // Arrange
        var createRequest = new CreateEventRequest
        {
            Title = "Cancel Test Event",
            Description = "Test Description",
            Slug = "cancel-test-event",
            OrganizationId = TestConstants.TestOrganizationId,
            PromoterId = TestConstants.TestPromoterId,
            VenueId = TestConstants.TestVenueId,
            EventDate = DateTime.UtcNow.AddDays(70),
            TimeZone = "UTC",
        };
        var createResponse = await _client.PostAsJsonAsync("/api/v1/events", createRequest);
        var createdEvent = await createResponse.Content.ReadFromJsonAsync<EventDto>();
        createdEvent.Should().NotBeNull();
        await CreateTicketTypeForEvent(createdEvent!.Id);

        var cancelRequest = new { Reason = "Test cancellation" };

        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/events/{createdEvent.Id}/cancel");
        request.Headers.IfMatch.Add(new System.Net.Http.Headers.EntityTagHeaderValue($"\"{createdEvent.Version}\""));
        request.Content = JsonContent.Create(cancelRequest);

        // Act
        var cancelResponse = await _client.SendAsync(request);

        // Assert
        cancelResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var cancelledEvent = await cancelResponse.Content.ReadFromJsonAsync<EventDto>();
        cancelledEvent.Should().NotBeNull();
        cancelledEvent.Status.Should().Be(EventStatus.Canceled);
        cancelledEvent.Version.Should().Be(createdEvent.Version + 1);
    }

    [Fact]
    public async Task SearchEvents_WithSearchTerm_ReturnsMatchingEvents()
    {
        // Arrange
        var createRequest1 = new CreateEventRequest
        {
            Title = "Unique Searchable Event One",
            Description = "Description with keyword Alpha",
            Slug = "unique-searchable-event-one",
            OrganizationId = TestConstants.TestOrganizationId,
            PromoterId = TestConstants.TestPromoterId,
            VenueId = TestConstants.TestVenueId,
            EventDate = DateTime.UtcNow.AddDays(80),
            TimeZone = "UTC",
        };
        var createResponse1 = await _client.PostAsJsonAsync("/api/v1/events", createRequest1);
        var event1 = await createResponse1.Content.ReadFromJsonAsync<EventDto>();
        event1.Should().NotBeNull();
        await CreateTicketTypeForEvent(event1!.Id);

        var createRequest2 = new CreateEventRequest
        {
            Title = "Another Event",
            Description = "Description with keyword Unique Searchable Beta",
            Slug = "another-event-beta",
            OrganizationId = TestConstants.TestOrganizationId,
            PromoterId = TestConstants.TestPromoterId,
            VenueId = TestConstants.TestVenueId,
            EventDate = DateTime.UtcNow.AddDays(81),
            TimeZone = "UTC",
        };
        var createResponse2 = await _client.PostAsJsonAsync("/api/v1/events", createRequest2);
        var event2 = await createResponse2.Content.ReadFromJsonAsync<EventDto>();
        event2.Should().NotBeNull();
        await CreateTicketTypeForEvent(event2!.Id);

        var createRequest3 = new CreateEventRequest
        {
            Title = "Third Event",
            Description = "A completely different description",
            Slug = "third-event",
            OrganizationId = TestConstants.TestOrganizationId,
            PromoterId = TestConstants.TestPromoterId,
            VenueId = TestConstants.TestVenueId,
            EventDate = DateTime.UtcNow.AddDays(82),
            TimeZone = "UTC",
        };
        var createResponse3 = await _client.PostAsJsonAsync("/api/v1/events", createRequest3);
        var event3 = await createResponse3.Content.ReadFromJsonAsync<EventDto>();
        event3.Should().NotBeNull();
        await CreateTicketTypeForEvent(event3!.Id);


        // Publish the events to make them searchable
        foreach (var ev in new[] { event1, event2, event3 })
        {
            var publishRequest = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/events/{ev.Id}/publish");
            publishRequest.Headers.IfMatch.Add(new System.Net.Http.Headers.EntityTagHeaderValue($"\"{ev.Version}\""));
            var publishResponse = await _client.SendAsync(publishRequest);
            publishResponse.EnsureSuccessStatusCode();
        }

        // Act
        var response = await _client.GetAsync("/api/v1/events/search?searchTerm=Unique%20Searchable");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var searchResult = await response.Content.ReadFromJsonAsync<PagedResult<EventCatalogDto>>();
        searchResult.Should().NotBeNull();
        searchResult.Items.Should().HaveCount(2);
        searchResult.Items.Should().Contain(e => e.Title.Contains("Unique Searchable Event One"));
        searchResult.Items.Should().Contain(e => e.Description.Contains("Unique Searchable Beta"));
    }

    [Fact]
    public async Task GetEvents_WithPromoterFilter_ReturnsFilteredEvents()
    {
        // Arrange
        var specificPromoterId = TestConstants.SpecificPromoterId;


        var createRequest1 = new CreateEventRequest
        {
            Title = "Event by Specific Promoter",
            Description = "Description",
            Slug = "event-by-specific-promoter",
            OrganizationId = TestConstants.TestOrganizationId,
            PromoterId = specificPromoterId,
            VenueId = TestConstants.TestVenueId,
            EventDate = DateTime.UtcNow.AddDays(90),
            TimeZone = "UTC",
        };
        var createResponse1 = await _client.PostAsJsonAsync("/api/v1/events", createRequest1);
        var event1 = await createResponse1.Content.ReadFromJsonAsync<EventDto>();
        await CreateTicketTypeForEvent(event1!.Id);

        var createRequest2 = new CreateEventRequest
        {
            Title = "Event by Other Promoter",
            Description = "Description",
            Slug = "event-by-other-promoter",
            OrganizationId = TestConstants.TestOrganizationId,
            PromoterId = TestConstants.TestPromoterId, // a different promoter
            VenueId = TestConstants.TestVenueId,
            EventDate = DateTime.UtcNow.AddDays(91),
            TimeZone = "UTC",
        };
        var createResponse2 = await _client.PostAsJsonAsync("/api/v1/events", createRequest2);
        var event2 = await createResponse2.Content.ReadFromJsonAsync<EventDto>();
        await CreateTicketTypeForEvent(event2!.Id);

        // Act
        var response = await _client.GetAsync($"/api/v1/events?promoterId={specificPromoterId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var getResult = await response.Content.ReadFromJsonAsync<PagedResult<EventDto>>();
        getResult.Should().NotBeNull();
        getResult.Items.Should().HaveCount(1);
        getResult.Items.First().Title.Should().Be("Event by Specific Promoter");
    }

    private async Task<TicketTypeDto> CreateTicketTypeForEvent(Guid eventId)
    {
        var request = new CreateTicketTypeCommandRequest
        {
            Name = "General Admission",
            Code = "GA",
            BasePrice = new MoneyDto { Amount = 50, Currency = "USD" },
            InventoryType = InventoryType.GeneralAdmission,
            MinPurchaseQuantity = 1,
            MaxPurchaseQuantity = 10,
        };

                        var response = await _client.PostAsJsonAsync($"/api/v1/tickettypes/events/{eventId}/ticket-types", request);
        response.EnsureSuccessStatusCode();
        var ticketType = await response.Content.ReadFromJsonAsync<TicketTypeDto>();
        return ticketType!;
    }
}