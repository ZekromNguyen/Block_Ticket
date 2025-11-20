using Event.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Event.Infrastructure.Persistence.Configurations;

public class SeatAllocationConfiguration : IEntityTypeConfiguration<SeatAllocation>
{
    public void Configure(EntityTypeBuilder<SeatAllocation> builder)
    {
        builder.ToTable("SeatAllocations");

        // Composite primary key
        builder.HasKey(sa => new { sa.AllocationId, sa.SeatId });

        // Relationship with Allocation
        builder.HasOne(sa => sa.Allocation)
            .WithMany(a => a.AllocatedSeats)
            .HasForeignKey(sa => sa.AllocationId)
            .OnDelete(DeleteBehavior.Cascade);

        // Relationship with Seat
        builder.HasOne(sa => sa.Seat)
            .WithMany()
            .HasForeignKey(sa => sa.SeatId)
            .OnDelete(DeleteBehavior.Restrict); // Prevent deleting a seat if it's part of an allocation
    }
}

