using Microsoft.EntityFrameworkCore;

namespace Notification;

public sealed class NotificationDbContext : DbContext
{
    public NotificationDbContext(DbContextOptions<NotificationDbContext> options) : base(options)
    {
    }

    public DbSet<NotificationMessage> Messages => Set<NotificationMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<NotificationMessage>(entity =>
        {
            entity.HasKey(message => message.Id);
            entity.Property(message => message.Type).IsRequired().HasMaxLength(64);
            entity.Property(message => message.Subject).IsRequired().HasMaxLength(200);
            entity.Property(message => message.Recipient).IsRequired().HasMaxLength(128);
            entity.HasIndex(message => new { message.Type, message.CorrelationId }).IsUnique();
        });
    }
}

public sealed class NotificationMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Type { get; set; } = string.Empty;
    public string Recipient { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public Guid CorrelationId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? SentAt { get; set; }
    public string Status { get; set; } = "Pending";
}
