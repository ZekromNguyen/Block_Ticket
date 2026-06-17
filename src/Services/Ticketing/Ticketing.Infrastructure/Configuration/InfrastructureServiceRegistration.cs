using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Ticketing.Application.Interfaces;
using Ticketing.Infrastructure.Messaging;
using Ticketing.Infrastructure.Payments;
using Ticketing.Infrastructure.Persistence;

namespace Ticketing.Infrastructure.Configuration;

public static class InfrastructureServiceRegistration
{
    public static IServiceCollection AddTicketingInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<TicketingDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<ITicketingRepository, TicketingRepository>();
        services.AddSingleton<InMemoryInventoryLockService>();
        services.AddSingleton<IInventoryLockService, RedisInventoryLockService>();
        services.AddScoped<IPaymentProvider, FakePaymentProvider>();
        services.AddScoped<ITicketEventPublisher, TicketEventPublisher>();

        services.AddMassTransit(bus =>
        {
            bus.AddConsumer<TicketMintedConsumer>();
            bus.AddConsumer<TicketMintFailedConsumer>();
            bus.AddConsumer<TicketsRestockedConsumer>();
            bus.AddConsumer<ReservationReleasedConsumer>();
            bus.AddConsumer<EventCancelledConsumer>();

            bus.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(configuration.GetConnectionString("RabbitMQ"));
                cfg.ConfigureEndpoints(context);
            });
        });

        return services;
    }
}
