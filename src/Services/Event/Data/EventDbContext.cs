using Microsoft.EntityFrameworkCore;

namespace Event.Api.Data;

public class EventDbContext : DbContext
{
    public EventDbContext(DbContextOptions<EventDbContext> options) : base(options)
    {
    }

    public DbSet<Models.Event> Events { get; set; }
    public DbSet<Models.TicketType> TicketTypes { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Models.Event>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Venue).IsRequired().HasMaxLength(300);
            entity.Property(e => e.TicketPrice).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Status).HasConversion<string>();
            
            entity.HasMany(e => e.TicketTypes)
                  .WithOne(t => t.Event)
                  .HasForeignKey(t => t.EventId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Models.TicketType>(entity =>
        {
            entity.HasKey(t => t.Id);
            entity.Property(t => t.Name).IsRequired().HasMaxLength(100);
            entity.Property(t => t.Price).HasColumnType("decimal(18,2)");
        });
    }
}
