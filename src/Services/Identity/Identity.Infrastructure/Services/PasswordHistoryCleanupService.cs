using Identity.Domain.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Identity.Infrastructure.Services;

public class PasswordHistoryCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<PasswordHistoryCleanupService> _logger;
    private readonly TimeSpan _cleanupInterval = TimeSpan.FromDays(1); // Run daily

    public PasswordHistoryCleanupService(
        IServiceProvider serviceProvider,
        ILogger<PasswordHistoryCleanupService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Password History Cleanup Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PerformCleanupAsync(stoppingToken);
                await Task.Delay(_cleanupInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Expected when the service is stopping
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during password history cleanup");
                // Wait a shorter time before retrying on error
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }

        _logger.LogInformation("Password History Cleanup Service stopped");
    }

    private async Task PerformCleanupAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var passwordHistoryService = scope.ServiceProvider.GetRequiredService<IPasswordHistoryService>();

        _logger.LogDebug("Starting scheduled password history cleanup");

        await passwordHistoryService.CleanupAllPasswordHistoryAsync(cancellationToken);

        _logger.LogInformation("Completed scheduled password history cleanup");
    }
}
