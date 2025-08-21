using Event.Application.Interfaces.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Event.Infrastructure.Services;

/// <summary>
/// Background service for processing approval workflow tasks
/// </summary>
public class ApprovalWorkflowBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ApprovalWorkflowBackgroundService> _logger;
    private readonly TimeSpan _processingInterval = TimeSpan.FromMinutes(5);
    private readonly TimeSpan _escalationInterval = TimeSpan.FromHours(1);

    public ApprovalWorkflowBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<ApprovalWorkflowBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Approval workflow background service started");

        var lastEscalationCheck = DateTime.UtcNow;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var approvalService = scope.ServiceProvider.GetRequiredService<IApprovalWorkflowService>();

                // Process expired workflows every cycle
                await ProcessExpiredWorkflowsAsync(approvalService, stoppingToken);

                // Process escalations less frequently
                if (DateTime.UtcNow - lastEscalationCheck >= _escalationInterval)
                {
                    await ProcessEscalationsAsync(approvalService, stoppingToken);
                    lastEscalationCheck = DateTime.UtcNow;
                }

                // Wait for next processing cycle
                await Task.Delay(_processingInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is requested
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during approval workflow background processing");
                
                // Wait a shorter time on error to avoid rapid retries
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }

        _logger.LogInformation("Approval workflow background service stopped");
    }

    private async Task ProcessExpiredWorkflowsAsync(IApprovalWorkflowService approvalService, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Processing expired approval workflows");
            await approvalService.ProcessExpiredWorkflowsAsync(cancellationToken);
            _logger.LogDebug("Completed processing expired approval workflows");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing expired approval workflows");
        }
    }

    private async Task ProcessEscalationsAsync(IApprovalWorkflowService approvalService, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Processing approval workflow escalations");
            await approvalService.ProcessEscalationsAsync(cancellationToken);
            _logger.LogDebug("Completed processing approval workflow escalations");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing approval workflow escalations");
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Approval workflow background service is stopping");
        await base.StopAsync(cancellationToken);
    }
}
