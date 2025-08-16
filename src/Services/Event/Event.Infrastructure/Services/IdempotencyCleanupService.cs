using Event.Domain.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Event.Infrastructure.Services;

/// <summary>
/// Background service for cleaning up expired idempotency records
/// </summary>
public class IdempotencyCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<IdempotencyCleanupService> _logger;
    private readonly TimeSpan _cleanupInterval;

    public IdempotencyCleanupService(
        IServiceProvider serviceProvider,
        ILogger<IdempotencyCleanupService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _cleanupInterval = TimeSpan.FromHours(1); // Run cleanup every hour
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Idempotency cleanup service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_cleanupInterval, stoppingToken);
                
                using var scope = _serviceProvider.CreateScope();
                var idempotencyService = scope.ServiceProvider.GetRequiredService<IIdempotencyService>();
                
                var removedCount = await idempotencyService.CleanupExpiredRecordsAsync(stoppingToken);
                
                if (removedCount > 0)
                {
                    _logger.LogInformation("Cleaned up {Count} expired idempotency records", removedCount);
                }
                else
                {
                    _logger.LogDebug("No expired idempotency records to clean up");
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during idempotency cleanup");
            }
        }

        _logger.LogInformation("Idempotency cleanup service stopped");
    }
}
