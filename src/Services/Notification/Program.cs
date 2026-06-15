using MassTransit;
using Microsoft.EntityFrameworkCore;
using Notification;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddDbContext<NotificationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddMassTransit(bus =>
{
    bus.AddConsumer<TicketPurchasedNotificationConsumer>();
    bus.AddConsumer<TicketMintedNotificationConsumer>();
    bus.AddConsumer<TicketMintFailedNotificationConsumer>();

    bus.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(builder.Configuration.GetConnectionString("RabbitMQ"));
        cfg.ConfigureEndpoints(context);
    });
});

var host = builder.Build();

using (var scope = host.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();
    await context.Database.EnsureCreatedAsync();
}

await host.RunAsync();
