using System.Net;
using System.Net.Http.Json;

using Event.Application.Common.Models;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

using Event.Infrastructure.Persistence;

namespace Event.API.IntegrationTests;

public class EventSeriesControllerTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory<Program> _factory;

    public EventSeriesControllerTests(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();


        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<EventDbContext>();
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();
        SeedData.InitializeDbForTests(context);






    }

    [Fact]
    public async Task CreateEventSeries_WithValidData_ReturnsCreated()
    {
        // Arrange
        var request = new CreateEventSeriesRequest
        {
            Name = "Test Event Series",
            Description = "A series of test events.",
            Slug = "test-event-series",
            OrganizationId = TestConstants.TestOrganizationId,
            PromoterId = TestConstants.TestPromoterId,
            Categories = new List<string> { "Music", "Live" },
            Tags = new List<string> { "test", "integration" }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/eventseries", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdSeries = await response.Content.ReadFromJsonAsync<EventSeriesDto>();
        createdSeries.Should().NotBeNull();
        createdSeries.Name.Should().Be(request.Name);
        createdSeries.Description.Should().Be(request.Description);
        createdSeries.OrganizationId.Should().Be(request.OrganizationId);
    }

    [Fact]
    public async Task GetEventSeries_WithExistingId_ReturnsEventSeries()
    {
        // Arrange
        var createRequest = new CreateEventSeriesRequest
        {
            Name = "Get Test Event Series",
            Description = "A test to verify getting an event series by its ID.",
            Slug = "get-test-event-series",
            OrganizationId = TestConstants.TestOrganizationId,
            PromoterId = TestConstants.TestPromoterId
        };

        var createResponse = await _client.PostAsJsonAsync("/api/v1/eventseries", createRequest);
        var createdSeries = await createResponse.Content.ReadFromJsonAsync<EventSeriesDto>();
        createdSeries.Should().NotBeNull();

        // Act
        var getResponse = await _client.GetAsync($"/api/v1/eventseries/{createdSeries.Id}");

        // Assert
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var fetchedSeries = await getResponse.Content.ReadFromJsonAsync<EventSeriesDto>();
        fetchedSeries.Should().NotBeNull();
        fetchedSeries.Id.Should().Be(createdSeries.Id);
        fetchedSeries.Name.Should().Be(createRequest.Name);
    }

    [Fact]
    public async Task UpdateEventSeries_WithValidData_ReturnsOk()
    {
        // Arrange
        var createRequest = new CreateEventSeriesRequest
        {
            Name = "Original Name",
            Description = "Original Description",
            Slug = "update-test-series",
            OrganizationId = TestConstants.TestOrganizationId,
            PromoterId = TestConstants.TestPromoterId
        };
        var createResponse = await _client.PostAsJsonAsync("/api/v1/eventseries", createRequest);
        var createdSeries = await createResponse.Content.ReadFromJsonAsync<EventSeriesDto>();
        createdSeries.Should().NotBeNull();

        var updateRequest = new UpdateEventSeriesRequest
        {
            Name = "Updated Name",
            Description = "Updated Description"
        };

        // Act
        var requestMessage = new HttpRequestMessage(HttpMethod.Put, $"/api/v1/eventseries/{createdSeries.Id}");
                requestMessage.Headers.TryAddWithoutValidation("If-Match", createdSeries.Version.ToString());
        requestMessage.Content = JsonContent.Create(updateRequest);
        var updateResponse = await _client.SendAsync(requestMessage);

        // Assert
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var getResponse = await _client.GetAsync($"/api/v1/eventseries/{createdSeries.Id}");
        var updatedSeries = await getResponse.Content.ReadFromJsonAsync<EventSeriesDto>();
        updatedSeries.Should().NotBeNull();
        updatedSeries.Name.Should().Be(updateRequest.Name);
        updatedSeries.Description.Should().Be(updateRequest.Description);
    }

    [Fact]
    public async Task DeleteEventSeries_WithExistingId_ReturnsNoContent()
    {
        // Arrange
        var createRequest = new CreateEventSeriesRequest
        {
            Name = "To Be Deleted",
            Slug = "delete-test-series",
            OrganizationId = TestConstants.TestOrganizationId,
            PromoterId = TestConstants.TestPromoterId
        };
        var createResponse = await _client.PostAsJsonAsync("/api/v1/eventseries", createRequest);
        var createdSeries = await createResponse.Content.ReadFromJsonAsync<EventSeriesDto>();
        createdSeries.Should().NotBeNull();

        // Act
        var requestMessage = new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/eventseries/{createdSeries.Id}");
        requestMessage.Headers.TryAddWithoutValidation("If-Match", createdSeries.Version.ToString());
        var deleteResponse = await _client.SendAsync(requestMessage);

        // Assert
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getResponse = await _client.GetAsync($"/api/v1/eventseries/{createdSeries.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ActivateEventSeries_WithExistingId_ReturnsOkAndActivatesSeries()
    {
        // Arrange
        var createRequest = new CreateEventSeriesRequest
        {
            Name = "To Be Activated",
            Slug = "activate-test-series",
            OrganizationId = TestConstants.TestOrganizationId,
            PromoterId = TestConstants.TestPromoterId
        };
        var createResponse = await _client.PostAsJsonAsync("/api/v1/eventseries", createRequest);
        var createdSeries = await createResponse.Content.ReadFromJsonAsync<EventSeriesDto>();
        createdSeries.Should().NotBeNull();
        createdSeries.IsActive.Should().BeFalse(); // Should be inactive by default

        // Act
        var requestMessage = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/eventseries/{createdSeries.Id}/activate");
        requestMessage.Headers.TryAddWithoutValidation("If-Match", createdSeries.Version.ToString());
        var activateResponse = await _client.SendAsync(requestMessage);

        // Assert
        activateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var activatedSeries = await activateResponse.Content.ReadFromJsonAsync<EventSeriesDto>();
        activatedSeries.Should().NotBeNull();
        activatedSeries.IsActive.Should().BeTrue();

        // Verify by getting the series again
        var getResponse = await _client.GetAsync($"/api/v1/eventseries/{createdSeries.Id}");
        var fetchedSeries = await getResponse.Content.ReadFromJsonAsync<EventSeriesDto>();
        fetchedSeries.Should().NotBeNull();
        fetchedSeries.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task DeactivateEventSeries_WithExistingId_ReturnsOkAndDeactivatesSeries()
    {
        // Arrange
        var createRequest = new CreateEventSeriesRequest
        {
            Name = "To Be Deactivated",
            Slug = "deactivate-test-series",
            OrganizationId = TestConstants.TestOrganizationId,
            PromoterId = TestConstants.TestPromoterId
        };
        var createResponse = await _client.PostAsJsonAsync("/api/v1/eventseries", createRequest);
        var createdSeries = await createResponse.Content.ReadFromJsonAsync<EventSeriesDto>();
        createdSeries.Should().NotBeNull();

        // Pre-activate the series
        var activateRequest = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/eventseries/{createdSeries.Id}/activate");
        activateRequest.Headers.TryAddWithoutValidation("If-Match", createdSeries.Version.ToString());
        var activateResponse = await _client.SendAsync(activateRequest);
        var activatedSeries = await activateResponse.Content.ReadFromJsonAsync<EventSeriesDto>();
        activatedSeries.Should().NotBeNull();
        activatedSeries.IsActive.Should().BeTrue();

        // Act
        var requestMessage = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/eventseries/{activatedSeries.Id}/deactivate");
        requestMessage.Headers.TryAddWithoutValidation("If-Match", activatedSeries.Version.ToString());
        var deactivateResponse = await _client.SendAsync(requestMessage);

        // Assert
        deactivateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var deactivatedSeries = await deactivateResponse.Content.ReadFromJsonAsync<EventSeriesDto>();
        deactivatedSeries.Should().NotBeNull();
        deactivatedSeries.IsActive.Should().BeFalse();

        // Verify by getting the series again
        var getResponse = await _client.GetAsync($"/api/v1/eventseries/{createdSeries.Id}");
        var fetchedSeries = await getResponse.Content.ReadFromJsonAsync<EventSeriesDto>();
        fetchedSeries.Should().NotBeNull();
        fetchedSeries.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task GetEventSeriesBySlug_WithExistingSlug_ReturnsOk()
    {
        // Arrange
        var slug = "get-by-slug-test-series";
        var createRequest = new CreateEventSeriesRequest
        {
            Name = "Get By Slug Test Series",
            Slug = slug,
            OrganizationId = TestConstants.TestOrganizationId,
            PromoterId = TestConstants.TestPromoterId
        };
        var createResponse = await _client.PostAsJsonAsync("/api/v1/eventseries", createRequest);
        var createdSeries = await createResponse.Content.ReadFromJsonAsync<EventSeriesDto>();
        createdSeries.Should().NotBeNull();

        // Act
        var getResponse = await _client.GetAsync($"/api/v1/eventseries/by-slug/{TestConstants.TestOrganizationId}/{slug}");

        // Assert
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var fetchedSeries = await getResponse.Content.ReadFromJsonAsync<EventSeriesDto>();
        fetchedSeries.Should().NotBeNull();
        fetchedSeries.Id.Should().Be(createdSeries.Id);
        fetchedSeries.Slug.ToString().Should().Be(slug);
    }


    [Fact]
    public async Task GetEventSeriesList_WithExistingSeries_ReturnsOk()
    {
        // Arrange
        var createRequest1 = new CreateEventSeriesRequest
        {
            Name = "Event Series 1",
            Slug = "event-series-1",
            OrganizationId = TestConstants.TestOrganizationId,
            PromoterId = TestConstants.TestPromoterId
        };
        var createRequest2 = new CreateEventSeriesRequest
        {
            Name = "Event Series 2",
            Slug = "event-series-2",
            OrganizationId = TestConstants.TestOrganizationId,
            PromoterId = TestConstants.TestPromoterId
        };
        await _client.PostAsJsonAsync("/api/v1/eventseries", createRequest1);
        await _client.PostAsJsonAsync("/api/v1/eventseries", createRequest2);

        // Act
        var response = await _client.GetAsync($"/api/v1/eventseries?PromoterId={TestConstants.TestPromoterId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var pagedResult = await response.Content.ReadFromJsonAsync<PagedResult<EventSeriesDto>>();
        pagedResult.Should().NotBeNull();
        pagedResult.Items.Should().HaveCount(2);
    }





}

