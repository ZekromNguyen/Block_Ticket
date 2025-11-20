using Shared.Common.Models;

namespace Event.Domain.Entities;

/// <summary>
/// Represents the link between an Allocation and a specific Seat, used for seat-level holds.
/// </summary>
public class SeatAllocation : BaseEntity
{
    public Guid AllocationId { get; private set; }
    public Guid SeatId { get; private set; }

    // Navigation Properties
    public Allocation Allocation { get; private set; } = null!;
    public Seat Seat { get; private set; } = null!;

    // For EF Core
    private SeatAllocation() { }

    public SeatAllocation(Guid allocationId, Guid seatId)
    {
        if (allocationId == Guid.Empty)
            throw new ArgumentException("Allocation ID cannot be empty", nameof(allocationId));
        
        if (seatId == Guid.Empty)
            throw new ArgumentException("Seat ID cannot be empty", nameof(seatId));

        AllocationId = allocationId;
        SeatId = seatId;
    }
}

