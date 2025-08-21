namespace Event.Domain.Exceptions;

/// <summary>
/// Base class for all domain exceptions
/// </summary>
public abstract class DomainException : Exception
{
    protected DomainException(string message) : base(message) { }
    protected DomainException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Exception thrown when an event-related business rule is violated
/// </summary>
public class EventDomainException : DomainException
{
    public EventDomainException(string message) : base(message) { }
    public EventDomainException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Exception thrown when a venue-related business rule is violated
/// </summary>
public class VenueDomainException : DomainException
{
    public VenueDomainException(string message) : base(message) { }
    public VenueDomainException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Exception thrown when an inventory-related business rule is violated
/// </summary>
public class InventoryDomainException : DomainException
{
    public InventoryDomainException(string message) : base(message) { }
    public InventoryDomainException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Exception thrown when a pricing-related business rule is violated
/// </summary>
public class PricingDomainException : DomainException
{
    public PricingDomainException(string message) : base(message) { }
    public PricingDomainException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Exception thrown when a reservation-related business rule is violated
/// </summary>
public class ReservationDomainException : DomainException
{
    public ReservationDomainException(string message) : base(message) { }
    public ReservationDomainException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Exception thrown when a seat map-related business rule is violated
/// </summary>
public class SeatMapDomainException : DomainException
{
    public SeatMapDomainException(string message) : base(message) { }
    public SeatMapDomainException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Exception thrown when trying to access an entity that doesn't exist
/// </summary>
public class EntityNotFoundException : DomainException
{
    public string EntityName { get; }
    public object EntityId { get; }

    public EntityNotFoundException(string entityName, object entityId)
        : base($"{entityName} with id '{entityId}' was not found")
    {
        EntityName = entityName;
        EntityId = entityId;
    }
}

/// <summary>
/// Exception thrown when trying to create an entity that already exists
/// </summary>
public class EntityAlreadyExistsException : DomainException
{
    public string EntityName { get; }
    public object EntityId { get; }

    public EntityAlreadyExistsException(string entityName, object entityId)
        : base($"{entityName} with id '{entityId}' already exists")
    {
        EntityName = entityName;
        EntityId = entityId;
    }
}

/// <summary>
/// Exception thrown when an operation is not allowed in the current state
/// </summary>
public class InvalidOperationException : DomainException
{
    public InvalidOperationException(string message) : base(message) { }
    public InvalidOperationException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Exception thrown when a business rule validation fails
/// </summary>
public class BusinessRuleValidationException : DomainException
{
    public string RuleName { get; }

    public BusinessRuleValidationException(string ruleName, string message)
        : base($"Business rule '{ruleName}' validation failed: {message}")
    {
        RuleName = ruleName;
    }
}

/// <summary>
/// Exception thrown when capacity limits are exceeded
/// </summary>
public class CapacityExceededException : DomainException
{
    public int RequestedQuantity { get; }
    public int AvailableQuantity { get; }

    public CapacityExceededException(int requestedQuantity, int availableQuantity)
        : base($"Requested quantity {requestedQuantity} exceeds available capacity {availableQuantity}")
    {
        RequestedQuantity = requestedQuantity;
        AvailableQuantity = availableQuantity;
    }
}

/// <summary>
/// Exception thrown when a seat is not available for reservation
/// </summary>
public class SeatNotAvailableException : DomainException
{
    public Guid SeatId { get; }
    public string SeatPosition { get; }

    public SeatNotAvailableException(Guid seatId, string seatPosition)
        : base($"Seat {seatPosition} (ID: {seatId}) is not available for reservation")
    {
        SeatId = seatId;
        SeatPosition = seatPosition;
    }
}

/// <summary>
/// Exception thrown when a discount code is invalid or expired
/// </summary>
public class InvalidDiscountCodeException : DomainException
{
    public string DiscountCode { get; }

    public InvalidDiscountCodeException(string discountCode, string reason)
        : base($"Discount code '{discountCode}' is invalid: {reason}")
    {
        DiscountCode = discountCode;
    }
}

/// <summary>
/// Exception thrown when a concurrency conflict occurs (optimistic concurrency control)
/// </summary>
public class ConcurrencyException : DomainException
{
    public int ExpectedVersion { get; }
    public int ActualVersion { get; }

    public ConcurrencyException(string message) : base(message) { }

    public ConcurrencyException(int expectedVersion, int actualVersion)
        : base($"Concurrency conflict: Expected version {expectedVersion}, but current version is {actualVersion}")
    {
        ExpectedVersion = expectedVersion;
        ActualVersion = actualVersion;
    }
}
