using Event.Application.IntegrationEvents.Events;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Event.Infrastructure.Messaging;

/// <summary>
/// Configuration for messaging contracts and topology
/// </summary>
public static class MessagingConfiguration
{
    /// <summary>
    /// Configure message topology for all integration events
    /// </summary>
    public static void ConfigureMessageTopology(IRabbitMqBusFactoryConfigurator cfg)
    {
        // Event Service Published Events
        cfg.Message<EventCreatedIntegrationEvent>(e => e.SetEntityName("event.created"));
        cfg.Message<EventPublishedIntegrationEvent>(e => e.SetEntityName("event.published"));
        cfg.Message<EventCancelledIntegrationEvent>(e => e.SetEntityName("event.cancelled"));
        cfg.Message<EventUpdatedIntegrationEvent>(e => e.SetEntityName("event.updated"));
        cfg.Message<EventSoldOutIntegrationEvent>(e => e.SetEntityName("event.soldout"));

        // Inventory Events
        cfg.Message<InventoryChangedIntegrationEvent>(e => e.SetEntityName("inventory.changed"));
        cfg.Message<TicketsRestockedIntegrationEvent>(e => e.SetEntityName("tickets.restocked"));

        // Reservation Events
        cfg.Message<ReservationCreatedIntegrationEvent>(e => e.SetEntityName("reservation.created"));
        cfg.Message<ReservationConfirmedIntegrationEvent>(e => e.SetEntityName("reservation.confirmed"));
        cfg.Message<ReservationCancelledIntegrationEvent>(e => e.SetEntityName("reservation.cancelled"));
        cfg.Message<ReservationExpiredIntegrationEvent>(e => e.SetEntityName("reservation.expired"));
        cfg.Message<ReservationExtendedIntegrationEvent>(e => e.SetEntityName("reservation.extended"));

        // Pricing Events
        cfg.Message<PricingRuleCreatedIntegrationEvent>(e => e.SetEntityName("pricing.rule.created"));
        cfg.Message<PricingRuleUpdatedIntegrationEvent>(e => e.SetEntityName("pricing.rule.updated"));
        cfg.Message<PricingRuleStatusChangedIntegrationEvent>(e => e.SetEntityName("pricing.rule.status.changed"));
        cfg.Message<PricingRuleExpiredIntegrationEvent>(e => e.SetEntityName("pricing.rule.expired"));
        cfg.Message<DiscountCodeUsedIntegrationEvent>(e => e.SetEntityName("discount.code.used"));
        cfg.Message<DynamicPricingUpdatedIntegrationEvent>(e => e.SetEntityName("dynamic.pricing.updated"));

        // External Events (Consumed from other services)
        cfg.Message<OrderPaymentAuthorizedIntegrationEvent>(e => e.SetEntityName("order.payment.authorized"));
        cfg.Message<OrderPaymentCompletedIntegrationEvent>(e => e.SetEntityName("order.payment.completed"));
        cfg.Message<OrderPaymentFailedIntegrationEvent>(e => e.SetEntityName("order.payment.failed"));
        cfg.Message<OrderCancelledIntegrationEvent>(e => e.SetEntityName("order.cancelled"));
        cfg.Message<RefundRequestedIntegrationEvent>(e => e.SetEntityName("refund.requested"));
        cfg.Message<RefundProcessedIntegrationEvent>(e => e.SetEntityName("refund.processed"));
        cfg.Message<TicketResaleListedIntegrationEvent>(e => e.SetEntityName("ticket.resale.listed"));
        cfg.Message<TicketResaleSoldIntegrationEvent>(e => e.SetEntityName("ticket.resale.sold"));
        cfg.Message<UserPreferencesUpdatedIntegrationEvent>(e => e.SetEntityName("user.preferences.updated"));
    }

    /// <summary>
    /// Configure consumer endpoints with specific settings
    /// </summary>
    public static void ConfigureConsumerEndpoints(IRabbitMqBusFactoryConfigurator cfg, IBusRegistrationContext context)
    {
        // Configure payment-related consumers with higher priority
        cfg.ReceiveEndpoint("event-service-payment-events", e =>
        {
            e.PrefetchCount = 10;
            e.UseMessageRetry(r => r.Exponential(5, TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(1), TimeSpan.FromSeconds(2)));
            
            e.ConfigureConsumer<Consumers.OrderPaymentAuthorizedConsumer>(context);
            e.ConfigureConsumer<Consumers.OrderPaymentCompletedConsumer>(context);
            e.ConfigureConsumer<Consumers.OrderPaymentFailedConsumer>(context);
        });

        // Configure refund consumers
        cfg.ReceiveEndpoint("event-service-refund-events", e =>
        {
            e.PrefetchCount = 5;
            e.UseMessageRetry(r => r.Exponential(3, TimeSpan.FromSeconds(2), TimeSpan.FromMinutes(2), TimeSpan.FromSeconds(3)));
            
            e.ConfigureConsumer<Consumers.RefundRequestedConsumer>(context);
            e.ConfigureConsumer<Consumers.RefundProcessedConsumer>(context);
        });

        // Configure resale consumers
        cfg.ReceiveEndpoint("event-service-resale-events", e =>
        {
            e.PrefetchCount = 5;
            e.UseMessageRetry(r => r.Exponential(3, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(1)));
            
            e.ConfigureConsumer<Consumers.TicketResaleListedConsumer>(context);
            e.ConfigureConsumer<Consumers.TicketResaleSoldConsumer>(context);
        });

        // Configure order management consumers
        cfg.ReceiveEndpoint("event-service-order-events", e =>
        {
            e.PrefetchCount = 10;
            e.UseMessageRetry(r => r.Exponential(3, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(1)));
            
            e.ConfigureConsumer<Consumers.OrderCancelledConsumer>(context);
        });

        // Configure user preference consumers (lower priority)
        cfg.ReceiveEndpoint("event-service-user-events", e =>
        {
            e.PrefetchCount = 20;
            e.UseMessageRetry(r => r.Exponential(2, TimeSpan.FromSeconds(5), TimeSpan.FromMinutes(1), TimeSpan.FromSeconds(5)));
            
            e.ConfigureConsumer<Consumers.UserPreferencesUpdatedConsumer>(context);
        });
    }

    /// <summary>
    /// Get message routing configuration
    /// </summary>
    public static Dictionary<string, string> GetMessageRouting()
    {
        return new Dictionary<string, string>
        {
            // Event Service publishes to these exchanges
            { nameof(EventCreatedIntegrationEvent), "event.created" },
            { nameof(EventPublishedIntegrationEvent), "event.published" },
            { nameof(EventCancelledIntegrationEvent), "event.cancelled" },
            { nameof(EventUpdatedIntegrationEvent), "event.updated" },
            { nameof(InventoryChangedIntegrationEvent), "inventory.changed" },
            { nameof(TicketsRestockedIntegrationEvent), "tickets.restocked" },
            { nameof(ReservationCreatedIntegrationEvent), "reservation.created" },
            { nameof(ReservationConfirmedIntegrationEvent), "reservation.confirmed" },
            { nameof(ReservationCancelledIntegrationEvent), "reservation.cancelled" },
            { nameof(ReservationExpiredIntegrationEvent), "reservation.expired" },
            { nameof(PricingRuleCreatedIntegrationEvent), "pricing.rule.created" },
            { nameof(DiscountCodeUsedIntegrationEvent), "discount.code.used" },

            // Event Service consumes from these exchanges
            { nameof(OrderPaymentAuthorizedIntegrationEvent), "order.payment.authorized" },
            { nameof(OrderPaymentCompletedIntegrationEvent), "order.payment.completed" },
            { nameof(OrderPaymentFailedIntegrationEvent), "order.payment.failed" },
            { nameof(OrderCancelledIntegrationEvent), "order.cancelled" },
            { nameof(RefundRequestedIntegrationEvent), "refund.requested" },
            { nameof(RefundProcessedIntegrationEvent), "refund.processed" },
            { nameof(TicketResaleListedIntegrationEvent), "ticket.resale.listed" },
            { nameof(TicketResaleSoldIntegrationEvent), "ticket.resale.sold" },
            { nameof(UserPreferencesUpdatedIntegrationEvent), "user.preferences.updated" }
        };
    }

    /// <summary>
    /// Configure dead letter queues and error handling
    /// </summary>
    public static void ConfigureErrorHandling(IRabbitMqBusFactoryConfigurator cfg)
    {
        // Configure global error handling
        cfg.UseDelayedRedelivery(r => r.Intervals(
            TimeSpan.FromSeconds(5),
            TimeSpan.FromSeconds(15),
            TimeSpan.FromSeconds(30),
            TimeSpan.FromMinutes(1),
            TimeSpan.FromMinutes(5)));

        cfg.UseMessageRetry(r => r.Exponential(
            retryLimit: 5,
            minInterval: TimeSpan.FromSeconds(1),
            maxInterval: TimeSpan.FromMinutes(5),
            intervalDelta: TimeSpan.FromSeconds(2)));

        cfg.UseInMemoryOutbox();
    }

    /// <summary>
    /// Validate messaging configuration
    /// </summary>
    public static void ValidateConfiguration(IConfiguration configuration)
    {
        var rabbitMqSection = configuration.GetSection("RabbitMQ");
        
        if (!rabbitMqSection.Exists())
        {
            throw new InvalidOperationException("RabbitMQ configuration section is missing");
        }

        var host = rabbitMqSection["Host"];
        if (string.IsNullOrWhiteSpace(host))
        {
            throw new InvalidOperationException("RabbitMQ Host configuration is missing");
        }

        // Additional validation can be added here
    }
}
