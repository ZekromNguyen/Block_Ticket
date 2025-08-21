using Event.Domain.Events;
using Event.Domain.Exceptions;
using Event.Domain.ValueObjects;
using Shared.Common.Models;

namespace Event.Domain.Entities;

/// <summary>
/// Represents a venue where events can be held
/// </summary>
public class Venue : BaseAuditableEntity
{
    private readonly List<Seat> _seats = new();

    // Basic Properties
    public Guid OrganizationId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public Address Address { get; private set; } = null!;
    public TimeZoneId TimeZone { get; private set; } = null!;
    public int TotalCapacity { get; private set; }

    // Address Components (for backward compatibility with application layer)
    public string City => Address?.City ?? string.Empty;
    public string State => Address?.State ?? string.Empty;
    public string Country => Address?.Country ?? string.Empty;
    public string PostalCode => Address?.PostalCode ?? string.Empty;

    // Contact Information
    public string? ContactEmail { get; private set; }
    public string? ContactPhone { get; private set; }
    public string? Website { get; private set; }

    // Additional contact properties (for backward compatibility)
    public string? Phone => ContactPhone;
    public string? Email => ContactEmail;
    public int Capacity => TotalCapacity;
    
    // Seat Map
    public bool HasSeatMap { get; private set; }
    public string? SeatMapMetadata { get; private set; } // JSON
    public string? SeatMapChecksum { get; private set; }
    public DateTime? SeatMapLastUpdated { get; private set; }

    // Additional seat map properties (for backward compatibility)
    public string? SeatMap => SeatMapMetadata; // Alias for SeatMapMetadata
    public string? SeatMapVersion { get; private set; } = "1.0";
    
    // Navigation Properties
    public IReadOnlyCollection<Seat> Seats => _seats.AsReadOnly();

    // For EF Core
    private Venue() { }

    public Venue(
        Guid organizationId,
        string name,
        Address address,
        TimeZoneId timeZone,
        int totalCapacity,
        string? description = null)
    {
        if (organizationId == Guid.Empty)
            throw new VenueDomainException("Organization ID cannot be empty");

        if (string.IsNullOrWhiteSpace(name))
            throw new VenueDomainException("Venue name cannot be empty");

        if (totalCapacity <= 0)
            throw new VenueDomainException("Venue capacity must be greater than zero");

        OrganizationId = organizationId;
        Name = name.Trim();
        Description = description?.Trim();
        Address = address;
        TimeZone = timeZone;
        TotalCapacity = totalCapacity;
        HasSeatMap = false;

        AddDomainEvent(new VenueCreatedDomainEvent(Id, Name, Address.GetFullAddress(), TotalCapacity));
    }

    public void UpdateBasicInfo(string name, string? description, Address address)
    {
        var changes = new Dictionary<string, object>();
        
        if (Name != name.Trim())
        {
            changes["Name"] = new { Old = Name, New = name.Trim() };
            Name = name.Trim();
        }
        
        if (Description != description?.Trim())
        {
            changes["Description"] = new { Old = Description, New = description?.Trim() };
            Description = description?.Trim();
        }
        
        if (!Address.Equals(address))
        {
            changes["Address"] = new { Old = Address.GetFullAddress(), New = address.GetFullAddress() };
            Address = address;
        }

        if (changes.Any())
        {
            AddDomainEvent(new VenueUpdatedDomainEvent(Id, Name, changes));
        }
    }

    public void SetContactInfo(string? email, string? phone, string? website)
    {
        ContactEmail = email?.Trim();
        ContactPhone = phone?.Trim();
        Website = website?.Trim();
    }

    public void UpdateCapacity(int newCapacity)
    {
        if (newCapacity <= 0)
            throw new VenueDomainException("Venue capacity must be greater than zero");
        
        if (HasSeatMap && newCapacity != _seats.Count)
            throw new VenueDomainException("Cannot change capacity when seat map exists. Update seat map instead.");

        TotalCapacity = newCapacity;
    }

    public void ImportSeatMap(List<SeatMapRow> seatMapData, string checksum)
    {
        if (!seatMapData.Any())
            throw new SeatMapDomainException("Seat map data cannot be empty");

        // Clear existing seats
        _seats.Clear();

        var totalSeats = 0;
        var seatPositions = new HashSet<string>();

        foreach (var row in seatMapData)
        {
            ValidateSeatMapRow(row);
            
            foreach (var seatData in row.Seats)
            {
                var position = new SeatPosition(row.Section, row.Row, seatData.Number);
                var positionKey = position.ToString();
                
                if (seatPositions.Contains(positionKey))
                    throw new SeatMapDomainException($"Duplicate seat position: {position.GetDisplayName()}");
                
                seatPositions.Add(positionKey);
                
                var seat = new Seat(
                    Id,
                    position,
                    seatData.IsAccessible,
                    seatData.HasRestrictedView,
                    seatData.PriceCategory);
                
                _seats.Add(seat);
                totalSeats++;
            }
        }

        // Update venue properties
        TotalCapacity = totalSeats;
        HasSeatMap = true;
        SeatMapChecksum = checksum;
        SeatMapLastUpdated = DateTime.UtcNow;
        
        // Store metadata as JSON
        SeatMapMetadata = System.Text.Json.JsonSerializer.Serialize(new
        {
            ImportedAt = DateTime.UtcNow,
            TotalSeats = totalSeats,
            Sections = seatMapData.Select(r => r.Section).Distinct().ToList(),
            Rows = seatMapData.Count,
            Checksum = checksum
        });

        AddDomainEvent(new SeatMapImportedDomainEvent(Id, Name, totalSeats, DateTime.UtcNow));
    }

    public void ClearSeatMap()
    {
        if (!HasSeatMap)
            return;

        _seats.Clear();
        HasSeatMap = false;
        SeatMapMetadata = null;
        SeatMapChecksum = null;
        SeatMapLastUpdated = null;
        
        // Reset to general admission capacity
        // This should be set based on business rules
        TotalCapacity = 1000; // Default GA capacity
    }

    public Seat? GetSeat(SeatPosition position)
    {
        return _seats.FirstOrDefault(s => s.Position.Equals(position));
    }

    public Seat? GetSeat(Guid seatId)
    {
        return _seats.FirstOrDefault(s => s.Id == seatId);
    }

    public List<Seat> GetSeatsBySection(string section)
    {
        return _seats.Where(s => s.Position.Section.Equals(section, StringComparison.OrdinalIgnoreCase))
                    .OrderBy(s => s.Position.Row)
                    .ThenBy(s => s.Position.Number)
                    .ToList();
    }

    public List<string> GetSections()
    {
        return _seats.Select(s => s.Position.Section)
                    .Distinct()
                    .OrderBy(s => s)
                    .ToList();
    }

    public bool ValidateSeatMapIntegrity()
    {
        if (!HasSeatMap)
            return true;

        // Check for duplicate positions
        var positions = _seats.Select(s => s.Position.ToString()).ToList();
        return positions.Count == positions.Distinct().Count();
    }

    private static void ValidateSeatMapRow(SeatMapRow row)
    {
        if (string.IsNullOrWhiteSpace(row.Section))
            throw new SeatMapDomainException("Section cannot be empty");
        
        if (string.IsNullOrWhiteSpace(row.Row))
            throw new SeatMapDomainException("Row cannot be empty");
        
        if (!row.Seats.Any())
            throw new SeatMapDomainException($"Row {row.Section}-{row.Row} must have at least one seat");
        
        foreach (var seat in row.Seats)
        {
            if (string.IsNullOrWhiteSpace(seat.Number))
                throw new SeatMapDomainException($"Seat number cannot be empty in row {row.Section}-{row.Row}");
        }
    }
}

/// <summary>
/// Represents a row of seats in the seat map import data
/// </summary>
public class SeatMapRow
{
    public string Section { get; set; } = string.Empty;
    public string Row { get; set; } = string.Empty;
    public List<SeatMapSeat> Seats { get; set; } = new();
}

/// <summary>
/// Represents seat data in the seat map import
/// </summary>
public class SeatMapSeat
{
    public string Number { get; set; } = string.Empty;
    public bool IsAccessible { get; set; }
    public bool HasRestrictedView { get; set; }
    public string? PriceCategory { get; set; }
}
