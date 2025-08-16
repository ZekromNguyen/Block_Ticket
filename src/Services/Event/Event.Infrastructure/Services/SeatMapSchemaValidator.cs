using Event.Domain.Models;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Event.Infrastructure.Services;

/// <summary>
/// Service for validating seat map schemas
/// </summary>
public class SeatMapSchemaValidator
{
    private readonly ILogger<SeatMapSchemaValidator> _logger;
    private static readonly Regex SectionNameRegex = new(@"^[A-Za-z0-9\-_\s]+$", RegexOptions.Compiled);
    private static readonly Regex RowNameRegex = new(@"^[A-Za-z0-9\-_\s]+$", RegexOptions.Compiled);
    private static readonly Regex SeatNumberRegex = new(@"^[A-Za-z0-9\-_]+$", RegexOptions.Compiled);

    public SeatMapSchemaValidator(ILogger<SeatMapSchemaValidator> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Validate complete seat map schema
    /// </summary>
    public async Task<SchemaValidationResult> ValidateSchemaAsync(
        SeatMapSchema schema, 
        CancellationToken cancellationToken = default)
    {
        var result = new SchemaValidationResult();
        var metrics = new SchemaValidationMetrics();

        try
        {
            // Basic schema validation
            await ValidateBasicSchemaAsync(schema, result);

            // Venue validation
            await ValidateVenueInfoAsync(schema.Venue, result);

            // Metadata validation
            await ValidateMetadataAsync(schema.Metadata, result);

            // Sections validation
            await ValidateSectionsAsync(schema.Sections, result, metrics);

            // Layout validation (if present)
            if (schema.Layout != null)
            {
                await ValidateLayoutAsync(schema.Layout, result);
            }

            // Accessibility validation (if present)
            if (schema.Accessibility != null)
            {
                await ValidateAccessibilityAsync(schema.Accessibility, result, metrics);
            }

            // Price categories validation
            await ValidatePriceCategoriesAsync(schema.PriceCategories, result);

            // Cross-validation checks
            await PerformCrossValidationAsync(schema, result, metrics);

            // Set final validation status
            result.IsValid = !result.Errors.Any();
            result.Metrics = metrics;

            _logger.LogInformation("Schema validation completed. Valid: {IsValid}, Errors: {ErrorCount}, Warnings: {WarningCount}",
                result.IsValid, result.Errors.Count, result.Warnings.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during schema validation");
            result.Errors.Add($"Validation error: {ex.Message}");
            result.IsValid = false;
        }

        return result;
    }

    /// <summary>
    /// Validate from JSON stream
    /// </summary>
    public async Task<SchemaValidationResult> ValidateFromStreamAsync(
        Stream jsonStream, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var schema = await JsonSerializer.DeserializeAsync<SeatMapSchema>(jsonStream, cancellationToken: cancellationToken);
            if (schema == null)
            {
                return new SchemaValidationResult
                {
                    IsValid = false,
                    Errors = { "Failed to deserialize seat map schema from JSON" }
                };
            }

            return await ValidateSchemaAsync(schema, cancellationToken);
        }
        catch (JsonException jsonEx)
        {
            _logger.LogError(jsonEx, "JSON deserialization error during validation");
            return new SchemaValidationResult
            {
                IsValid = false,
                Errors = { $"JSON format error: {jsonEx.Message}" }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating from stream");
            return new SchemaValidationResult
            {
                IsValid = false,
                Errors = { $"Validation error: {ex.Message}" }
            };
        }
    }

    private async Task ValidateBasicSchemaAsync(SeatMapSchema schema, SchemaValidationResult result)
    {
        // Schema version validation
        if (string.IsNullOrWhiteSpace(schema.SchemaVersion))
        {
            result.Errors.Add("Schema version is required");
        }
        else if (!IsValidSchemaVersion(schema.SchemaVersion))
        {
            result.Errors.Add($"Unsupported schema version: {schema.SchemaVersion}");
        }

        // Data annotations validation
        var validationContext = new ValidationContext(schema);
        var validationResults = new List<ValidationResult>();
        if (!Validator.TryValidateObject(schema, validationContext, validationResults, true))
        {
            foreach (var validationResult in validationResults)
            {
                result.Errors.Add(validationResult.ErrorMessage ?? "Unknown validation error");
            }
        }

        await Task.CompletedTask;
    }

    private async Task ValidateVenueInfoAsync(VenueSchemaInfo venue, SchemaValidationResult result)
    {
        if (string.IsNullOrWhiteSpace(venue.Name))
        {
            result.Errors.Add("Venue name is required");
        }

        if (venue.Name?.Length > 200)
        {
            result.Errors.Add("Venue name cannot exceed 200 characters");
        }

        if (!string.IsNullOrEmpty(venue.Description) && venue.Description.Length > 1000)
        {
            result.Errors.Add("Venue description cannot exceed 1000 characters");
        }

        // Validate address if present
        if (venue.Address != null)
        {
            await ValidateAddressAsync(venue.Address, result);
        }

        // Validate timezone if present
        if (!string.IsNullOrEmpty(venue.TimeZone))
        {
            try
            {
                TimeZoneInfo.FindSystemTimeZoneById(venue.TimeZone);
            }
            catch (TimeZoneNotFoundException)
            {
                result.Warnings.Add($"Unknown timezone: {venue.TimeZone}");
            }
        }

        await Task.CompletedTask;
    }

    private async Task ValidateAddressAsync(AddressSchema address, SchemaValidationResult result)
    {
        if (address.Latitude.HasValue && (address.Latitude < -90 || address.Latitude > 90))
        {
            result.Errors.Add("Latitude must be between -90 and 90 degrees");
        }

        if (address.Longitude.HasValue && (address.Longitude < -180 || address.Longitude > 180))
        {
            result.Errors.Add("Longitude must be between -180 and 180 degrees");
        }

        await Task.CompletedTask;
    }

    private async Task ValidateMetadataAsync(SeatMapMetadataSchema metadata, SchemaValidationResult result)
    {
        if (metadata.TotalSeats < 0)
        {
            result.Errors.Add("Total seats cannot be negative");
        }

        if (metadata.TotalSections < 0)
        {
            result.Errors.Add("Total sections cannot be negative");
        }

        if (metadata.CreatedAt > DateTime.UtcNow.AddDays(1))
        {
            result.Warnings.Add("Created date is in the future");
        }

        if (metadata.UpdatedAt < metadata.CreatedAt)
        {
            result.Errors.Add("Updated date cannot be before created date");
        }

        // Validate checksum format if present
        if (!string.IsNullOrEmpty(metadata.Checksum) && metadata.Checksum.Length != 64)
        {
            result.Warnings.Add("Checksum appears to be in unexpected format (expected 64 characters)");
        }

        await Task.CompletedTask;
    }

    private async Task ValidateSectionsAsync(
        List<SectionSchema> sections, 
        SchemaValidationResult result, 
        SchemaValidationMetrics metrics)
    {
        if (!sections.Any())
        {
            result.Errors.Add("At least one section is required");
            return;
        }

        var sectionNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var totalSeats = 0;
        var accessibleSeats = 0;
        var restrictedViewSeats = 0;
        var seatPositions = new HashSet<string>();

        foreach (var section in sections)
        {
            // Validate section name uniqueness
            if (sectionNames.Contains(section.Name))
            {
                result.Errors.Add($"Duplicate section name: {section.Name}");
            }
            else
            {
                sectionNames.Add(section.Name);
            }

            // Validate section name format
            if (!SectionNameRegex.IsMatch(section.Name))
            {
                result.Errors.Add($"Invalid section name format: {section.Name}");
            }

            // Validate section
            await ValidateSectionAsync(section, result, metrics, seatPositions);

            // Update metrics
            metrics.SeatsBySection[section.Name] = section.Rows.Sum(r => r.Seats.Count);
            totalSeats += section.Rows.Sum(r => r.Seats.Count);
            accessibleSeats += section.Rows.Sum(r => r.Seats.Count(s => s.IsAccessible));
            restrictedViewSeats += section.Rows.Sum(r => r.Seats.Count(s => s.HasRestrictedView));
        }

        // Update final metrics
        metrics.TotalSections = sections.Count;
        metrics.TotalSeats = totalSeats;
        metrics.AccessibleSeats = accessibleSeats;
        metrics.RestrictedViewSeats = restrictedViewSeats;

        await Task.CompletedTask;
    }

    private async Task ValidateSectionAsync(
        SectionSchema section, 
        SchemaValidationResult result, 
        SchemaValidationMetrics metrics,
        HashSet<string> allSeatPositions)
    {
        if (!section.Rows.Any())
        {
            result.Errors.Add($"Section {section.Name} must have at least one row");
            return;
        }

        var rowNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var sectionSeats = 0;

        foreach (var row in section.Rows)
        {
            // Validate row name uniqueness within section
            var rowKey = $"{section.Name}_{row.Name}";
            if (rowNames.Contains(rowKey))
            {
                result.Errors.Add($"Duplicate row name '{row.Name}' in section '{section.Name}'");
            }
            else
            {
                rowNames.Add(rowKey);
            }

            // Validate row name format
            if (!RowNameRegex.IsMatch(row.Name))
            {
                result.Errors.Add($"Invalid row name format: {row.Name} in section {section.Name}");
            }

            // Validate row
            await ValidateRowAsync(section.Name, row, result, allSeatPositions);

            sectionSeats += row.Seats.Count;
            metrics.TotalRows++;
        }

        // Validate section capacity matches actual seats
        if (section.Capacity > 0 && section.Capacity != sectionSeats)
        {
            result.Warnings.Add($"Section {section.Name} capacity ({section.Capacity}) does not match actual seats ({sectionSeats})");
        }

        await Task.CompletedTask;
    }

    private async Task ValidateRowAsync(
        string sectionName, 
        RowSchema row, 
        SchemaValidationResult result,
        HashSet<string> allSeatPositions)
    {
        if (!row.Seats.Any())
        {
            result.Errors.Add($"Row {row.Name} in section {sectionName} must have at least one seat");
            return;
        }

        var seatNumbers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var rowSeats = 0;

        foreach (var seat in row.Seats)
        {
            // Validate seat number uniqueness within row
            if (seatNumbers.Contains(seat.Number))
            {
                result.Errors.Add($"Duplicate seat number '{seat.Number}' in row '{row.Name}', section '{sectionName}'");
            }
            else
            {
                seatNumbers.Add(seat.Number);
            }

            // Validate seat number format
            if (!SeatNumberRegex.IsMatch(seat.Number))
            {
                result.Errors.Add($"Invalid seat number format: {seat.Number} in {sectionName}-{row.Name}");
            }

            // Validate global seat position uniqueness
            var seatPosition = $"{sectionName}-{row.Name}-{seat.Number}";
            if (allSeatPositions.Contains(seatPosition))
            {
                result.Errors.Add($"Duplicate seat position: {seatPosition}");
            }
            else
            {
                allSeatPositions.Add(seatPosition);
            }

            // Validate seat
            await ValidateSeatAsync(sectionName, row.Name, seat, result);

            rowSeats++;
        }

        // Validate row capacity matches actual seats
        if (row.Capacity > 0 && row.Capacity != rowSeats)
        {
            result.Warnings.Add($"Row {row.Name} in section {sectionName} capacity ({row.Capacity}) does not match actual seats ({rowSeats})");
        }

        await Task.CompletedTask;
    }

    private async Task ValidateSeatAsync(
        string sectionName, 
        string rowName, 
        SeatSchema seat, 
        SchemaValidationResult result)
    {
        // Validate seat attributes
        if (!string.IsNullOrEmpty(seat.Notes) && seat.Notes.Length > 500)
        {
            result.Errors.Add($"Seat notes too long for seat {sectionName}-{rowName}-{seat.Number}");
        }

        if (!string.IsNullOrEmpty(seat.PriceCategory) && seat.PriceCategory.Length > 50)
        {
            result.Errors.Add($"Price category name too long for seat {sectionName}-{rowName}-{seat.Number}");
        }

        // Validate status
        var validStatuses = new[] { "Available", "Blocked", "Reserved", "Confirmed", "Maintenance" };
        if (!validStatuses.Contains(seat.Status))
        {
            result.Warnings.Add($"Unknown seat status '{seat.Status}' for seat {sectionName}-{rowName}-{seat.Number}");
        }

        // Validate layout position if present
        if (seat.LayoutPosition != null)
        {
            await ValidatePositionAsync(seat.LayoutPosition, result, $"seat {sectionName}-{rowName}-{seat.Number}");
        }

        await Task.CompletedTask;
    }

    private async Task ValidateLayoutAsync(VenueLayoutSchema layout, SchemaValidationResult result)
    {
        // Validate dimensions
        if (layout.Dimensions != null)
        {
            if (layout.Dimensions.Width <= 0)
            {
                result.Errors.Add("Layout width must be positive");
            }
            if (layout.Dimensions.Height <= 0)
            {
                result.Errors.Add("Layout height must be positive");
            }
        }

        // Validate positions
        if (layout.StagePosition != null)
        {
            await ValidatePositionAsync(layout.StagePosition, result, "stage position");
        }

        foreach (var entryPoint in layout.EntryPoints)
        {
            await ValidatePositionAsync(entryPoint, result, "entry point");
        }

        foreach (var emergencyExit in layout.EmergencyExits)
        {
            await ValidatePositionAsync(emergencyExit, result, "emergency exit");
        }

        await Task.CompletedTask;
    }

    private async Task ValidatePositionAsync(PositionSchema position, SchemaValidationResult result, string context)
    {
        // Basic position validation
        if (double.IsNaN(position.X) || double.IsInfinity(position.X))
        {
            result.Errors.Add($"Invalid X coordinate for {context}");
        }

        if (double.IsNaN(position.Y) || double.IsInfinity(position.Y))
        {
            result.Errors.Add($"Invalid Y coordinate for {context}");
        }

        if (position.Z.HasValue && (double.IsNaN(position.Z.Value) || double.IsInfinity(position.Z.Value)))
        {
            result.Errors.Add($"Invalid Z coordinate for {context}");
        }

        if (position.Rotation.HasValue && (position.Rotation < 0 || position.Rotation >= 360))
        {
            result.Warnings.Add($"Rotation value {position.Rotation} may be out of expected range (0-360) for {context}");
        }

        if (position.Scale.HasValue && position.Scale <= 0)
        {
            result.Errors.Add($"Scale must be positive for {context}");
        }

        await Task.CompletedTask;
    }

    private async Task ValidateAccessibilityAsync(
        AccessibilitySchema accessibility, 
        SchemaValidationResult result, 
        SchemaValidationMetrics metrics)
    {
        if (accessibility.TotalAccessibleSeats < 0)
        {
            result.Errors.Add("Total accessible seats cannot be negative");
        }

        if (accessibility.TotalAccessibleSeats > metrics.TotalSeats)
        {
            result.Errors.Add("Total accessible seats cannot exceed total seats");
        }

        if (accessibility.TotalAccessibleSeats != metrics.AccessibleSeats)
        {
            result.Warnings.Add($"Accessibility total ({accessibility.TotalAccessibleSeats}) does not match seat-level count ({metrics.AccessibleSeats})");
        }

        await Task.CompletedTask;
    }

    private async Task ValidatePriceCategoriesAsync(List<PriceCategorySchema> priceCategories, SchemaValidationResult result)
    {
        if (!priceCategories.Any())
        {
            result.Warnings.Add("No price categories defined");
            return;
        }

        var categoryIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var categoryNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var category in priceCategories)
        {
            // Validate uniqueness
            if (categoryIds.Contains(category.Id))
            {
                result.Errors.Add($"Duplicate price category ID: {category.Id}");
            }
            else
            {
                categoryIds.Add(category.Id);
            }

            if (categoryNames.Contains(category.Name))
            {
                result.Errors.Add($"Duplicate price category name: {category.Name}");
            }
            else
            {
                categoryNames.Add(category.Name);
            }

            // Validate base price
            if (category.BasePrice.HasValue && category.BasePrice < 0)
            {
                result.Errors.Add($"Base price cannot be negative for category {category.Id}");
            }

            // Validate color format if present
            if (!string.IsNullOrEmpty(category.Color) && !IsValidColorFormat(category.Color))
            {
                result.Warnings.Add($"Invalid color format for category {category.Id}: {category.Color}");
            }
        }

        await Task.CompletedTask;
    }

    private async Task PerformCrossValidationAsync(
        SeatMapSchema schema, 
        SchemaValidationResult result, 
        SchemaValidationMetrics metrics)
    {
        // Validate metadata totals match actual counts
        if (schema.Metadata.TotalSeats != metrics.TotalSeats)
        {
            result.Warnings.Add($"Metadata total seats ({schema.Metadata.TotalSeats}) does not match actual count ({metrics.TotalSeats})");
        }

        if (schema.Metadata.TotalSections != metrics.TotalSections)
        {
            result.Warnings.Add($"Metadata total sections ({schema.Metadata.TotalSections}) does not match actual count ({metrics.TotalSections})");
        }

        // Validate price category references
        var definedCategories = new HashSet<string>(schema.PriceCategories.Select(pc => pc.Id), StringComparer.OrdinalIgnoreCase);
        var usedCategories = new HashSet<string>();

        foreach (var section in schema.Sections)
        {
            if (!string.IsNullOrEmpty(section.DefaultPriceCategory))
            {
                usedCategories.Add(section.DefaultPriceCategory);
                if (!definedCategories.Contains(section.DefaultPriceCategory))
                {
                    result.Warnings.Add($"Section {section.Name} references undefined price category: {section.DefaultPriceCategory}");
                }
            }

            foreach (var row in section.Rows)
            {
                foreach (var seat in row.Seats)
                {
                    if (!string.IsNullOrEmpty(seat.PriceCategory))
                    {
                        usedCategories.Add(seat.PriceCategory);
                        if (!definedCategories.Contains(seat.PriceCategory))
                        {
                            result.Warnings.Add($"Seat {section.Name}-{row.Name}-{seat.Number} references undefined price category: {seat.PriceCategory}");
                        }
                    }
                }
            }
        }

        // Check for unused price categories
        var unusedCategories = definedCategories.Except(usedCategories, StringComparer.OrdinalIgnoreCase);
        foreach (var unusedCategory in unusedCategories)
        {
            result.Warnings.Add($"Price category '{unusedCategory}' is defined but not used");
        }

        // Update metrics with price category usage
        foreach (var category in usedCategories)
        {
            var count = 0;
            foreach (var section in schema.Sections)
            {
                count += section.Rows.Sum(r => r.Seats.Count(s => 
                    string.Equals(s.PriceCategory, category, StringComparison.OrdinalIgnoreCase)));
            }
            metrics.SeatsByCategory[category] = count;
        }

        await Task.CompletedTask;
    }

    private static bool IsValidSchemaVersion(string version)
    {
        var supportedVersions = new[] { "1.0", "1.1" };
        return supportedVersions.Contains(version);
    }

    private static bool IsValidColorFormat(string color)
    {
        // Support hex colors (#RGB, #RRGGBB) and common color names
        if (color.StartsWith("#"))
        {
            return Regex.IsMatch(color, @"^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$");
        }

        var commonColors = new[] { "red", "blue", "green", "yellow", "orange", "purple", "black", "white", "gray", "brown" };
        return commonColors.Contains(color.ToLowerInvariant());
    }
}
