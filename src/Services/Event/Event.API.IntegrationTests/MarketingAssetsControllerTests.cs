using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Event.Application.DTOs;
using Event.Application.Features.MarketingAssets.Commands;
using FluentAssertions;
using Xunit;
using System.IO;
using Microsoft.Extensions.DependencyInjection;

namespace Event.API.IntegrationTests
{
    public class MarketingAssetsControllerTests : IClassFixture<CustomWebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;

        public MarketingAssetsControllerTests(CustomWebApplicationFactory<Program> factory)
        {
            _client = factory.CreateClient();

            using var scope = factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<Event.Infrastructure.Persistence.EventDbContext>();

            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();
            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();
            SeedData.InitializeDbForTests(context);




        }

        [Fact]
        public async Task CreateMarketingAsset_WithValidData_ReturnsCreated()
        {
            // Arrange
            var fileStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("dummy file content"));

            var formData = new MultipartFormDataContent();
            formData.Add(new StringContent("Test Asset"), "Name");
            formData.Add(new StringContent("Test Description"), "Description");
            formData.Add(new StringContent("Image"), "Type");
            var fileContent = new StreamContent(fileStream);
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
            formData.Add(fileContent, "File", "test.jpg");
            formData.Add(new StringContent(TestConstants.TestOrganizationId.ToString()), "OrganizationId");

            // Act
            var response = await _client.PostAsync("/api/v1/marketingassets", formData);

            // Assert
            if (response.StatusCode != HttpStatusCode.Created)
            {
                var error = await response.Content.ReadAsStringAsync();
                Assert.True(false, $"Expected status code 201 but got {(int)response.StatusCode}. Response: {error}");
            }

            response.StatusCode.Should().Be(HttpStatusCode.Created);
            var createdAsset = await response.Content.ReadFromJsonAsync<MarketingAssetDto>();
            createdAsset.Should().NotBeNull();
            createdAsset.Name.Should().Be("Test Asset");
            response.Headers.Location.Should().NotBeNull();
            response.Headers.Location.ToString().Should().Contain($"/api/v1.0/marketingassets/{createdAsset.Id}");
        }
    }
}
