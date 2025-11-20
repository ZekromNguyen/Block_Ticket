using Event.Domain.Enums;
using Event.Domain.Exceptions;
using Event.Domain.ValueObjects;
using Shared.Common.Models;

namespace Event.Domain.Entities;

/// <summary>
/// Represents an allocation of tickets or seats for specific purposes
/// (promoter holds, artist holds, presale allocations, etc.)
/// </summary>
public class Allocation : BaseAuditableEntity
{
    private readonly List<SeatAllocation> _allocatedSeats = new();
    // Basic Properties
    public Guid EventId { get; private set; }
    public Guid? TicketTypeId { get; private set; }
    public AllocationType Type { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public AllocationScope Scope { get; private set; }

    // Allocation Details
    private int _quantity;
    public int Quantity
    {
        get => Scope == AllocationScope.ByQuantity ? _quantity : _allocatedSeats.Count;
        private set => _quantity = value;
    }
    public int UsedQuantity { get; private set; }
    public int RemainingQuantity => Quantity - UsedQuantity;

    // Access Control
    public string? AccessCode { get; private set; }
    public bool RequiresAccessCode => !string.IsNullOrEmpty(AccessCode);
    public List<string> AllowedCustomerSegments { get; private set; } = new();

    // Time Windows
    public DateTime? StartTime { get; private set; }
    public DateTime? EndTime { get; private set; }
    public bool IsActive => IsWithinTimeWindow() && RemainingQuantity > 0;

    // Capacity Limits
    public int? MaxPerCustomer { get; private set; }
    public int? MinPerCustomer { get; private set; }

    // Status and Metadata
    public bool IsEnabled { get; private set; } = true;
    public int Priority { get; private set; } = 0; // Higher number = higher priority
    public Dictionary<string, object> Metadata { get; private set; } = new();

    // Navigation Properties
    public IReadOnlyCollection<SeatAllocation> AllocatedSeats => _allocatedSeats.AsReadOnly();
    public EventAggregate Event { get; private set; } = null!;
    public TicketType? TicketType { get; private set; }

    // For EF Core
    private Allocation() { }

    private Allocation(
        Guid eventId,
        AllocationType type,
        string name,
        Guid? ticketTypeId,
        string? description,
        string? accessCode,
        DateTime? startTime,
        DateTime? endTime,
        int? maxPerCustomer,
        int? minPerCustomer,
        int priority)
    {
        if (maxPerCustomer.HasValue && maxPerCustomer.Value <= 0)
            throw new AllocationDomainException("Max per customer must be greater than zero");

        if (minPerCustomer.HasValue && minPerCustomer.Value <= 0)
            throw new AllocationDomainException("Min per customer must be greater than zero");

        if (maxPerCustomer.HasValue && minPerCustomer.HasValue && minPerCustomer.Value > maxPerCustomer.Value)
            throw new AllocationDomainException("Min per customer cannot be greater than max per customer");

        if (startTime.HasValue && endTime.HasValue && startTime.Value >= endTime.Value)
            throw new AllocationDomainException("Start time must be before end time");

        EventId = eventId;
        TicketTypeId = ticketTypeId;
        Type = type;
        Name = name?.Trim() ?? throw new ArgumentNullException(nameof(name));
        Description = description?.Trim();
        UsedQuantity = 0;
        AccessCode = accessCode?.Trim();
        StartTime = startTime;
        EndTime = endTime;
        MaxPerCustomer = maxPerCustomer;
        MinPerCustomer = minPerCustomer;
        Priority = priority;
    }

    public Allocation(
        Guid eventId,
        AllocationType type,
        string name,
        int quantity,
        Guid? ticketTypeId = null,
        string? description = null,
        string? accessCode = null,
        DateTime? startTime = null,
        DateTime? endTime = null,
        int? maxPerCustomer = null,
        int? minPerCustomer = null,
        int priority = 0)
        : this(eventId, type, name, ticketTypeId, description, accessCode, startTime, endTime, maxPerCustomer, minPerCustomer, priority)
    {
        if (quantity <= 0)
            throw new AllocationDomainException("Allocation quantity must be greater than zero");

        Scope = AllocationScope.ByQuantity;
        Quantity = quantity;
    }

    public Allocation(
        Guid eventId,
        AllocationType type,
        string name,
        IEnumerable<Seat> seats,
        Guid? ticketTypeId = null,
        string? description = null,
        string? accessCode = null,
        DateTime? startTime = null,
        DateTime? endTime = null,
        int? maxPerCustomer = null,
        int? minPerCustomer = null,
        int priority = 0)
        : this(eventId, type, name, ticketTypeId, description, accessCode, startTime, endTime, maxPerCustomer, minPerCustomer, priority)
    {
        Scope = AllocationScope.BySeat;
        _allocatedSeats = seats.Select(s => new SeatAllocation(Id, s.Id)).ToList();
    }

    public void UpdateDetails(
        string name,
        string? description = null,
        int? maxPerCustomer = null,
        int? minPerCustomer = null,
        int priority = 0)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new AllocationDomainException("Allocation name cannot be empty");

        if (maxPerCustomer.HasValue && maxPerCustomer.Value <= 0)
            throw new AllocationDomainException("Max per customer must be greater than zero");

        if (minPerCustomer.HasValue && minPerCustomer.Value <= 0)
            throw new AllocationDomainException("Min per customer must be greater than zero");

        if (maxPerCustomer.HasValue && minPerCustomer.HasValue && minPerCustomer.Value > maxPerCustomer.Value)
            throw new AllocationDomainException("Min per customer cannot be greater than max per customer");

        Name = name.Trim();
        Description = description?.Trim();
        MaxPerCustomer = maxPerCustomer;
        MinPerCustomer = minPerCustomer;
        Priority = priority;
    }

    public void UpdateQuantity(int newQuantity)
    {
        if (Scope != AllocationScope.ByQuantity)
            throw new AllocationDomainException("Cannot update quantity for a seat-based allocation.");

        if (newQuantity <= 0)
            throw new AllocationDomainException("Allocation quantity must be greater than zero");

        if (newQuantity < UsedQuantity)
            throw new AllocationDomainException($"Cannot reduce quantity below used quantity ({UsedQuantity})");

        Quantity = newQuantity;
    }

    public void AddSeat(Seat seat)
    {
        if (Scope != AllocationScope.BySeat)
            throw new AllocationDomainException("Cannot add seats to a quantity-based allocation.");

        if (_allocatedSeats.Any(sa => sa.SeatId == seat.Id))
            return; // Seat already in allocation

        _allocatedSeats.Add(new SeatAllocation(Id, seat.Id));
    }

    public void RemoveSeat(Seat seat)
    {
        if (Scope != AllocationScope.BySeat)
            throw new AllocationDomainException("Cannot remove seats from a quantity-based allocation.");

        var seatAllocation = _allocatedSeats.FirstOrDefault(sa => sa.SeatId == seat.Id);
        if (seatAllocation != null)
        {
            _allocatedSeats.Remove(seatAllocation);
        }
    }

    public void UpdateTimeWindow(DateTime? startTime, DateTime? endTime)
    {
        if (startTime.HasValue && endTime.HasValue && startTime.Value >= endTime.Value)
            throw new AllocationDomainException("Start time must be before end time");

        StartTime = startTime;
        EndTime = endTime;
    }

    public void SetAccessCode(string? accessCode)
    {
        AccessCode = accessCode?.Trim();
    }

    public void SetAllowedCustomerSegments(List<string> segments)
    {
        AllowedCustomerSegments = segments?.Where(s => !string.IsNullOrWhiteSpace(s))
                                          .Select(s => s.Trim())
                                          .Distinct()
                                          .ToList() ?? new List<string>();
    }

    public void Enable()
    {
        IsEnabled = true;
    }

    public void Disable()
    {
        IsEnabled = false;
    }

    public bool IsWithinTimeWindow()
    {
        var now = DateTime.UtcNow;

        if (StartTime.HasValue && now < StartTime.Value)
            return false;

        if (EndTime.HasValue && now > EndTime.Value)
            return false;

        return true;
    }

    public bool IsAvailableForCustomer(string? customerSegment = null)
    {
        if (!IsEnabled || !IsWithinTimeWindow() || RemainingQuantity <= 0)
            return false;

        // Check customer segment restrictions
        if (AllowedCustomerSegments.Any() && !string.IsNullOrEmpty(customerSegment))
        {
            return AllowedCustomerSegments.Contains(customerSegment, StringComparer.OrdinalIgnoreCase);
        }

        return true;
    }

    public bool ValidateAccessCode(string? providedCode)
    {
        if (!RequiresAccessCode)
            return true;

        return string.Equals(AccessCode, providedCode, StringComparison.Ordinal);
    }

    public void UseQuantity(int quantity)
    {
        if (quantity <= 0)
            throw new AllocationDomainException("Usage quantity must be greater than zero");

        if (quantity > RemainingQuantity)
            throw new AllocationDomainException($"Cannot use {quantity} tickets. Only {RemainingQuantity} remaining");

        UsedQuantity += quantity;
    }

    public void ReleaseQuantity(int quantity)
    {
        if (quantity <= 0)
            throw new AllocationDomainException("Release quantity must be greater than zero");

        if (quantity > UsedQuantity)
            throw new AllocationDomainException($"Cannot release {quantity} tickets. Only {UsedQuantity} used");

        UsedQuantity -= quantity;
    }

    public void SetMetadata(Dictionary<string, object> metadata)
    {
        Metadata = metadata ?? new Dictionary<string, object>();
    }

    public void AddMetadata(string key, object value)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Metadata key cannot be empty", nameof(key));

        Metadata[key] = value;
    }

    public void RemoveMetadata(string key)
    {
        if (!string.IsNullOrWhiteSpace(key))
        {
            Metadata.Remove(key);
        }
    }

    public string GetStatusDescription()
    {
        if (!IsEnabled)
            return "Disabled";

        if (!IsWithinTimeWindow())
        {
            if (StartTime.HasValue && DateTime.UtcNow < StartTime.Value)
                return "Not Started";
            if (EndTime.HasValue && DateTime.UtcNow > EndTime.Value)
                return "Expired";
        }

        if (RemainingQuantity <= 0)
            return "Fully Used";

        return "Active";
    }
}
