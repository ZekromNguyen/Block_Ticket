using Event.Application.Common.Interfaces;
using Event.Domain.Interfaces;
using Event.Infrastructure.Persistence;
using Event.Infrastructure.Persistence.Interceptors;
using Event.Infrastructure.Persistence.Repositories;
using Event.Infrastructure.Services;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Event.Infrastructure.Configuration;

/// <summary>
/// Infrastructure layer service registration
/// </summary>
public static class InfrastructureServiceRegistration
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, 
        IConfiguration configuration, 
        IHostEnvironment environment)
    {
        // Add Entity Framework
        AddEntityFramework(services, configuration);

        // Add Redis
        AddRedis(services, configuration);

        // Add Repositories
        AddRepositories(services);

        // Add Domain Services
        AddDomainServices(services);

        // Add Infrastructure Services
        AddInfrastructureServices(services);

        // Add MassTransit (Message Bus)
        AddMassTransit(services, configuration, environment);

        // Add Background Services
        AddBackgroundServices(services);

        return services;
    }

    private static void AddEntityFramework(IServiceCollection services, IConfiguration configuration)
    {
        // Add audit interceptor
        services.AddScoped<AuditInterceptor>();

        // Add DbContext
        services.AddDbContext<EventDbContext>((serviceProvider, options) =>
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.MigrationsAssembly(typeof(EventDbContext).Assembly.FullName);
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorCodesToAdd: null);
                npgsqlOptions.CommandTimeout(30);
            });

            // Enable sensitive data logging in development
            if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
            {
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
            }

            // Add logging
            options.UseLoggerFactory(serviceProvider.GetRequiredService<ILoggerFactory>());
        });

        // Add health checks for database
        services.AddHealthChecks()
            .AddNpgSql(configuration.GetConnectionString("DefaultConnection")!, "database");
    }

    private static void AddRedis(IServiceCollection services, IConfiguration configuration)
    {
        var redisConnectionString = configuration.GetConnectionString("Redis");
        
        if (!string.IsNullOrEmpty(redisConnectionString))
        {
            services.AddSingleton<IConnectionMultiplexer>(provider =>
            {
                var configurationOptions = ConfigurationOptions.Parse(redisConnectionString);
                configurationOptions.AbortOnConnectFail = false;
                configurationOptions.ConnectRetry = 3;
                configurationOptions.ConnectTimeout = 5000;
                configurationOptions.SyncTimeout = 5000;
                
                return ConnectionMultiplexer.Connect(configurationOptions);
            });

            services.AddScoped<ICacheService, RedisCacheService>();

            // Add health checks for Redis
            services.AddHealthChecks()
                .AddRedis(redisConnectionString, "redis");
        }
        else
        {
            // Fallback to in-memory cache if Redis is not configured
            services.AddMemoryCache();
            services.AddScoped<ICacheService, InMemoryCacheService>();
        }
    }

    private static void AddRepositories(IServiceCollection services)
    {
        services.AddScoped<IEventRepository, EventRepository>();
        services.AddScoped<IVenueRepository, VenueRepository>();
        services.AddScoped<IReservationRepository, ReservationRepository>();
        services.AddScoped<IPricingRuleRepository, Persistence.Repositories.PricingRuleRepository>();
        services.AddScoped<IEventSeriesRepository, EventSeriesRepository>();

        // Add Unit of Work
        services.AddScoped<IUnitOfWork, Persistence.UnitOfWork>();

        // Register application services
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, Services.CurrentUserService>();
        services.AddScoped<IIntegrationEventPublisher, Services.IntegrationEventPublisher>();
        services.AddScoped<INotificationService, Services.NotificationService>();
        services.AddScoped<Event.Application.Services.IPricingValidationService, Event.Application.Services.PricingValidationService>();

        // Register organization context and RLS services
        services.AddScoped<Event.Application.Common.Interfaces.IOrganizationContextProvider, Services.OrganizationContextProvider>();
        services.AddScoped<Middleware.IPostgreSqlSessionManager, Middleware.PostgreSqlSessionManager>();
    }

    private static void AddDomainServices(IServiceCollection services)
    {
        services.AddScoped<ISeatLockService, SeatLockService>();
        services.AddScoped<IInventorySnapshotService, InventorySnapshotService>();
        services.AddScoped<IPricingEngineService, Services.PricingEngineService>();
    }

    private static void AddInfrastructureServices(IServiceCollection services)
    {
        // Add other infrastructure services as needed
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<IFileStorageService, FileStorageService>();
    }

    private static void AddMassTransit(IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        services.AddMassTransit(x =>
        {
            // Add consumers
            x.AddConsumersFromNamespaceContaining<EventCreatedConsumer>();

            // Add integration event consumers
            x.AddConsumer<Messaging.Consumers.OrderPaymentAuthorizedConsumer>();
            x.AddConsumer<Messaging.Consumers.OrderPaymentCompletedConsumer>();
            x.AddConsumer<Messaging.Consumers.OrderPaymentFailedConsumer>();
            x.AddConsumer<Messaging.Consumers.OrderCancelledConsumer>();
            x.AddConsumer<Messaging.Consumers.RefundRequestedConsumer>();
            x.AddConsumer<Messaging.Consumers.RefundProcessedConsumer>();
            x.AddConsumer<Messaging.Consumers.TicketResaleListedConsumer>();
            x.AddConsumer<Messaging.Consumers.TicketResaleSoldConsumer>();
            x.AddConsumer<Messaging.Consumers.UserPreferencesUpdatedConsumer>();

            x.UsingRabbitMq((context, cfg) =>
            {
                var rabbitMqSettings = configuration.GetSection("RabbitMQ");
                var host = rabbitMqSettings["Host"] ?? "localhost";
                var username = rabbitMqSettings["Username"] ?? "guest";
                var password = rabbitMqSettings["Password"] ?? "guest";

                cfg.Host(host, "/", h =>
                {
                    h.Username(username);
                    h.Password(password);
                });

                // Configure retry policy
                cfg.UseMessageRetry(r => r.Exponential(
                    retryLimit: 3,
                    minInterval: TimeSpan.FromSeconds(1),
                    maxInterval: TimeSpan.FromSeconds(30),
                    intervalDelta: TimeSpan.FromSeconds(1)));

                // Configure message topology and error handling
                Messaging.MessagingConfiguration.ConfigureMessageTopology(cfg);
                Messaging.MessagingConfiguration.ConfigureErrorHandling(cfg);

                // Configure endpoints
                cfg.ConfigureEndpoints(context);
            });
        });

        // Add health checks for RabbitMQ
        var rabbitMqConnectionString = configuration.GetConnectionString("RabbitMQ");
        if (!string.IsNullOrEmpty(rabbitMqConnectionString))
        {
            services.AddHealthChecks()
                .AddRabbitMQ(rabbitMqConnectionString, name: "rabbitmq");
        }
    }

    private static void AddBackgroundServices(IServiceCollection services)
    {
        services.AddHostedService<ReservationCleanupService>();
        services.AddHostedService<EventStatusUpdateService>();
        services.AddHostedService<CacheWarmupService>();
    }
}

// Placeholder services that would need to be implemented
public class ReservationRepository : BaseRepository<Domain.Entities.Reservation>, IReservationRepository
{
    public ReservationRepository(EventDbContext context) : base(context) { }

    public Task<IEnumerable<Domain.Entities.Reservation>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<Domain.Entities.Reservation>> GetByEventIdAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<Domain.Entities.Reservation>> GetExpiredReservationsAsync(DateTime cutoffTime, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<Domain.Entities.Reservation?> GetActiveReservationAsync(Guid userId, Guid eventId, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<bool> HasActiveReservationForSeatsAsync(List<Guid> seatIds, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    // UpdateAsync, DeleteAsync, ExistsAsync inherited from BaseRepository
}

// PricingRuleRepository is now implemented in Repositories/PricingRuleRepository.cs



// Placeholder services
public class InMemoryCacheService : ICacheService
{
    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class => throw new NotImplementedException();
    public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class => throw new NotImplementedException();
    public Task RemoveAsync(string key, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default) => throw new NotImplementedException();
}

public class SeatLockService : ISeatLockService
{
    public Task<bool> TryLockSeatsAsync(List<Guid> seatIds, Guid reservationId, TimeSpan lockDuration, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task ReleaseSeatLocksAsync(List<Guid> seatIds, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task ExtendSeatLocksAsync(List<Guid> seatIds, TimeSpan additionalTime, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task<bool> AreSeatsLockedAsync(List<Guid> seatIds, CancellationToken cancellationToken = default) => throw new NotImplementedException();
}

public class InventorySnapshotService : IInventorySnapshotService
{
    public Task<string> GetInventoryETagAsync(Guid eventId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task InvalidateInventoryETagAsync(Guid eventId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task<Dictionary<Guid, int>> GetAvailabilitySnapshotAsync(Guid eventId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
}

// PricingEngineService is now implemented in Services/PricingEngineService.cs

public class EmailService : IEmailService
{
    public Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken = default) => throw new NotImplementedException();
}

public class FileStorageService : IFileStorageService
{
    public Task<string> UploadAsync(Stream stream, string fileName, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task DeleteAsync(string fileName, CancellationToken cancellationToken = default) => throw new NotImplementedException();
}

public class EventCreatedConsumer : IConsumer<object>
{
    private readonly ILogger<EventCreatedConsumer> _logger;

    public EventCreatedConsumer(ILogger<EventCreatedConsumer> logger)
    {
        _logger = logger;
    }

    public Task Consume(ConsumeContext<object> context)
    {
        _logger.LogInformation("Event created message received: {MessageType}", context.Message.GetType().Name);
        return Task.CompletedTask;
    }
}

public class ReservationCleanupService : BackgroundService
{
    private readonly ILogger<ReservationCleanupService> _logger;
    private readonly IServiceProvider _serviceProvider;

    public ReservationCleanupService(ILogger<ReservationCleanupService> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Reservation cleanup service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogDebug("Running reservation cleanup task");

                // TODO: Implement actual cleanup logic
                // This would typically:
                // 1. Find expired reservations
                // 2. Release reserved seats
                // 3. Update reservation status

                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is requested
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during reservation cleanup");
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }

        _logger.LogInformation("Reservation cleanup service stopped");
    }
}

public class EventStatusUpdateService : BackgroundService
{
    private readonly ILogger<EventStatusUpdateService> _logger;
    private readonly IServiceProvider _serviceProvider;

    public EventStatusUpdateService(ILogger<EventStatusUpdateService> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Event status update service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogDebug("Running event status update task");

                // TODO: Implement actual status update logic
                // This would typically:
                // 1. Check for events that should transition status
                // 2. Update event status based on dates/conditions
                // 3. Publish status change events

                await Task.Delay(TimeSpan.FromMinutes(10), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is requested
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during event status update");
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }

        _logger.LogInformation("Event status update service stopped");
    }
}

public class CacheWarmupService : BackgroundService
{
    private readonly ILogger<CacheWarmupService> _logger;
    private readonly IServiceProvider _serviceProvider;

    public CacheWarmupService(ILogger<CacheWarmupService> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Cache warmup service started");

        // Initial warmup
        try
        {
            _logger.LogInformation("Performing initial cache warmup");

            // TODO: Implement actual cache warmup logic
            // This would typically:
            // 1. Pre-load frequently accessed data
            // 2. Warm up Redis cache
            // 3. Pre-compute expensive queries

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            _logger.LogInformation("Initial cache warmup completed");
        }
        catch (OperationCanceledException)
        {
            return;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during initial cache warmup");
        }

        // Periodic warmup
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogDebug("Running periodic cache warmup");

                // TODO: Implement periodic cache refresh logic

                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during periodic cache warmup");
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }

        _logger.LogInformation("Cache warmup service stopped");
    }
}

public interface IEmailService
{
    Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken = default);
}

public interface IFileStorageService
{
    Task<string> UploadAsync(Stream stream, string fileName, CancellationToken cancellationToken = default);
    Task DeleteAsync(string fileName, CancellationToken cancellationToken = default);
}
