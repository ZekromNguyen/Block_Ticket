using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Builder;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Event.Infrastructure.Persistence;
using System.Linq;
using MassTransit;

using Microsoft.Data.Sqlite;

namespace Event.API.IntegrationTests
{
    public class CustomWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram> where TProgram : class
    {
        private readonly SqliteConnection _connection;

        public CustomWebApplicationFactory()
        {
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                // Remove production DbContext and its options
                var dbContextDescriptors = services.Where(
                    d => d.ServiceType == typeof(DbContextOptions<EventDbContext>) || d.ServiceType == typeof(EventDbContext)).ToList();

                foreach (var descriptor in dbContextDescriptors)
                {
                    services.Remove(descriptor);
                }

                services.AddDbContext<EventDbContext>(options =>
                {
                    options.UseSqlite(_connection);
                });



                // Remove Redis and add in-memory cache
                var servicesToRemove = services.Where(
                    d => d.ServiceType == typeof(StackExchange.Redis.IConnectionMultiplexer) ||
                         d.ServiceType.Name.Contains("Redis", StringComparison.OrdinalIgnoreCase) ||
                         d.ImplementationType?.Name.Contains("Redis", StringComparison.OrdinalIgnoreCase) == true
                ).ToList();

                foreach (var descriptor in servicesToRemove)
                {
                    services.Remove(descriptor);
                }

                                services.AddMemoryCache();

                services.AddSingleton<Event.Application.Interfaces.Infrastructure.IAdvancedCacheService, Event.Infrastructure.Services.InMemoryCacheService>();

                services.AddSingleton<Event.Domain.Interfaces.ICacheService>(sp => sp.GetRequiredService<Event.Application.Interfaces.Infrastructure.IAdvancedCacheService>());
                // Remove Redis-based rate limiting and add in-memory version
                var rateLimitStorageDescriptors = services.Where(
                    d => d.ServiceType == typeof(Event.Infrastructure.Security.RateLimiting.Interfaces.IRateLimitStorage)).ToList();

                foreach (var descriptor in rateLimitStorageDescriptors)
                {
                    services.Remove(descriptor);
                }

                services.AddSingleton<Event.Infrastructure.Security.RateLimiting.Interfaces.IRateLimitStorage, InMemoryRateLimitStorage>();

                // Remove MassTransit and RabbitMQ services and add in-memory test harness
                var massTransitDescriptors = services.Where(
                    d => d.ServiceType.Namespace?.Contains("MassTransit", StringComparison.OrdinalIgnoreCase) == true).ToList();

                foreach (var descriptor in massTransitDescriptors)
                {
                    services.Remove(descriptor);
                }

                services.AddMassTransitTestHarness();

                services.AddAuthentication("Test")
                    .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, TestAuthHandler>("Test", options => { });


            });






            builder.UseEnvironment("Testing");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _connection.Close();
                _connection.Dispose();
            }
            base.Dispose(disposing);
        }


    }
}
