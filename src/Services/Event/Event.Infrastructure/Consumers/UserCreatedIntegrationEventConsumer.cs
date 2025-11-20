using Event.Domain.Entities;
using Event.Infrastructure.Persistence;
using MassTransit;
using Microsoft.Extensions.Logging;
using Shared.Contracts.Events;

namespace Event.Infrastructure.Consumers;

public class UserCreatedIntegrationEventConsumer : IConsumer<UserCreatedIntegrationEvent>
{
    private readonly EventDbContext _context;
    private readonly ILogger<UserCreatedIntegrationEventConsumer> _logger;

    public UserCreatedIntegrationEventConsumer(EventDbContext context, ILogger<UserCreatedIntegrationEventConsumer> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<UserCreatedIntegrationEvent> context)
    {
        var message = context.Message;
        
        _logger.LogInformation("Processing UserCreatedIntegrationEvent for user {UserId} of type {UserType}", 
            message.UserId, message.UserType);

        try
        {
            if (message.UserType.Equals("Promoter", StringComparison.OrdinalIgnoreCase))
            {
                var promoter = new Promoter
                {
                    Id = message.UserId,
                    FirstName = message.FirstName,
                    LastName = message.LastName,
                    Email = message.Email
                };

                _context.Promoters.Add(promoter);
            }
            else if (message.UserType.Equals("Organization", StringComparison.OrdinalIgnoreCase))
            {
                var organization = new Organization
                {
                    Id = message.UserId,
                    Name = $"{message.FirstName} {message.LastName}".Trim(),
                    Email = message.Email
                };

                _context.Organizations.Add(organization);
            }

            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Successfully processed UserCreatedIntegrationEvent for user {UserId}", message.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing UserCreatedIntegrationEvent for user {UserId}", message.UserId);
            throw;
        }
    }
}
