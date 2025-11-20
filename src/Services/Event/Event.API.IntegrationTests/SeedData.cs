using Event.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace Event.API.IntegrationTests;

public static class SeedData
{
    public static void InitializeDbForTests(EventDbContext db)
    {
        db.Database.EnsureCreated();

        // Clear existing data
        db.Venues.RemoveRange(db.Venues);
        db.Events.RemoveRange(db.Events);
        db.EventSeries.RemoveRange(db.EventSeries);
        db.Promoters.RemoveRange(db.Promoters);
        db.Organizations.RemoveRange(db.Organizations);
        db.SaveChanges();

        // Seed Organization
        var organization = new Domain.Entities.Organization
        {
            Id = TestConstants.TestOrganizationId,
            Name = "Test Organization",
            Email = "org@test.com"
        };
        db.Organizations.Add(organization);

        // Seed Promoter
        var promoter = new Domain.Entities.Promoter
        {
            Id = TestConstants.TestPromoterId,
            FirstName = "Test",
            LastName = "Promoter",
            Email = "promoter@test.com"
        };
        db.Promoters.Add(promoter);

        // Seed Specific Promoter for testing
        var specificPromoter = new Domain.Entities.Promoter
        {
            Id = TestConstants.SpecificPromoterId,
            FirstName = "Specific",
            LastName = "Promoter",
            Email = "specific.promoter@test.com"
        };
        db.Promoters.Add(specificPromoter);

        // Seed Venue
        var venue = new Domain.Entities.Venue(
            TestConstants.TestOrganizationId,
            "Test Venue",
            new Domain.ValueObjects.Address("123 Main St", "Test City", "TS", "USA", "12345"),
            Domain.ValueObjects.TimeZoneId.FromString("UTC"),
            1000,
            "A venue for testing purposes.");
        venue.Id = TestConstants.TestVenueId;
        db.Venues.Add(venue);

        db.SaveChanges();
    }
}
