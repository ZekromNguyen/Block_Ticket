using Identity.Domain.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Identity.Infrastructure.Services;

/// <summary>
/// Background service for sending scheduled security notifications and summaries
/// </summary>
public class SecurityNotificationSchedulerService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SecurityNotificationSchedulerService> _logger;
    private readonly IConfiguration _configuration;
    private readonly TimeSpan _summaryInterval;
    private readonly TimeOnly _summaryTime;

    public SecurityNotificationSchedulerService(
        IServiceProvider serviceProvider,
        ILogger<SecurityNotificationSchedulerService> logger,
        IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _configuration = configuration;
        
        // Default to daily summaries at 9:00 AM
        _summaryInterval = TimeSpan.FromDays(1);
        _summaryTime = TimeOnly.Parse(configuration["Notifications:DailySummaryTime"] ?? "09:00");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Security Notification Scheduler Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var now = DateTime.UtcNow;
                var nextSummaryTime = GetNextSummaryTime(now);
                
                _logger.LogDebug("Next security summary scheduled for {NextSummaryTime}", nextSummaryTime);

                var delay = nextSummaryTime - now;
                if (delay > TimeSpan.Zero)
                {
                    await Task.Delay(delay, stoppingToken);
                }

                if (!stoppingToken.IsCancellationRequested)
                {
                    await SendDailySummaryAsync(stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Security Notification Scheduler Service stopped");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Security Notification Scheduler Service");
                
                // Wait a bit before retrying to avoid tight error loops
                try
                {
                    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
            }
        }
    }

    private async Task SendDailySummaryAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var notificationService = scope.ServiceProvider.GetRequiredService<ISecurityNotificationService>();

            var now = DateTime.UtcNow;
            var from = now.Date.AddDays(-1); // Previous day
            var to = now.Date; // Start of current day

            _logger.LogInformation("Sending daily security summary for period {From} to {To}", from, to);

            await notificationService.SendSecuritySummaryAsync(from, to, cancellationToken);

            _logger.LogInformation("Daily security summary sent successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending daily security summary");
        }
    }

    private DateTime GetNextSummaryTime(DateTime current)
    {
        var today = current.Date;
        var todaySummaryTime = today.Add(_summaryTime.ToTimeSpan());

        // If we've already passed today's summary time, schedule for tomorrow
        if (current >= todaySummaryTime)
        {
            return todaySummaryTime.Add(_summaryInterval);
        }

        return todaySummaryTime;
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Security Notification Scheduler Service is stopping");
        await base.StopAsync(cancellationToken);
    }
}
