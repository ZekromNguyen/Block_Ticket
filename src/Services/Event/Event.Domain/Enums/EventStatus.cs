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
    /// Event is published but tickets not yet on sale
    /// </summary>
    Published = 2,
    
    /// <summary>
    /// Tickets are currently on sale
    /// </summary>
    OnSale = 3,
    
    /// <summary>
    /// All tickets have been sold
    /// </summary>
    SoldOut = 4,
    
    /// <summary>
    /// Event has occurred and is completed
    /// </summary>
    Completed = 5,
    
    /// <summary>
    /// Event has been cancelled
    /// </summary>
    Cancelled = 6,
    
    /// <summary>
    /// Event has been archived (soft deleted)
    /// </summary>
    Archived = 7
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
/// Represents the status of a seat
/// </summary>
public enum SeatStatus
{
    /// <summary>
    /// Seat is available for purchase
    /// </summary>
    Available = 0,
    
    /// <summary>
    /// Seat is temporarily held during purchase process
    /// </summary>
    Held = 1,
    
    /// <summary>
    /// Seat is reserved but not yet confirmed
    /// </summary>
    Reserved = 2,
    
    /// <summary>
    /// Seat purchase is confirmed
    /// </summary>
    Confirmed = 3,
    
    /// <summary>
    /// Seat hold/reservation has been released
    /// </summary>
    Released = 4,
    
    /// <summary>
    /// Seat hold/reservation has expired
    /// </summary>
    Expired = 5,
    
    /// <summary>
    /// Seat is blocked/unavailable
    /// </summary>
    Blocked = 6
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
