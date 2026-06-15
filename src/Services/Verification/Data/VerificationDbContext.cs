using Microsoft.EntityFrameworkCore;
using Verification.Models;

namespace Verification.Data;

public sealed class VerificationDbContext : DbContext
{
    public VerificationDbContext(DbContextOptions<VerificationDbContext> options) : base(options)
    {
    }

    public DbSet<TicketScan> TicketScans => Set<TicketScan>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TicketScan>(entity =>
        {
            entity.HasKey(scan => scan.Id);
            entity.Property(scan => scan.VerificationCode).IsRequired().HasMaxLength(64);
            entity.Property(scan => scan.CheckedBy).IsRequired().HasMaxLength(128);
            entity.Property(scan => scan.Location).IsRequired().HasMaxLength(200);
            entity.Property(scan => scan.Result).IsRequired().HasMaxLength(32);
            entity.Property(scan => scan.Reason).IsRequired().HasMaxLength(300);
            entity.HasIndex(scan => new { scan.TicketId, scan.Accepted });
        });
    }
}
