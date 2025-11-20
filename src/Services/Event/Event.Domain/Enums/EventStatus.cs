namespace Event.Domain.Enums;

/// <summary>
/// Represents the status of an event in its lifecycle
/// </summary>
public enum EventStatus
{
    /// <summary>
    /// Event is being created/edited, not visible to public
    /// </summary>
    Draft = 0,
    
    /// <summary>
    /// Event is under review for approval
    /// </summary>
    Review = 1,

    /// <summary>
    /// Event is published and visible to the public
    /// </summary>
    Published = 2,

    /// <summary>
    /// Event has been cancelled
    /// </summary>
    Canceled = 3,

    /// <summary>
    /// Event has been archived and is no longer visible
    /// </summary>
    Archived = 4
}

/// <summary>
/// Represents the type of ticket inventory
/// </summary>
public enum InventoryType
{
    /// <summary>
    /// General admission - no specific seat assignment
    /// </summary>
    GeneralAdmission = 0,
    
    /// <summary>
    /// Reserved seating - specific seat assignments
    /// </summary>
    ReservedSeating = 1
}

/// <summary>
/// Represents the status of a seat in Event Service (seat map definition only)
/// Note: Reservation statuses (Held, Reserved, Confirmed) are managed by Ticket Service
/// </summary>
public enum SeatStatus
{
    /// <summary>
    /// Seat is available for allocation and purchase
    /// </summary>
    Available = 0,

    /// <summary>
    /// Seat is blocked/unavailable (maintenance, damaged, etc.)
    /// </summary>
    Blocked = 1,

    /// <summary>
    /// Seat is temporarily held in a reservation cart.
    /// </summary>
    Held = 2,

    /// <summary>
    /// Seat has been sold and is unavailable.
    /// </summary>
    Sold = 3
}

/// <summary>
/// Represents the type of pricing rule
/// </summary>
public enum PricingRuleType
{
    /// <summary>
    /// Base price for the ticket type
    /// </summary>
    BasePrice = 0,
    
    /// <summary>
    /// Time-based pricing (early bird, regular, etc.)
    /// </summary>
    TimeBased = 1,
    
    /// <summary>
    /// Quantity-based pricing (bulk discounts)
    /// </summary>
    QuantityBased = 2,
    
    /// <summary>
    /// Discount code pricing
    /// </summary>
    DiscountCode = 3,
    
    /// <summary>
    /// Dynamic pricing based on demand
    /// </summary>
    Dynamic = 4
}

/// <summary>
/// Represents the type of discount
/// </summary>
public enum DiscountType
{
    /// <summary>
    /// Fixed amount discount
    /// </summary>
    FixedAmount = 0,
    
    /// <summary>
    /// Percentage discount
    /// </summary>
    Percentage = 1
}

/// <summary>
/// Represents the type of allocation/hold
/// </summary>
public enum AllocationType
{
    /// <summary>
    /// General public allocation
    /// </summary>
    Public = 0,
    
    /// <summary>
    /// Promoter hold
    /// </summary>
    PromoterHold = 1,
    
    /// <summary>
    /// Artist hold
    /// </summary>
    ArtistHold = 2,
    
    /// <summary>
    /// Presale allocation
    /// </summary>
    Presale = 3,
    
    /// <summary>
    /// VIP allocation
    /// </summary>
    VIP = 4,
    
    /// <summary>
    /// Press/Media allocation
    /// </summary>
    Press = 5
}
