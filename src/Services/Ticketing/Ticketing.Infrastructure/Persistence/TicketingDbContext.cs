using Microsoft.EntityFrameworkCore;
using Ticketing.Domain.Entities;

namespace Ticketing.Infrastructure.Persistence;

public class TicketingDbContext : DbContext
{
    public TicketingDbContext(DbContextOptions<TicketingDbContext> options) : base(options)
    {
    }

    public DbSet<Reservation> Reservations => Set<Reservation>();

    public DbSet<ReservationItem> ReservationItems => Set<ReservationItem>();

    public DbSet<ReservationPayment> ReservationPayments => Set<ReservationPayment>();

    public DbSet<Ticket> Tickets => Set<Ticket>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Reservation>(entity =>
        {
            entity.HasKey(reservation => reservation.Id);
            entity.Property(reservation => reservation.ReservationNumber).IsRequired().HasMaxLength(50);
            entity.Property(reservation => reservation.Status).HasConversion<string>().HasMaxLength(32);
            entity.Property(reservation => reservation.Currency).IsRequired().HasMaxLength(3);
            entity.Property(reservation => reservation.IdempotencyKey).HasMaxLength(128);
            entity.Property(reservation => reservation.InventoryLockOwner).IsRequired().HasMaxLength(128);
            entity.Property(reservation => reservation.PaymentIntentId).HasMaxLength(128);
            entity.Property(reservation => reservation.Subtotal).HasColumnType("decimal(18,2)");
            entity.Property(reservation => reservation.ServiceFee).HasColumnType("decimal(18,2)");
            entity.Property(reservation => reservation.ProcessingFee).HasColumnType("decimal(18,2)");
            entity.Property(reservation => reservation.TotalAmount).HasColumnType("decimal(18,2)");
            entity.HasIndex(reservation => reservation.ReservationNumber).IsUnique();
            entity.HasIndex(reservation => reservation.IdempotencyKey).IsUnique().HasFilter("\"IdempotencyKey\" IS NOT NULL");

            entity.HasMany(reservation => reservation.Items)
                .WithOne(item => item.Reservation)
                .HasForeignKey(item => item.ReservationId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(reservation => reservation.Payments)
                .WithOne(payment => payment.Reservation)
                .HasForeignKey(payment => payment.ReservationId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(reservation => reservation.Tickets)
                .WithOne(ticket => ticket.Reservation)
                .HasForeignKey(ticket => ticket.ReservationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ReservationItem>(entity =>
        {
            entity.HasKey(item => item.Id);
            entity.Property(item => item.TicketTypeName).IsRequired().HasMaxLength(200);
            entity.Property(item => item.UnitPrice).HasColumnType("decimal(18,2)");
            entity.Property(item => item.TotalPrice).HasColumnType("decimal(18,2)");
            entity.HasIndex(item => new { item.ReservationId, item.TicketTypeId });
        });

        modelBuilder.Entity<ReservationPayment>(entity =>
        {
            entity.HasKey(payment => payment.Id);
            entity.Property(payment => payment.PaymentIntentId).IsRequired().HasMaxLength(128);
            entity.Property(payment => payment.PaymentMethod).IsRequired().HasMaxLength(50);
            entity.Property(payment => payment.Currency).IsRequired().HasMaxLength(3);
            entity.Property(payment => payment.Amount).HasColumnType("decimal(18,2)");
            entity.Property(payment => payment.Status).HasConversion<string>().HasMaxLength(32);
            entity.Property(payment => payment.TransactionId).HasMaxLength(128);
            entity.HasIndex(payment => payment.PaymentIntentId);
        });

        modelBuilder.Entity<Ticket>(entity =>
        {
            entity.HasKey(ticket => ticket.Id);
            entity.Property(ticket => ticket.TicketNumber).IsRequired().HasMaxLength(50);
            entity.Property(ticket => ticket.TicketTypeName).IsRequired().HasMaxLength(200);
            entity.Property(ticket => ticket.PricePaid).HasColumnType("decimal(18,2)");
            entity.Property(ticket => ticket.Status).HasConversion<string>().HasMaxLength(32);
            entity.Property(ticket => ticket.ContractAddress).HasMaxLength(128);
            entity.Property(ticket => ticket.TokenId).HasMaxLength(128);
            entity.Property(ticket => ticket.TransactionHash).HasMaxLength(128);
            entity.Property(ticket => ticket.VerificationCode).IsRequired().HasMaxLength(32);
            entity.Property(ticket => ticket.UsedBy).HasMaxLength(128);
            entity.Property(ticket => ticket.UsedLocation).HasMaxLength(200);
            entity.HasIndex(ticket => ticket.TicketNumber).IsUnique();
            entity.HasIndex(ticket => ticket.VerificationCode).IsUnique();
            entity.HasIndex(ticket => new { ticket.UserId, ticket.EventId });
        });
    }
}
