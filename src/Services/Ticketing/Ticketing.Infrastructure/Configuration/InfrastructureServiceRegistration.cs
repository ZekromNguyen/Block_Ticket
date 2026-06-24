using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Ticketing.Application.Interfaces;
using Ticketing.Infrastructure.Hosting;
using Ticketing.Infrastructure.Messaging;
using Ticketing.Infrastructure.Payments;
using Ticketing.Infrastructure.Persistence;
using Ticketing.Infrastructure.Services;

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

        services.AddMemoryCache();
        services.AddHttpClient("event", client =>
        {
            client.BaseAddress = new Uri(configuration["Services:Event"] ?? "http://localhost:5002");
        });
        services.AddHttpContextAccessor();
        services.AddScoped<ISeatMapAvailabilityService, HttpSeatMapAvailabilityService>();
        services.AddScoped<ITicketResalePolicy, HttpTicketResalePolicy>();
        services.AddScoped<IPricingEvaluationService, HttpPricingEvaluationService>();
        services.AddScoped<ICurrencyPolicyService, HttpCurrencyPolicyService>();
        services.AddScoped<IRiskAssessmentService, HttpRiskAssessmentService>();

        services.AddMassTransit(bus =>
        {
            bus.AddConsumer<TicketMintedConsumer>();
            bus.AddConsumer<TicketMintFailedConsumer>();
            bus.AddConsumer<TicketsRestockedConsumer>();
            bus.AddConsumer<ReservationReleasedConsumer>();
            bus.AddConsumer<EventCancelledConsumer>();
            bus.AddConsumer<TicketPurchasedAnalyticsConsumer>();
            bus.AddConsumer<TicketRefundedAnalyticsConsumer>();
            bus.AddConsumer<TicketTransferredAnalyticsConsumer>();
            bus.AddConsumer<TicketListedForResaleAnalyticsConsumer>();

            bus.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(configuration.GetConnectionString("RabbitMQ"));
                cfg.ConfigureEndpoints(context);
            });
        });

        services.AddHostedService<WaitingListExpirySweepService>();
        services.AddHostedService<ReservationExpirySweepService>();

        return services;
    }
}