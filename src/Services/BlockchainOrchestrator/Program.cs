using BlockchainOrchestrator;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Shared.Common.Extensions;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSharedServices();

builder.Services.AddDbContext<BlockchainDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddMassTransit(bus =>
{
    bus.AddConsumer<MintTicketCommandConsumer>();
    bus.AddConsumer<BurnTicketCommandConsumer>();

    bus.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(builder.Configuration.GetConnectionString("RabbitMQ"));
        cfg.ConfigureEndpoints(context);
    });
});

var host = builder.Build();

using (var scope = host.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<BlockchainDbContext>();
    await context.Database.EnsureCreatedAsync();
}

await host.RunAsync();
