namespace Event.Domain.Enums;

/// <summary>
/// Reservation status enumeration
/// </summary>
public enum ReservationStatus : byte
{
    /// <summary>
    /// Reservation is pending confirmation
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Reservation is active and confirmed
    /// </summary>
    Active = 1,

    /// <summary>
    /// Reservation has been confirmed/completed
    /// </summary>
    Confirmed = 2,

    /// <summary>
    /// Reservation has expired
    /// </summary>
    Expired = 3,

    /// <summary>
    /// Reservation has been cancelled
    /// </summary>
    Cancelled = 4,

    /// <summary>
    /// Reservation has been refunded
    /// </summary>
    Refunded = 5
}
