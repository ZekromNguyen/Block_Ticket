using Event.Infrastructure.Persistence;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared.Contracts.Events;

namespace Event.Infrastructure.Consumers;

public class UserUpdatedIntegrationEventConsumer : IConsumer<UserUpdatedIntegrationEvent>
{
    private readonly EventDbContext _context;
    private readonly ILogger<UserUpdatedIntegrationEventConsumer> _logger;

    public UserUpdatedIntegrationEventConsumer(EventDbContext context, ILogger<UserUpdatedIntegrationEventConsumer> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<UserUpdatedIntegrationEvent> context)
    {
        var message = context.Message;

        _logger.LogInformation("Processing UserUpdatedIntegrationEvent for user {UserId}", message.UserId);

        try
        {
            var promoter = await _context.Promoters.FirstOrDefaultAsync(p => p.Id == message.UserId);
            if (promoter != null)
            {
                promoter.FirstName = message.FirstName;
                promoter.LastName = message.LastName;
                promoter.Email = message.Email;
            }
            else
            {
                var organization = await _context.Organizations.FirstOrDefaultAsync(o => o.Id == message.UserId);
                if (organization != null)
                {
                    organization.Name = $"{message.FirstName} {message.LastName}".Trim();
                    organization.Email = message.Email;
                }
            }

            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Successfully processed UserUpdatedIntegrationEvent for user {UserId}", message.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing UserUpdatedIntegrationEvent for user {UserId}", message.UserId);
            throw;
        }
    }
}
