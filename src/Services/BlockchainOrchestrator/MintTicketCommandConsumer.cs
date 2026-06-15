using MassTransit;
using Microsoft.EntityFrameworkCore;
using Shared.Contracts.Commands;
using Shared.Contracts.Events;

namespace BlockchainOrchestrator;

public sealed class MintTicketCommandConsumer : IConsumer<MintTicketCommand>
{
    private readonly BlockchainDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<MintTicketCommandConsumer> _logger;

    public MintTicketCommandConsumer(
        BlockchainDbContext context,
        IConfiguration configuration,
        ILogger<MintTicketCommandConsumer> logger)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<MintTicketCommand> context)
    {
        var message = context.Message;
        var existing = await _context.TicketMints.FirstOrDefaultAsync(mint => mint.TicketId == message.TicketId, context.CancellationToken);
        if (existing?.Status == BlockchainMintStatus.Minted)
        {
            await context.Publish(new TicketMinted(
                existing.TicketId,
                existing.TransactionHash ?? string.Empty,
                existing.TokenId ?? string.Empty,
                existing.CompletedAt ?? DateTime.UtcNow,
                existing.ContractAddress ?? string.Empty),
                context.CancellationToken);
            return;
        }

        var mint = existing ?? new BlockchainTicketMint
        {
            TicketId = message.TicketId,
            EventId = message.EventId,
            UserWalletAddress = message.UserWalletAddress,
            Price = message.Price,
            TicketMetadata = message.TicketMetadata
        };

        try
        {
            mint.ContractAddress = _configuration["Blockchain:ContractAddress"] ?? "0x0000000000000000000000000000000000000000";
            mint.TokenId = Math.Abs(message.TicketId.GetHashCode()).ToString();
            mint.TransactionHash = $"0x{Guid.NewGuid():N}{Guid.NewGuid():N}";
            mint.Status = BlockchainMintStatus.Minted;
            mint.CompletedAt = DateTime.UtcNow;

            if (existing is null)
            {
                _context.TicketMints.Add(mint);
            }

            await _context.SaveChangesAsync(context.CancellationToken);

            await context.Publish(new TicketMinted(
                mint.TicketId,
                mint.TransactionHash,
                mint.TokenId,
                mint.CompletedAt.Value,
                mint.ContractAddress),
                context.CancellationToken);
        }
        catch (Exception ex)
        {
            mint.Status = BlockchainMintStatus.Failed;
            mint.FailureReason = ex.Message;

            if (existing is null)
            {
                _context.TicketMints.Add(mint);
            }

            await _context.SaveChangesAsync(context.CancellationToken);
            await context.Publish(new TicketMintFailed(message.TicketId, ex.Message, DateTime.UtcNow), context.CancellationToken);
            _logger.LogError(ex, "Failed to mint ticket {TicketId}", message.TicketId);
        }
    }
}

public sealed class BurnTicketCommandConsumer : IConsumer<BurnTicketCommand>
{
    private readonly BlockchainDbContext _context;

    public BurnTicketCommandConsumer(BlockchainDbContext context)
    {
        _context = context;
    }

    public async Task Consume(ConsumeContext<BurnTicketCommand> context)
    {
        var mint = await _context.TicketMints.FirstOrDefaultAsync(item => item.TicketId == context.Message.TicketId, context.CancellationToken);
        if (mint is null)
        {
            return;
        }

        mint.Status = BlockchainMintStatus.Burned;
        mint.CompletedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(context.CancellationToken);
    }
}
