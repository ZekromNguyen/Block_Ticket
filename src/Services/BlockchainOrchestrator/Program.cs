using BlockchainOrchestrator.Worker;
using BlockchainOrchestrator.Worker.Consumers;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);

// Add Serilog
builder.Services.AddLogging(loggingBuilder =>
{
    loggingBuilder.ClearProviders();
    loggingBuilder.AddConsole();
});

// Add services
builder.Services.AddSingleton<IBlockchainService, BlockchainService>();

// Add MassTransit
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<MintTicketCommandConsumer>();
    x.AddConsumer<BurnTicketCommandConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(builder.Configuration.GetConnectionString("RabbitMQ"));
        
        cfg.ReceiveEndpoint("mint-ticket-queue", e =>
        {
            e.ConfigureConsumer<MintTicketCommandConsumer>(context);
        });
        
        cfg.ReceiveEndpoint("burn-ticket-queue", e =>
        {
            e.ConfigureConsumer<BurnTicketCommandConsumer>(context);
        });
    });
});

var host = builder.Build();

host.Run();
