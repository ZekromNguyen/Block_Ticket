using Event.Application.Common.Interfaces;
using Event.Application.IntegrationEvents.Events;
using Event.Infrastructure.Messaging.Consumers;
using Event.Infrastructure.Services;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace Event.Tests.Integration;

/// <summary>
/// Integration tests for the messaging system
/// </summary>
public class MessagingIntegrationTests : IClassFixture<MessagingTestFixture>
{
    private readonly MessagingTestFixture _fixture;
    private readonly ITestOutputHelper _output;

    public MessagingIntegrationTests(MessagingTestFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
    }

    [Fact]
    public async Task PublishEventCreated_ShouldPublishMessage()
    {
        // Arrange
        using var scope = _fixture.ServiceProvider.CreateScope();
        var publisher = scope.ServiceProvider.GetRequiredService<IIntegrationEventPublisher>();
        var harness = scope.ServiceProvider.GetRequiredService<ITestHarness>();

        await harness.Start();

        try
        {
            // Act
            await publisher.PublishEventCreatedAsync(
                Guid.NewGuid(),
                "Test Event",
                Guid.NewGuid(),
                DateTime.UtcNow.AddDays(30),
                Guid.NewGuid());

            // Assert
            var published = await harness.Published.Any<EventCreatedIntegrationEvent>();
            Assert.True(published);

            _output.WriteLine("EventCreated integration event published successfully");
        }
        finally
        {
            await harness.Stop();
        }
    }

    [Fact]
    public async Task PublishInventoryChanged_ShouldPublishMessage()
    {
        // Arrange
        using var scope = _fixture.ServiceProvider.CreateScope();
        var publisher = scope.ServiceProvider.GetRequiredService<IIntegrationEventPublisher>();
        var harness = scope.ServiceProvider.GetRequiredService<ITestHarness>();

        await harness.Start();

        try
        {
            // Act
            await publisher.PublishInventoryChangedAsync(
                Guid.NewGuid(),
                Guid.NewGuid(),
                100,
                95,
                "Tickets reserved");

            // Assert
            var published = await harness.Published.Any<InventoryChangedIntegrationEvent>();
            Assert.True(published);

            _output.WriteLine("InventoryChanged integration event published successfully");
        }
        finally
        {
            await harness.Stop();
        }
    }

    [Fact]
    public async Task ConsumeOrderPaymentAuthorized_ShouldProcessMessage()
    {
        // Arrange
        using var scope = _fixture.ServiceProvider.CreateScope();
        var harness = scope.ServiceProvider.GetRequiredService<ITestHarness>();
        var consumerHarness = harness.GetConsumerHarness<OrderPaymentAuthorizedConsumer>();

        await harness.Start();

        try
        {
            // Act
            await harness.Bus.Publish(new OrderPaymentAuthorizedIntegrationEvent
            {
                OrderId = Guid.NewGuid(),
                ReservationId = Guid.NewGuid(),
                EventId = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                AuthorizedAmount = new Application.Common.Models.MoneyDto { Amount = 100m, Currency = "USD" },
                PaymentReference = "TEST-REF-123",
                AuthorizedAt = DateTime.UtcNow
            });

            // Assert
            var consumed = await consumerHarness.Consumed.Any<OrderPaymentAuthorizedIntegrationEvent>();
            Assert.True(consumed);

            _output.WriteLine("OrderPaymentAuthorized integration event consumed successfully");
        }
        finally
        {
            await harness.Stop();
        }
    }

    [Fact]
    public async Task ConsumeRefundProcessed_ShouldProcessMessage()
    {
        // Arrange
        using var scope = _fixture.ServiceProvider.CreateScope();
        var harness = scope.ServiceProvider.GetRequiredService<ITestHarness>();
        var consumerHarness = harness.GetConsumerHarness<RefundProcessedConsumer>();

        await harness.Start();

        try
        {
            // Act
            await harness.Bus.Publish(new RefundProcessedIntegrationEvent
            {
                RefundId = Guid.NewGuid(),
                OrderId = Guid.NewGuid(),
                EventId = Guid.NewGuid(),
                RefundedAmount = new Application.Common.Models.MoneyDto { Amount = 50m, Currency = "USD" },
                RefundedTickets = new List<RefundTicketDto>
                {
                    new RefundTicketDto
                    {
                        TicketId = Guid.NewGuid(),
                        TicketTypeId = Guid.NewGuid(),
                        TicketTypeName = "General Admission",
                        RefundAmount = new Application.Common.Models.MoneyDto { Amount = 50m, Currency = "USD" }
                    }
                },
                ProcessedAt = DateTime.UtcNow,
                ShouldRestockTickets = true
            });

            // Assert
            var consumed = await consumerHarness.Consumed.Any<RefundProcessedIntegrationEvent>();
            Assert.True(consumed);

            _output.WriteLine("RefundProcessed integration event consumed successfully");
        }
        finally
        {
            await harness.Stop();
        }
    }

    [Fact]
    public async Task MessageTopology_ShouldBeConfiguredCorrectly()
    {
        // Arrange
        using var scope = _fixture.ServiceProvider.CreateScope();
        var harness = scope.ServiceProvider.GetRequiredService<ITestHarness>();

        await harness.Start();

        try
        {
            // Act & Assert - Test that various message types can be published
            var eventCreatedTask = harness.Bus.Publish(new EventCreatedIntegrationEvent
            {
                EventId = Guid.NewGuid(),
                Title = "Test Event"
            });

            var reservationCreatedTask = harness.Bus.Publish(new ReservationCreatedIntegrationEvent
            {
                ReservationId = Guid.NewGuid(),
                EventId = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                ExpiresAt = DateTime.UtcNow.AddMinutes(15)
            });

            var pricingRuleCreatedTask = harness.Bus.Publish(new PricingRuleCreatedIntegrationEvent
            {
                PricingRuleId = Guid.NewGuid(),
                EventId = Guid.NewGuid(),
                RuleName = "Test Discount",
                RuleType = "DiscountCode"
            });

            await Task.WhenAll(eventCreatedTask, reservationCreatedTask, pricingRuleCreatedTask);

            // Verify messages were published
            var eventCreatedPublished = await harness.Published.Any<EventCreatedIntegrationEvent>();
            var reservationCreatedPublished = await harness.Published.Any<ReservationCreatedIntegrationEvent>();
            var pricingRuleCreatedPublished = await harness.Published.Any<PricingRuleCreatedIntegrationEvent>();

            Assert.True(eventCreatedPublished);
            Assert.True(reservationCreatedPublished);
            Assert.True(pricingRuleCreatedPublished);

            _output.WriteLine("All message types published successfully - topology is working");
        }
        finally
        {
            await harness.Stop();
        }
    }
}

/// <summary>
/// Test fixture for messaging integration tests
/// </summary>
public class MessagingTestFixture : IDisposable
{
    public IServiceProvider ServiceProvider { get; private set; }

    public MessagingTestFixture()
    {
        var services = new ServiceCollection();

        // Add logging
        services.AddLogging(builder => builder.AddConsole());

        // Add MassTransit with test harness
        services.AddMassTransitTestHarness(cfg =>
        {
            // Add consumers
            cfg.AddConsumer<OrderPaymentAuthorizedConsumer>();
            cfg.AddConsumer<OrderPaymentCompletedConsumer>();
            cfg.AddConsumer<RefundProcessedConsumer>();
            cfg.AddConsumer<TicketResaleListedConsumer>();

            cfg.UsingInMemory((context, configurator) =>
            {
                configurator.ConfigureEndpoints(context);
            });
        });

        // Add mock repositories and services
        services.AddScoped<Event.Domain.Interfaces.IReservationRepository, MockReservationRepository>();
        services.AddScoped<Event.Domain.Interfaces.IEventRepository, MockEventRepository>();
        services.AddScoped<Event.Domain.Interfaces.IVenueRepository, MockVenueRepository>();
        services.AddScoped<IIntegrationEventPublisher, IntegrationEventPublisher>();

        ServiceProvider = services.BuildServiceProvider();
    }

    public void Dispose()
    {
        if (ServiceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}

/// <summary>
/// Mock repositories for testing
/// </summary>
public class MockReservationRepository : Event.Domain.Interfaces.IReservationRepository
{
    public Task<Event.Domain.Entities.Reservation> AddAsync(Event.Domain.Entities.Reservation entity, CancellationToken cancellationToken = default)
        => Task.FromResult(entity);

    public Task<Event.Domain.Entities.Reservation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult<Event.Domain.Entities.Reservation?>(null);

    public void Update(Event.Domain.Entities.Reservation entity) { }
    public void Delete(Event.Domain.Entities.Reservation entity) { }
    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(false);
    public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}

public class MockEventRepository : Event.Domain.Interfaces.IEventRepository
{
    public Task<Event.Domain.Entities.EventAggregate> AddAsync(Event.Domain.Entities.EventAggregate entity, CancellationToken cancellationToken = default)
        => Task.FromResult(entity);

    public Task<Event.Domain.Entities.EventAggregate?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult<Event.Domain.Entities.EventAggregate?>(null);

    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(true);
    public void Update(Event.Domain.Entities.EventAggregate entity) { }
    public void Delete(Event.Domain.Entities.EventAggregate entity) { }
    public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}

public class MockVenueRepository : Event.Domain.Interfaces.IVenueRepository
{
    public Task<Event.Domain.Entities.Venue> AddAsync(Event.Domain.Entities.Venue entity, CancellationToken cancellationToken = default)
        => Task.FromResult(entity);

    public Task<Event.Domain.Entities.Venue?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult<Event.Domain.Entities.Venue?>(null);

    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(true);
    public void Update(Event.Domain.Entities.Venue entity) { }
    public void Delete(Event.Domain.Entities.Venue entity) { }
    public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}
