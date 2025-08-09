using Identity.Domain.Repositories;
using Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Identity.Infrastructure.Services;

public class AuditLogCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AuditLogCleanupService> _logger;
    private readonly IConfiguration _configuration;
    private readonly TimeSpan _cleanupInterval = TimeSpan.FromDays(1); // Run daily

    public AuditLogCleanupService(
        IServiceProvider serviceProvider, 
        ILogger<AuditLogCleanupService> logger,
        IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Audit log cleanup service started");

        // Wait for initial delay to avoid startup conflicts
        await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanupOldAuditLogsAsync(stoppingToken);
                await Task.Delay(_cleanupInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is requested
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during audit log cleanup");
                // Wait a bit before retrying
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }

        _logger.LogInformation("Audit log cleanup service stopped");
    }

    private async Task CleanupOldAuditLogsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();

        try
        {
            // Get retention period from configuration (default: 90 days)
            var retentionDays = _configuration.GetValue<int>("AuditLog:RetentionDays", 90);
            var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);

            _logger.LogDebug("Starting audit log cleanup for logs older than {CutoffDate}", cutoffDate);

            // Delete old audit logs in batches to avoid long-running transactions
            const int batchSize = 1000;
            int totalDeleted = 0;

            while (!cancellationToken.IsCancellationRequested)
            {
                var oldLogs = await context.AuditLogs
                    .Where(a => a.CreatedAt < cutoffDate)
                    .Take(batchSize)
                    .ToListAsync(cancellationToken);

                if (!oldLogs.Any())
                    break;

                context.AuditLogs.RemoveRange(oldLogs);
                await context.SaveChangesAsync(cancellationToken);

                totalDeleted += oldLogs.Count;
                _logger.LogDebug("Deleted batch of {Count} audit logs", oldLogs.Count);

                // Small delay between batches to reduce database load
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            }

            if (totalDeleted > 0)
            {
                _logger.LogInformation("Cleaned up {Count} old audit logs older than {Days} days", 
                    totalDeleted, retentionDays);
            }
            else
            {
                _logger.LogDebug("No old audit logs found for cleanup");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during audit log cleanup");
            throw;
        }
    }
}
