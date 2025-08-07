using MassTransit;
using Shared.Contracts.Commands;
using Shared.Contracts.Events;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace BlockchainOrchestrator.Worker.Consumers;

public class MintTicketCommandConsumer : IConsumer<MintTicketCommand>
{
    private readonly IBlockchainService _blockchainService;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<MintTicketCommandConsumer> _logger;

    public MintTicketCommandConsumer(
        IBlockchainService blockchainService,
        IPublishEndpoint publishEndpoint,
        ILogger<MintTicketCommandConsumer> logger)
    {
        _blockchainService = blockchainService;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<MintTicketCommand> context)
    {
        var command = context.Message;
        
        try
        {
            _logger.LogInformation("Processing MintTicketCommand for ticket {TicketId}", command.TicketId);
            
            var transactionHash = await _blockchainService.MintTicketAsync(
                command.TicketId,
                command.UserWalletAddress,
                command.TicketMetadata
            );

            // Publish ticket minted event
            await _publishEndpoint.Publish(new TicketMinted(
                command.TicketId,
                transactionHash,
                command.TicketId.GetHashCode().ToString(), // Simple tokenId generation
                DateTime.UtcNow
            ));
            
            _logger.LogInformation("Ticket {TicketId} minted successfully with transaction {TransactionHash}", 
                command.TicketId, transactionHash);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to mint ticket {TicketId}", command.TicketId);
            
            // In a real-world scenario, you might want to publish a failure event
            // or implement retry logic with exponential backoff
            throw;
        }
    }
}

public class BurnTicketCommandConsumer : IConsumer<BurnTicketCommand>
{
    private readonly IBlockchainService _blockchainService;
    private readonly ILogger<BurnTicketCommandConsumer> _logger;

    public BurnTicketCommandConsumer(
        IBlockchainService blockchainService,
        ILogger<BurnTicketCommandConsumer> logger)
    {
        _blockchainService = blockchainService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<BurnTicketCommand> context)
    {
        var command = context.Message;
        
        try
        {
            _logger.LogInformation("Processing BurnTicketCommand for ticket {TicketId}", command.TicketId);
            
            var tokenId = command.TicketId.GetHashCode().ToString();
            var transactionHash = await _blockchainService.BurnTicketAsync(tokenId);
            
            _logger.LogInformation("Ticket {TicketId} burned successfully with transaction {TransactionHash}", 
                command.TicketId, transactionHash);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to burn ticket {TicketId}", command.TicketId);
            throw;
        }
    }
}
