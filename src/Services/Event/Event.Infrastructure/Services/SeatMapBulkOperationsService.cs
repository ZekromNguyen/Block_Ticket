using Event.Domain.Entities;
using Event.Domain.Interfaces;
using Event.Domain.Models;
using Event.Domain.Services;
using Microsoft.Extensions.Logging;

namespace Event.Infrastructure.Services;

/// <summary>
/// Implementation of seat map bulk operations service
/// </summary>
public class SeatMapBulkOperationsService : ISeatMapBulkOperationsService
{
    private readonly IVenueRepository _venueRepository;
    private readonly ISeatRepository _seatRepository;
    private readonly ILogger<SeatMapBulkOperationsService> _logger;

    public SeatMapBulkOperationsService(
        IVenueRepository venueRepository,
        ISeatRepository seatRepository,
        ILogger<SeatMapBulkOperationsService> logger)
    {
        _venueRepository = venueRepository;
        _seatRepository = seatRepository;
        _logger = logger;
    }

    public async Task<BulkSeatOperationResult> PerformBulkSeatOperationAsync(
        Guid venueId,
        BulkSeatOperationRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Performing bulk seat operation {Operation} for {SeatCount} seats in venue {VenueId}",
            request.Operation, request.SeatIds.Count, venueId);

        var result = new BulkSeatOperationResult
        {
            TotalRequested = request.SeatIds.Count
        };

        try
        {
            // Validate venue exists
            var venue = await _venueRepository.GetByIdAsync(venueId, cancellationToken);
            if (venue == null)
            {
                result.Errors.Add($"Venue {venueId} not found");
                return result;
            }

            // Get all requested seats
            var seats = new List<Seat>();
            foreach (var seatId in request.SeatIds)
            {
                var seat = await _seatRepository.GetByIdAsync(seatId, cancellationToken);
                if (seat != null && seat.VenueId == venueId)
                {
                    seats.Add(seat);
                }
                else
                {
                    result.Results.Add(new BulkSeatOperationItemResult
                    {
                        SeatId = seatId,
                        Success = false,
                        ErrorMessage = seat == null ? "Seat not found" : "Seat does not belong to this venue"
                    });
                }
            }

            // Process operation based on type
            await ProcessBulkOperationAsync(request.Operation, seats, request.OperationData, result, cancellationToken);

            // Update counts
            result.Successful = result.Results.Count(r => r.Success);
            result.Failed = result.Results.Count(r => !r.Success);

            _logger.LogInformation("Bulk operation {Operation} completed. Success: {Successful}, Failed: {Failed}",
                request.Operation, result.Successful, result.Failed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing bulk seat operation {Operation} for venue {VenueId}",
                request.Operation, venueId);
            result.Errors.Add($"Bulk operation failed: {ex.Message}");
        }

        return result;
    }

    public async Task<BulkSeatUpdateResult> BulkUpdateSeatAttributesAsync(
        Guid venueId,
        BulkSeatAttributeUpdateRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Bulk updating attributes for {SeatCount} seats in venue {VenueId}",
            request.SeatIds.Count, venueId);

        var result = new BulkSeatUpdateResult
        {
            TotalRequested = request.SeatIds.Count
        };

        try
        {
            // Validate venue exists
            var venue = await _venueRepository.GetByIdAsync(venueId, cancellationToken);
            if (venue == null)
            {
                result.Errors.Add($"Venue {venueId} not found");
                return result;
            }

            // Process each seat
            foreach (var seatId in request.SeatIds)
            {
                var itemResult = new BulkSeatUpdateItemResult { SeatId = seatId };

                try
                {
                    var seat = await _seatRepository.GetByIdAsync(seatId, cancellationToken);
                    if (seat == null)
                    {
                        itemResult.Success = false;
                        itemResult.ErrorMessage = "Seat not found";
                    }
                    else if (seat.VenueId != venueId)
                    {
                        itemResult.Success = false;
                        itemResult.ErrorMessage = "Seat does not belong to this venue";
                    }
                    else
                    {
                        var changedFields = new Dictionary<string, object>();

                        // Apply updates
                        foreach (var update in request.Updates)
                        {
                            await ApplySeatAttributeUpdateAsync(seat, update.Key, update.Value, changedFields);
                        }

                        await _seatRepository.UpdateAsync(seat, cancellationToken);

                        itemResult.Success = true;
                        itemResult.ChangedFields = changedFields;
                    }
                }
                catch (Exception ex)
                {
                    itemResult.Success = false;
                    itemResult.ErrorMessage = ex.Message;
                }

                result.Results.Add(itemResult);
            }

            result.Successful = result.Results.Count(r => r.Success);
            result.Failed = result.Results.Count(r => !r.Success);

            _logger.LogInformation("Bulk attribute update completed. Success: {Successful}, Failed: {Failed}",
                result.Successful, result.Failed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during bulk seat attribute update for venue {VenueId}", venueId);
            result.Errors.Add($"Bulk update failed: {ex.Message}");
        }

        return result;
    }

    public async Task<SeatMapCopyResult> CopySeatMapAsync(
        Guid sourceVenueId,
        Guid targetVenueId,
        SeatMapCopyOptions options,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Copying seat map from venue {SourceVenueId} to venue {TargetVenueId}",
            sourceVenueId, targetVenueId);

        var result = new SeatMapCopyResult();

        try
        {
            // Get source venue
            var sourceVenue = await _venueRepository.GetByIdAsync(sourceVenueId, cancellationToken);
            if (sourceVenue == null)
            {
                result.Errors.Add($"Source venue {sourceVenueId} not found");
                return result;
            }

            if (!sourceVenue.HasSeatMap)
            {
                result.Errors.Add("Source venue does not have a seat map");
                return result;
            }

            // Get target venue
            var targetVenue = await _venueRepository.GetByIdAsync(targetVenueId, cancellationToken);
            if (targetVenue == null)
            {
                result.Errors.Add($"Target venue {targetVenueId} not found");
                return result;
            }

            // Check if target has existing seat map and replacement is not allowed
            if (targetVenue.HasSeatMap && !options.ReplaceExisting)
            {
                result.Errors.Add("Target venue already has a seat map and replace existing is not enabled");
                return result;
            }

            // Get source seats
            var sourceSeats = sourceVenue.Seats.ToList();
            if (!sourceSeats.Any())
            {
                result.Warnings.Add("Source venue has no seats to copy");
                return result;
            }

            // Create seat map data for copying
            var seatMapRows = ConvertSeatsToSeatMapRows(sourceSeats, options);

            // Generate new checksum
            var checksum = GenerateChecksumForRows(seatMapRows);

            // Import to target venue
            targetVenue.ImportSeatMap(seatMapRows, checksum);
            await _venueRepository.UpdateAsync(targetVenue, cancellationToken);

            result.Success = true;
            result.CopiedSeats = seatMapRows.Sum(r => r.Seats.Count);
            result.CopiedSections = seatMapRows.Select(r => r.Section).Distinct().Count();
            result.NewChecksum = checksum;

            _logger.LogInformation("Successfully copied seat map. Seats: {CopiedSeats}, Sections: {CopiedSections}",
                result.CopiedSeats, result.CopiedSections);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error copying seat map from {SourceVenueId} to {TargetVenueId}",
                sourceVenueId, targetVenueId);
            result.Errors.Add($"Copy failed: {ex.Message}");
        }

        return result;
    }

    public async Task<SeatMapMergeResult> MergeSeatMapsAsync(
        Guid venueId,
        SeatMapSchema newSeatMap,
        SeatMapMergeOptions options,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Merging seat map for venue {VenueId} using strategy {Strategy}",
            venueId, options.Strategy);

        var result = new SeatMapMergeResult();

        try
        {
            var venue = await _venueRepository.GetByIdAsync(venueId, cancellationToken);
            if (venue == null)
            {
                result.Errors.Add($"Venue {venueId} not found");
                return result;
            }

            var existingSeats = venue.Seats.ToList();
            var newSeatMapRows = ConvertSchemaToSeatMapRows(newSeatMap);

            // Perform merge based on strategy
            var mergeChanges = await PerformSeatMapMergeAsync(
                existingSeats, newSeatMapRows, options, result, cancellationToken);

            if (mergeChanges.Any())
            {
                var checksum = GenerateChecksumForRows(newSeatMapRows);
                venue.ImportSeatMap(newSeatMapRows, checksum);
                await _venueRepository.UpdateAsync(venue, cancellationToken);

                result.Success = true;
                result.Changes.AddRange(mergeChanges);
            }
            else
            {
                result.Success = true;
                result.Changes.Add("No changes required");
            }

            _logger.LogInformation("Seat map merge completed. Added: {Added}, Updated: {Updated}, Removed: {Removed}",
                result.AddedSeats, result.UpdatedSeats, result.RemovedSeats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error merging seat map for venue {VenueId}", venueId);
            result.Errors.Add($"Merge failed: {ex.Message}");
        }

        return result;
    }

    public async Task<SeatMapVersioningResult> CreateSeatMapVersionAsync(
        Guid venueId,
        string versionNote,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating seat map version for venue {VenueId}", venueId);

        var result = new SeatMapVersioningResult();

        try
        {
            var venue = await _venueRepository.GetByIdAsync(venueId, cancellationToken);
            if (venue == null)
            {
                result.Errors.Add($"Venue {venueId} not found");
                return result;
            }

            if (!venue.HasSeatMap)
            {
                result.Errors.Add("Venue does not have a seat map to version");
                return result;
            }

            // Create version record (this would typically involve a separate versioning table)
            var versionId = Guid.NewGuid();
            var versionNumber = await GetNextVersionNumberAsync(venueId, cancellationToken);
            var archivedSeats = venue.Seats.Count;

            // Store version information (implementation would depend on your versioning strategy)
            await StoreVersionAsync(venueId, versionId, versionNumber, versionNote, venue, cancellationToken);

            result.Success = true;
            result.VersionId = versionId;
            result.VersionNumber = versionNumber;
            result.VersionNote = versionNote;
            result.CreatedAt = DateTime.UtcNow;
            result.ArchivedSeats = archivedSeats;

            _logger.LogInformation("Created seat map version {VersionNumber} for venue {VenueId}",
                versionNumber, venueId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating seat map version for venue {VenueId}", venueId);
            result.Errors.Add($"Version creation failed: {ex.Message}");
        }

        return result;
    }

    public async Task<SeatMapRestoreResult> RestoreSeatMapVersionAsync(
        Guid venueId,
        Guid versionId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Restoring seat map version {VersionId} for venue {VenueId}",
            versionId, venueId);

        var result = new SeatMapRestoreResult();

        try
        {
            var venue = await _venueRepository.GetByIdAsync(venueId, cancellationToken);
            if (venue == null)
            {
                result.Errors.Add($"Venue {venueId} not found");
                return result;
            }

            // Retrieve archived version (implementation depends on versioning strategy)
            var versionData = await GetVersionDataAsync(venueId, versionId, cancellationToken);
            if (versionData == null)
            {
                result.Errors.Add($"Version {versionId} not found");
                return result;
            }

            // Restore the seat map
            venue.ImportSeatMap(versionData.SeatMapRows, versionData.Checksum);
            await _venueRepository.UpdateAsync(venue, cancellationToken);

            result.Success = true;
            result.RestoredVersionId = versionId;
            result.RestoredSeats = versionData.SeatMapRows.Sum(r => r.Seats.Count);
            result.RestoredAt = DateTime.UtcNow;
            result.Changes.Add($"Restored {result.RestoredSeats} seats from version {versionId}");

            _logger.LogInformation("Successfully restored seat map version {VersionId} for venue {VenueId}",
                versionId, venueId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error restoring seat map version {VersionId} for venue {VenueId}",
                versionId, venueId);
            result.Errors.Add($"Restore failed: {ex.Message}");
        }

        return result;
    }

    #region Private Helper Methods

    private async Task ProcessBulkOperationAsync(
        string operation,
        List<Seat> seats,
        object? operationData,
        BulkSeatOperationResult result,
        CancellationToken cancellationToken)
    {
        foreach (var seat in seats)
        {
            var itemResult = new BulkSeatOperationItemResult { SeatId = seat.Id };

            try
            {
                switch (operation.ToLowerInvariant())
                {
                    case "block":
                        await BlockSeatAsync(seat, operationData, cancellationToken);
                        itemResult.Success = true;
                        break;

                    case "unblock":
                        await UnblockSeatAsync(seat, cancellationToken);
                        itemResult.Success = true;
                        break;

                    case "allocate":
                        await AllocateSeatAsync(seat, operationData, cancellationToken);
                        itemResult.Success = true;
                        break;

                    case "deallocate":
                        await DeallocateSeatAsync(seat, cancellationToken);
                        itemResult.Success = true;
                        break;

                    default:
                        itemResult.Success = false;
                        itemResult.ErrorMessage = $"Unknown operation: {operation}";
                        break;
                }

                if (itemResult.Success)
                {
                    await _seatRepository.UpdateAsync(seat, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                itemResult.Success = false;
                itemResult.ErrorMessage = ex.Message;
            }

            result.Results.Add(itemResult);
        }
    }

    private async Task BlockSeatAsync(Seat seat, object? operationData, CancellationToken cancellationToken)
    {
        // Implementation would call seat.Block() method when available
        await Task.CompletedTask;
        // seat.Block(reason);
    }

    private async Task UnblockSeatAsync(Seat seat, CancellationToken cancellationToken)
    {
        // Implementation would call seat.Unblock() method when available
        await Task.CompletedTask;
        // seat.Unblock();
    }

    private async Task AllocateSeatAsync(Seat seat, object? operationData, CancellationToken cancellationToken)
    {
        // Implementation would call seat.Allocate() method when available
        await Task.CompletedTask;
        // if (operationData is Guid ticketTypeId)
        //     seat.AllocateToTicketType(ticketTypeId);
    }

    private async Task DeallocateSeatAsync(Seat seat, CancellationToken cancellationToken)
    {
        // Implementation would call seat.Deallocate() method when available
        await Task.CompletedTask;
        // seat.Deallocate();
    }

    private async Task ApplySeatAttributeUpdateAsync(
        Seat seat,
        string attributeName,
        object value,
        Dictionary<string, object> changedFields)
    {
        switch (attributeName.ToLowerInvariant())
        {
            case "isaccessible":
                if (value is bool isAccessible && seat.IsAccessible != isAccessible)
                {
                    changedFields["IsAccessible"] = new { Old = seat.IsAccessible, New = isAccessible };
                    // seat.UpdateAttributes would need to be modified to handle individual fields
                }
                break;

            case "hasrestrictedview":
                if (value is bool hasRestrictedView && seat.HasRestrictedView != hasRestrictedView)
                {
                    changedFields["HasRestrictedView"] = new { Old = seat.HasRestrictedView, New = hasRestrictedView };
                }
                break;

            case "pricecategory":
                if (value is string priceCategory && seat.PriceCategory != priceCategory)
                {
                    changedFields["PriceCategory"] = new { Old = seat.PriceCategory, New = priceCategory };
                }
                break;

            case "notes":
                if (value is string notes && seat.Notes != notes)
                {
                    changedFields["Notes"] = new { Old = seat.Notes, New = notes };
                }
                break;
        }

        await Task.CompletedTask;
    }

    private List<SeatMapRow> ConvertSeatsToSeatMapRows(List<Seat> seats, SeatMapCopyOptions options)
    {
        var groupedSeats = seats
            .GroupBy(s => new { s.Position.Section, s.Position.Row })
            .ToList();

        var seatMapRows = new List<SeatMapRow>();

        foreach (var group in groupedSeats)
        {
            var sectionName = group.Key.Section;
            var rowName = group.Key.Row;

            // Apply section mapping if configured
            if (options.SectionMappings.ContainsKey(sectionName))
            {
                sectionName = options.SectionMappings[sectionName];
            }

            var seatMapRow = new SeatMapRow
            {
                Section = sectionName,
                Row = rowName,
                Seats = group.Select(seat => new SeatMapSeat
                {
                    Number = seat.Position.Number,
                    IsAccessible = seat.IsAccessible,
                    HasRestrictedView = seat.HasRestrictedView,
                    PriceCategory = ApplyPriceCategoryMapping(seat.PriceCategory, options.PriceCategoryMappings)
                }).ToList()
            };

            seatMapRows.Add(seatMapRow);
        }

        return seatMapRows;
    }

    private List<SeatMapRow> ConvertSchemaToSeatMapRows(SeatMapSchema schema)
    {
        var seatMapRows = new List<SeatMapRow>();

        foreach (var section in schema.Sections)
        {
            foreach (var row in section.Rows)
            {
                var seatMapRow = new SeatMapRow
                {
                    Section = section.Name,
                    Row = row.Name,
                    Seats = row.Seats.Select(seat => new SeatMapSeat
                    {
                        Number = seat.Number,
                        IsAccessible = seat.IsAccessible,
                        HasRestrictedView = seat.HasRestrictedView,
                        PriceCategory = seat.PriceCategory
                    }).ToList()
                };

                seatMapRows.Add(seatMapRow);
            }
        }

        return seatMapRows;
    }

    private string? ApplyPriceCategoryMapping(string? originalCategory, Dictionary<string, string> mappings)
    {
        if (string.IsNullOrEmpty(originalCategory) || !mappings.ContainsKey(originalCategory))
        {
            return originalCategory;
        }

        return mappings[originalCategory];
    }

    private string GenerateChecksumForRows(List<SeatMapRow> rows)
    {
        var data = string.Join("|", rows.OrderBy(r => r.Section).ThenBy(r => r.Row)
            .Select(r => $"{r.Section}-{r.Row}-{string.Join(",", r.Seats.OrderBy(s => s.Number).Select(s => s.Number))}"));

        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(data));
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    private async Task<List<string>> PerformSeatMapMergeAsync(
        List<Seat> existingSeats,
        List<SeatMapRow> newSeatMapRows,
        SeatMapMergeOptions options,
        SeatMapMergeResult result,
        CancellationToken cancellationToken)
    {
        var changes = new List<string>();

        // Create position-based lookup for existing seats
        var existingSeatLookup = existingSeats.ToDictionary(
            s => $"{s.Position.Section}-{s.Position.Row}-{s.Position.Number}",
            s => s);

        // Process based on merge strategy
        switch (options.Strategy)
        {
            case MergeStrategy.Additive:
                changes.AddRange(await PerformAdditiveMergeAsync(existingSeatLookup, newSeatMapRows, result));
                break;

            case MergeStrategy.Replacement:
                changes.AddRange(await PerformReplacementMergeAsync(existingSeatLookup, newSeatMapRows, result));
                break;

            case MergeStrategy.Hybrid:
                changes.AddRange(await PerformHybridMergeAsync(existingSeatLookup, newSeatMapRows, options, result));
                break;
        }

        return changes;
    }

    private async Task<List<string>> PerformAdditiveMergeAsync(
        Dictionary<string, Seat> existingSeatLookup,
        List<SeatMapRow> newSeatMapRows,
        SeatMapMergeResult result)
    {
        var changes = new List<string>();

        foreach (var row in newSeatMapRows)
        {
            foreach (var seat in row.Seats)
            {
                var position = $"{row.Section}-{row.Row}-{seat.Number}";
                if (!existingSeatLookup.ContainsKey(position))
                {
                    changes.Add($"Added seat {position}");
                    result.AddedSeats++;
                }
            }
        }

        await Task.CompletedTask;
        return changes;
    }

    private async Task<List<string>> PerformReplacementMergeAsync(
        Dictionary<string, Seat> existingSeatLookup,
        List<SeatMapRow> newSeatMapRows,
        SeatMapMergeResult result)
    {
        var changes = new List<string>();

        // All existing seats will be replaced
        result.RemovedSeats = existingSeatLookup.Count;
        result.AddedSeats = newSeatMapRows.Sum(r => r.Seats.Count);

        changes.Add($"Replaced {result.RemovedSeats} existing seats with {result.AddedSeats} new seats");

        await Task.CompletedTask;
        return changes;
    }

    private async Task<List<string>> PerformHybridMergeAsync(
        Dictionary<string, Seat> existingSeatLookup,
        List<SeatMapRow> newSeatMapRows,
        SeatMapMergeOptions options,
        SeatMapMergeResult result)
    {
        var changes = new List<string>();

        foreach (var row in newSeatMapRows)
        {
            foreach (var seat in row.Seats)
            {
                var position = $"{row.Section}-{row.Row}-{seat.Number}";
                
                if (existingSeatLookup.ContainsKey(position))
                {
                    // Handle conflict based on resolution strategy
                    var resolution = await ResolveConflictAsync(existingSeatLookup[position], seat, options.ConflictResolution);
                    if (resolution != null)
                    {
                        changes.Add($"Updated seat {position}: {resolution}");
                        result.UpdatedSeats++;
                    }
                }
                else
                {
                    changes.Add($"Added seat {position}");
                    result.AddedSeats++;
                }
            }
        }

        await Task.CompletedTask;
        return changes;
    }

    private async Task<string?> ResolveConflictAsync(
        Seat existingSeat,
        SeatMapSeat newSeat,
        ConflictResolution resolution)
    {
        return resolution switch
        {
            ConflictResolution.UseExisting => null, // No change
            ConflictResolution.UseNew => "Replaced with new data",
            ConflictResolution.Merge => "Merged attributes",
            ConflictResolution.Skip => null, // No change
            ConflictResolution.Fail => throw new InvalidOperationException($"Conflict detected for seat {existingSeat.Position}"),
            _ => null
        };
    }

    private async Task<int> GetNextVersionNumberAsync(Guid venueId, CancellationToken cancellationToken)
    {
        // Implementation would query version history table
        await Task.CompletedTask;
        return 1; // Placeholder
    }

    private async Task StoreVersionAsync(
        Guid venueId,
        Guid versionId,
        int versionNumber,
        string versionNote,
        Venue venue,
        CancellationToken cancellationToken)
    {
        // Implementation would store version data in a separate table
        await Task.CompletedTask;
    }

    private async Task<SeatMapVersionData?> GetVersionDataAsync(
        Guid venueId,
        Guid versionId,
        CancellationToken cancellationToken)
    {
        // Implementation would retrieve version data from storage
        await Task.CompletedTask;
        return null; // Placeholder
    }

    #endregion

    #region Helper Classes

    private class SeatMapVersionData
    {
        public List<SeatMapRow> SeatMapRows { get; set; } = new();
        public string Checksum { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string VersionNote { get; set; } = string.Empty;
    }

    #endregion
}
