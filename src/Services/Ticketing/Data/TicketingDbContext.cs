using Microsoft.EntityFrameworkCore;
using Ticketing.Api.Models;

namespace Ticketing.Api.Data;

public class TicketingDbContext : DbContext
{
    public TicketingDbContext(DbContextOptions<TicketingDbContext> options) : base(options)
    {
    }

    public DbSet<Ticket> Tickets { get; set; }
    public DbSet<TicketTransaction> TicketTransactions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Ticket>(entity =>
        {
            entity.HasKey(t => t.Id);
            entity.Property(t => t.TicketNumber).IsRequired().HasMaxLength(50);
            entity.Property(t => t.Price).HasColumnType("decimal(18,2)");
            entity.Property(t => t.Status).HasConversion<string>();
            entity.HasIndex(t => t.TicketNumber).IsUnique();
            
            entity.HasMany(t => t.Transactions)
                  .WithOne(tr => tr.Ticket)
                  .HasForeignKey(tr => tr.TicketId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TicketTransaction>(entity =>
        {
            entity.HasKey(tr => tr.Id);
            entity.Property(tr => tr.Amount).HasColumnType("decimal(18,2)");
            entity.Property(tr => tr.Type).HasConversion<string>();
            entity.Property(tr => tr.Status).HasConversion<string>();
            entity.Property(tr => tr.PaymentMethod).IsRequired().HasMaxLength(50);
        });
    }
}
