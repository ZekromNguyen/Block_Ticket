using Identity.Domain.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Identity.Infrastructure.Services;

public class SessionCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SessionCleanupService> _logger;
    private readonly TimeSpan _cleanupInterval = TimeSpan.FromHours(1); // Run every hour

    public SessionCleanupService(IServiceProvider serviceProvider, ILogger<SessionCleanupService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Session cleanup service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanupExpiredSessionsAsync(stoppingToken);
                await Task.Delay(_cleanupInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is requested
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during session cleanup");
                // Wait a bit before retrying
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }

        _logger.LogInformation("Session cleanup service stopped");
    }

    private async Task CleanupExpiredSessionsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var sessionRepository = scope.ServiceProvider.GetRequiredService<IUserSessionRepository>();

        try
        {
            _logger.LogDebug("Starting expired session cleanup");
            
            var expiredSessions = await sessionRepository.GetExpiredSessionsAsync(cancellationToken);
            var expiredCount = expiredSessions.Count();

            if (expiredCount > 0)
            {
                await sessionRepository.DeleteExpiredSessionsAsync(cancellationToken);
                _logger.LogInformation("Cleaned up {Count} expired sessions", expiredCount);
            }
            else
            {
                _logger.LogDebug("No expired sessions found");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during expired session cleanup");
            throw;
        }
    }
}
