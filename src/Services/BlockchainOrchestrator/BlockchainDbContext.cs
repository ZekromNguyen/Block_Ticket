using Microsoft.EntityFrameworkCore;

namespace BlockchainOrchestrator;

public sealed class BlockchainDbContext : DbContext
{
    public BlockchainDbContext(DbContextOptions<BlockchainDbContext> options) : base(options)
    {
    }

    public DbSet<BlockchainTicketMint> TicketMints => Set<BlockchainTicketMint>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BlockchainTicketMint>(entity =>
        {
            entity.HasKey(mint => mint.Id);
            entity.Property(mint => mint.Status).HasConversion<string>().HasMaxLength(32);
            entity.Property(mint => mint.UserWalletAddress).IsRequired().HasMaxLength(128);
            entity.Property(mint => mint.ContractAddress).HasMaxLength(128);
            entity.Property(mint => mint.TokenId).HasMaxLength(128);
            entity.Property(mint => mint.TransactionHash).HasMaxLength(128);
            entity.HasIndex(mint => mint.TicketId).IsUnique();
        });
    }
}

public sealed class BlockchainTicketMint
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TicketId { get; set; }
    public Guid EventId { get; set; }
    public string UserWalletAddress { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string TicketMetadata { get; set; } = "{}";
    public string? ContractAddress { get; set; }
    public string? TokenId { get; set; }
    public string? TransactionHash { get; set; }
    public BlockchainMintStatus Status { get; set; } = BlockchainMintStatus.Pending;
    public string? FailureReason { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
}

public enum BlockchainMintStatus
{
    Pending = 1,
    Minted = 2,
    Failed = 3,
    Burned = 4
}
