using Identity.Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Identity.Infrastructure.Extensions;

public static class SeedDataExtensions
{
    /// <summary>
    /// Seeds the database with initial data including permissions, roles, scopes, OAuth clients, and default users.
    /// This should be called during application startup in development/staging environments.
    /// </summary>
    /// <param name="host">The application host</param>
    /// <returns>The host for method chaining</returns>
    public static async Task<IHost> SeedDatabaseAsync(this IHost host)
    {
        using var scope = host.Services.CreateScope();
        var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger("SeedDataExtensions");

        try
        {
            logger.LogInformation("Starting database seed operation...");
            await SeedData.InitializeAsync(scope.ServiceProvider);
            logger.LogInformation("Database seed operation completed successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while seeding the database");
            throw;
        }

        return host;
    }

    /// <summary>
    /// Seeds the database with initial data. This is a synchronous version for use in Program.cs
    /// </summary>
    /// <param name="serviceProvider">The service provider</param>
    public static void SeedDatabase(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger("SeedDataExtensions");

        try
        {
            logger.LogInformation("Starting database seed operation...");
            SeedData.InitializeAsync(scope.ServiceProvider).GetAwaiter().GetResult();
            logger.LogInformation("Database seed operation completed successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while seeding the database");
            throw;
        }
    }
}
