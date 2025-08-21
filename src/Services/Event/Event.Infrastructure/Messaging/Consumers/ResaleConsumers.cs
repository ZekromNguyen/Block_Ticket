using Event.Application.IntegrationEvents.Events;
using Event.Domain.Interfaces;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Event.Infrastructure.Messaging.Consumers;

/// <summary>
/// Consumer for ticket resale listed events from Ticketing Service
/// </summary>
public class TicketResaleListedConsumer : IConsumer<TicketResaleListedIntegrationEvent>
{
    private readonly IEventRepository _eventRepository;
    private readonly ILogger<TicketResaleListedConsumer> _logger;

    public TicketResaleListedConsumer(
        IEventRepository eventRepository,
        ILogger<TicketResaleListedConsumer> logger)
    {
        _eventRepository = eventRepository;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<TicketResaleListedIntegrationEvent> context)
    {
        var message = context.Message;
        _logger.LogInformation("Processing TicketResaleListed for Event {EventId}, Resale {ResaleId}", 
            message.EventId, message.ResaleId);

        try
        {
            // Get the event to validate resale policies
            var eventAggregate = await _eventRepository.GetByIdAsync(message.EventId, context.CancellationToken);
            if (eventAggregate == null)
            {
                _logger.LogWarning("Event {EventId} not found for resale listing {ResaleId}", 
                    message.EventId, message.ResaleId);
                return;
            }

            // Validate that resale is allowed for this event
            if (!eventAggregate.AllowsResale())
            {
                _logger.LogWarning("Resale is not allowed for Event {EventId}, but resale {ResaleId} was listed", 
                    message.EventId, message.ResaleId);
                // Could publish a ResaleRejected event here
                return;
            }

            // Validate resale pricing rules (e.g., maximum markup allowed)
            var isValidPricing = ValidateResalePricing(message);
            if (!isValidPricing)
            {
                _logger.LogWarning("Resale pricing for {ResaleId} violates event policies", message.ResaleId);
                return;
            }

            // Log successful resale listing
            _logger.LogInformation("Successfully validated resale listing {ResaleId} for Event {EventId}", 
                message.ResaleId, message.EventId);

            // Update any internal tracking or analytics
            // This could include updating resale availability counts, etc.
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process TicketResaleListed for ResaleId {ResaleId}", message.ResaleId);
            throw;
        }
    }

    private static bool ValidateResalePricing(TicketResaleListedIntegrationEvent message)
    {
        // Example validation: ensure resale price doesn't exceed 150% of original price
        foreach (var ticket in message.TicketsForSale)
        {
            var markupPercentage = (ticket.ResalePrice.Amount - ticket.OriginalPrice.Amount) / ticket.OriginalPrice.Amount * 100;
            if (markupPercentage > 50) // 50% markup limit
            {
                return false;
            }
        }
        return true;
    }
}

/// <summary>
/// Consumer for ticket resale sold events from Ticketing Service
/// </summary>
public class TicketResaleSoldConsumer : IConsumer<TicketResaleSoldIntegrationEvent>
{
    private readonly IEventRepository _eventRepository;
    private readonly ILogger<TicketResaleSoldConsumer> _logger;

    public TicketResaleSoldConsumer(
        IEventRepository eventRepository,
        ILogger<TicketResaleSoldConsumer> logger)
    {
        _eventRepository = eventRepository;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<TicketResaleSoldIntegrationEvent> context)
    {
        var message = context.Message;
        _logger.LogInformation("Processing TicketResaleSold for Event {EventId}, Resale {ResaleId}", 
            message.EventId, message.ResaleId);

        try
        {
            // Get the event
            var eventAggregate = await _eventRepository.GetByIdAsync(message.EventId, context.CancellationToken);
            if (eventAggregate == null)
            {
                _logger.LogWarning("Event {EventId} not found for resale sale {ResaleId}", 
                    message.EventId, message.ResaleId);
                return;
            }

            // Log the successful resale transaction
            _logger.LogInformation("Resale {ResaleId} completed for Event {EventId}. " +
                                 "Seller: {SellerId}, Buyer: {BuyerId}, Amount: {Amount}", 
                message.ResaleId, message.EventId, message.SellerId, message.BuyerId, message.SalePrice.Amount);

            // Update any internal analytics or tracking
            // This could include:
            // - Updating resale volume metrics
            // - Tracking price trends
            // - Updating availability counts

            // The actual ticket ownership transfer is handled by the Ticketing Service
            // This service just tracks the transaction for analytics and compliance
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process TicketResaleSold for ResaleId {ResaleId}", message.ResaleId);
            throw;
        }
    }
}

/// <summary>
/// Consumer for user preferences updated events from Identity Service
/// </summary>
public class UserPreferencesUpdatedConsumer : IConsumer<UserPreferencesUpdatedIntegrationEvent>
{
    private readonly ILogger<UserPreferencesUpdatedConsumer> _logger;

    public UserPreferencesUpdatedConsumer(ILogger<UserPreferencesUpdatedConsumer> logger)
    {
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<UserPreferencesUpdatedIntegrationEvent> context)
    {
        var message = context.Message;
        _logger.LogInformation("Processing UserPreferencesUpdated for User {UserId}", message.UserId);

        try
        {
            // This could be used to:
            // - Update recommendation algorithms
            // - Trigger personalized event suggestions
            // - Update marketing campaign targeting
            // - Cache user preferences for faster event filtering

            _logger.LogInformation("User {UserId} updated preferences: Categories: [{Categories}], Location: {Location}", 
                message.UserId, 
                string.Join(", ", message.PreferredCategories), 
                message.PreferredLocation ?? "Not specified");

            // For now, just log the update
            // In a full implementation, this might update a user preferences cache
            // or trigger recommendation engine updates

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process UserPreferencesUpdated for User {UserId}", message.UserId);
            throw;
        }
    }
}
