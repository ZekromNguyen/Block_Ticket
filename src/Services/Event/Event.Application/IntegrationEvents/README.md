# Event Service - Integration Events Documentation

This document describes the complete messaging contracts for the Event Service, including published events, consumed events, and message routing configuration.

## Overview

The Event Service uses MassTransit with RabbitMQ for asynchronous messaging. It publishes events about event lifecycle, inventory changes, reservations, and pricing rules, while consuming events from other services like Ticketing and Identity services.

## Published Events (Event Service → Other Services)

### Event Lifecycle Events

#### EventCreatedIntegrationEvent
- **Exchange**: `event.created`
- **Purpose**: Published when a new event is created
- **Consumers**: Ticketing Service, Notification Service, Analytics Service
- **Key Fields**: EventId, Title, PromoterId, VenueId, EventDate, TicketTypes

#### EventPublishedIntegrationEvent
- **Exchange**: `event.published`
- **Purpose**: Published when an event is made available to the public
- **Consumers**: Ticketing Service, Notification Service, Marketing Service
- **Key Fields**: EventId, Title, PublishedAt, AvailableTicketTypes

#### EventCancelledIntegrationEvent
- **Exchange**: `event.cancelled`
- **Purpose**: Published when an event is cancelled
- **Consumers**: Ticketing Service, Notification Service, Refund Service
- **Key Fields**: EventId, CancelledAt, Reason, AffectedReservations

#### EventUpdatedIntegrationEvent
- **Exchange**: `event.updated`
- **Purpose**: Published when event details are modified
- **Consumers**: Ticketing Service, Notification Service
- **Key Fields**: EventId, Changes, UpdatedAt

### Inventory Events

#### InventoryChangedIntegrationEvent
- **Exchange**: `inventory.changed`
- **Purpose**: Published when ticket inventory changes (reserved, released, sold)
- **Consumers**: Ticketing Service, Analytics Service, Notification Service
- **Key Fields**: EventId, TicketTypeId, PreviousQuantity, NewQuantity, ChangeType

#### TicketsRestockedIntegrationEvent
- **Exchange**: `tickets.restocked`
- **Purpose**: Published when tickets are returned to inventory (refunds, cancellations)
- **Consumers**: Ticketing Service, Analytics Service
- **Key Fields**: EventId, RestockedTickets, Reason

#### EventSoldOutIntegrationEvent
- **Exchange**: `event.soldout`
- **Purpose**: Published when an event or ticket type sells out
- **Consumers**: Notification Service, Marketing Service
- **Key Fields**: EventId, SoldOutAt, SoldOutTicketTypes

### Reservation Events

#### ReservationCreatedIntegrationEvent
- **Exchange**: `reservation.created`
- **Purpose**: Published when a new reservation is created
- **Consumers**: Ticketing Service, Notification Service
- **Key Fields**: ReservationId, EventId, UserId, ExpiresAt, SeatIds

#### ReservationConfirmedIntegrationEvent
- **Exchange**: `reservation.confirmed`
- **Purpose**: Published when a reservation is confirmed (payment successful)
- **Consumers**: Ticketing Service, Notification Service, Analytics Service
- **Key Fields**: ReservationId, EventId, UserId, ConfirmedAt, PaymentReference

#### ReservationCancelledIntegrationEvent
- **Exchange**: `reservation.cancelled`
- **Purpose**: Published when a reservation is cancelled
- **Consumers**: Ticketing Service, Notification Service
- **Key Fields**: ReservationId, EventId, CancelledAt, Reason, ReleasedSeatIds

#### ReservationExpiredIntegrationEvent
- **Exchange**: `reservation.expired`
- **Purpose**: Published when a reservation expires
- **Consumers**: Ticketing Service, Analytics Service
- **Key Fields**: ReservationId, EventId, ExpiredAt, ReleasedSeatIds

### Pricing Events

#### PricingRuleCreatedIntegrationEvent
- **Exchange**: `pricing.rule.created`
- **Purpose**: Published when a new pricing rule is created
- **Consumers**: Ticketing Service, Analytics Service
- **Key Fields**: PricingRuleId, EventId, RuleName, RuleType, DiscountCode

#### PricingRuleUpdatedIntegrationEvent
- **Exchange**: `pricing.rule.updated`
- **Purpose**: Published when a pricing rule is modified
- **Consumers**: Ticketing Service, Analytics Service
- **Key Fields**: PricingRuleId, EventId, Changes, UpdatedAt

#### DiscountCodeUsedIntegrationEvent
- **Exchange**: `discount.code.used`
- **Purpose**: Published when a discount code is applied to an order
- **Consumers**: Analytics Service, Fraud Detection Service
- **Key Fields**: PricingRuleId, DiscountCode, UserId, DiscountAmount, RemainingUses

#### DynamicPricingUpdatedIntegrationEvent
- **Exchange**: `dynamic.pricing.updated`
- **Purpose**: Published when dynamic pricing adjustments are made
- **Consumers**: Ticketing Service, Analytics Service
- **Key Fields**: EventId, PriceUpdates, DemandLevel, Reason

## Consumed Events (Other Services → Event Service)

### Payment Events (from Ticketing Service)

#### OrderPaymentAuthorizedIntegrationEvent
- **Exchange**: `order.payment.authorized`
- **Purpose**: Payment has been authorized, extend reservation expiry
- **Handler**: OrderPaymentAuthorizedConsumer
- **Action**: Extend reservation expiry time

#### OrderPaymentCompletedIntegrationEvent
- **Exchange**: `order.payment.completed`
- **Purpose**: Payment completed successfully, confirm reservation
- **Handler**: OrderPaymentCompletedConsumer
- **Action**: Confirm reservation, mark seats as sold

#### OrderPaymentFailedIntegrationEvent
- **Exchange**: `order.payment.failed`
- **Purpose**: Payment failed, cancel reservation
- **Handler**: OrderPaymentFailedConsumer
- **Action**: Cancel reservation, release seats

### Order Management Events (from Ticketing Service)

#### OrderCancelledIntegrationEvent
- **Exchange**: `order.cancelled`
- **Purpose**: Order was cancelled, restock tickets
- **Handler**: OrderCancelledConsumer
- **Action**: Cancel associated reservation, restock tickets

### Refund Events (from Ticketing Service)

#### RefundRequestedIntegrationEvent
- **Exchange**: `refund.requested`
- **Purpose**: Refund was requested, validate eligibility
- **Handler**: RefundRequestedConsumer
- **Action**: Validate refund eligibility based on event policies

#### RefundProcessedIntegrationEvent
- **Exchange**: `refund.processed`
- **Purpose**: Refund was processed, restock tickets if needed
- **Handler**: RefundProcessedConsumer
- **Action**: Restock tickets, publish inventory change

### Resale Events (from Ticketing Service)

#### TicketResaleListedIntegrationEvent
- **Exchange**: `ticket.resale.listed`
- **Purpose**: Tickets listed for resale, validate policies
- **Handler**: TicketResaleListedConsumer
- **Action**: Validate resale is allowed, check pricing rules

#### TicketResaleSoldIntegrationEvent
- **Exchange**: `ticket.resale.sold`
- **Purpose**: Resale transaction completed
- **Handler**: TicketResaleSoldConsumer
- **Action**: Update analytics, log transaction

### User Events (from Identity Service)

#### UserPreferencesUpdatedIntegrationEvent
- **Exchange**: `user.preferences.updated`
- **Purpose**: User updated their preferences
- **Handler**: UserPreferencesUpdatedConsumer
- **Action**: Update recommendation cache, trigger personalized suggestions

## Message Routing Configuration

### Consumer Endpoints

- **event-service-payment-events**: High priority payment-related events
- **event-service-refund-events**: Refund processing events
- **event-service-resale-events**: Ticket resale events
- **event-service-order-events**: Order management events
- **event-service-user-events**: Lower priority user preference events

### Error Handling

- **Retry Policy**: Exponential backoff with 5 retries
- **Dead Letter Queue**: Failed messages after all retries
- **Delayed Redelivery**: Progressive delays for transient failures
- **Circuit Breaker**: Automatic failure detection and recovery

### Performance Configuration

- **Prefetch Counts**: Optimized per endpoint type
- **Concurrency**: Parallel message processing
- **Batching**: Efficient message grouping where applicable
- **Outbox Pattern**: Ensures message delivery consistency

## Testing

Integration tests are provided in `MessagingIntegrationTests.cs` to verify:
- Message publishing works correctly
- Message consumption processes properly
- Message topology is configured correctly
- Error handling behaves as expected

## Configuration

### RabbitMQ Settings
```json
{
  "RabbitMQ": {
    "Host": "localhost",
    "Username": "guest",
    "Password": "guest"
  }
}
```

### Message Retention
- **Standard Messages**: 7 days
- **Critical Events**: 30 days
- **Analytics Events**: 90 days

## Monitoring and Observability

- **Message Metrics**: Published/consumed counts, processing times
- **Error Rates**: Failed message percentages
- **Queue Depths**: Backlog monitoring
- **Consumer Health**: Active consumer status
- **Correlation IDs**: End-to-end tracing support

## Security Considerations

- **Message Encryption**: TLS 1.3 for transport
- **Authentication**: RabbitMQ user credentials
- **Authorization**: Queue-level permissions
- **PII Handling**: Minimal personal data in messages
- **Audit Logging**: All message operations logged

## Deployment Notes

- **Blue/Green Deployments**: Message compatibility during deployments
- **Schema Evolution**: Backward-compatible message changes
- **Consumer Scaling**: Horizontal scaling support
- **Disaster Recovery**: Message persistence and backup strategies
