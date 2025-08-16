using Event.Domain.Entities;
using Event.Domain.Interfaces;
using Event.Domain.Models;
using Event.Domain.Services;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Event.Infrastructure.Services;

/// <summary>
/// Implementation of seat map import/export service
/// </summary>
public class SeatMapImportExportService : ISeatMapImportExportService
{
    private readonly IVenueRepository _venueRepository;
    private readonly SeatMapSchemaValidator _schemaValidator;
    private readonly ILogger<SeatMapImportExportService> _logger;

    public SeatMapImportExportService(
        IVenueRepository venueRepository,
        SeatMapSchemaValidator schemaValidator,
        ILogger<SeatMapImportExportService> logger)
    {
        _venueRepository = venueRepository;
        _schemaValidator = schemaValidator;
        _logger = logger;
    }

    public async Task<SeatMapImportResult> ImportSeatMapAsync(
        Guid venueId,
        Stream dataStream,
        SeatMapFormat format,
        SeatMapImportOptions options,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting seat map import for venue {VenueId} in format {Format}", venueId, format);

        try
        {
            // Parse the input data based on format
            var schema = await ParseInputDataAsync(dataStream, format, cancellationToken);
            if (schema == null)
            {
                return new SeatMapImportResult
                {
                    Success = false,
                    Errors = { $"Failed to parse seat map data in format {format}" }
                };
            }

            return await ImportSeatMapFromSchemaAsync(venueId, schema, options, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing seat map for venue {VenueId}", venueId);
            return new SeatMapImportResult
            {
                Success = false,
                Errors = { $"Import failed: {ex.Message}" }
            };
        }
    }

    public async Task<SeatMapImportResult> ImportSeatMapFromSchemaAsync(
        Guid venueId,
        SeatMapSchema seatMapSchema,
        SeatMapImportOptions options,
        CancellationToken cancellationToken = default)
    {
        var result = new SeatMapImportResult();
        var importedSeats = 0;

        try
        {
            // Get the venue
            var venue = await _venueRepository.GetByIdAsync(venueId, cancellationToken);
            if (venue == null)
            {
                result.Errors.Add($"Venue {venueId} not found");
                return result;
            }

            // Validate schema if requested
            if (options.ValidateSchema)
            {
                var validationResult = await _schemaValidator.ValidateSchemaAsync(seatMapSchema, cancellationToken);
                result.Errors.AddRange(validationResult.Errors);
                result.Warnings.AddRange(validationResult.Warnings);

                if (!validationResult.IsValid)
                {
                    _logger.LogWarning("Schema validation failed for venue {VenueId}", venueId);
                    return result;
                }
            }

            // Generate checksum
            var checksum = GenerateChecksum(seatMapSchema);

            // Dry run check
            if (options.DryRun)
            {
                result.Success = true;
                result.TotalRows = seatMapSchema.Sections.Sum(s => s.Rows.Count);
                result.ValidRows = result.TotalRows;
                result.ImportedSeats = seatMapSchema.Sections.Sum(s => s.Rows.Sum(r => r.Seats.Count));
                result.Checksum = checksum;
                return result;
            }

            // Convert schema to SeatMapRow format for existing import method
            var seatMapRows = ConvertSchemaToSeatMapRows(seatMapSchema);

            // Preserve existing data if requested
            if (!options.ReplaceExisting && venue.HasSeatMap)
            {
                // Implement merge logic here
                await MergeSeatMapDataAsync(venue, seatMapRows, options);
            }

            // Import the seat map
            venue.ImportSeatMap(seatMapRows, checksum);
            await _venueRepository.UpdateAsync(venue, cancellationToken);

            // Set result
            result.Success = true;
            result.TotalRows = seatMapRows.Count;
            result.ValidRows = seatMapRows.Count;
            result.ImportedSeats = seatMapRows.Sum(r => r.Seats.Count);
            result.Checksum = checksum;

            _logger.LogInformation("Successfully imported seat map for venue {VenueId}. Seats: {SeatCount}",
                venueId, result.ImportedSeats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing seat map from schema for venue {VenueId}", venueId);
            result.Errors.Add($"Import failed: {ex.Message}");
        }

        return result;
    }

    public async Task<SeatMapExportResult> ExportSeatMapAsync(
        Guid venueId,
        SeatMapFormat format,
        SeatMapExportOptions options,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Exporting seat map for venue {VenueId} in format {Format}", venueId, format);

        try
        {
            var venue = await _venueRepository.GetByIdAsync(venueId, cancellationToken);
            if (venue == null)
            {
                return new SeatMapExportResult
                {
                    SeatMapData = new(),
                    TotalSeats = 0,
                    Sections = new(),
                    ExportedAt = DateTime.UtcNow
                };
            }

            if (!venue.HasSeatMap)
            {
                return new SeatMapExportResult
                {
                    SeatMapData = new(),
                    TotalSeats = 0,
                    Sections = new(),
                    ExportedAt = DateTime.UtcNow
                };
            }

            // Get all seats for the venue
            var seats = venue.Seats.ToList();

            // Convert to export format
            var seatMapData = ConvertSeatsToSeatMapRows(seats, options);

            var result = new SeatMapExportResult
            {
                SeatMapData = seatMapData,
                TotalSeats = seats.Count,
                Sections = seats.Select(s => s.Position.Section).Distinct().ToList(),
                Checksum = venue.SeatMapChecksum,
                ExportedAt = DateTime.UtcNow
            };

            _logger.LogInformation("Successfully exported seat map for venue {VenueId}. Seats: {SeatCount}",
                venueId, result.TotalSeats);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting seat map for venue {VenueId}", venueId);
            throw;
        }
    }

    public async Task<Stream> ExportSeatMapToStreamAsync(
        Guid venueId,
        SeatMapFormat format,
        SeatMapExportOptions options,
        CancellationToken cancellationToken = default)
    {
        var exportResult = await ExportSeatMapAsync(venueId, format, options, cancellationToken);
        
        return format switch
        {
            SeatMapFormat.Json => await ConvertToJsonStreamAsync(exportResult, options),
            SeatMapFormat.Csv => await ConvertToCsvStreamAsync(exportResult, options),
            SeatMapFormat.Excel => await ConvertToExcelStreamAsync(exportResult, options),
            _ => await ConvertToJsonStreamAsync(exportResult, options)
        };
    }

    public async Task<SchemaValidationResult> ValidateSeatMapSchemaAsync(
        Stream dataStream,
        SeatMapFormat format,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var schema = await ParseInputDataAsync(dataStream, format, cancellationToken);
            if (schema == null)
            {
                return new SchemaValidationResult
                {
                    IsValid = false,
                    Errors = { $"Failed to parse data in format {format}" }
                };
            }

            return await _schemaValidator.ValidateSchemaAsync(schema, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating seat map schema");
            return new SchemaValidationResult
            {
                IsValid = false,
                Errors = { $"Validation error: {ex.Message}" }
            };
        }
    }

    public async Task<SchemaValidationResult> ValidateSeatMapSchemaAsync(
        SeatMapSchema seatMapSchema,
        CancellationToken cancellationToken = default)
    {
        return await _schemaValidator.ValidateSchemaAsync(seatMapSchema, cancellationToken);
    }

    public async Task<SeatMapImportPreview> PreviewImportAsync(
        Guid venueId,
        Stream dataStream,
        SeatMapFormat format,
        SeatMapImportOptions options,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var schema = await ParseInputDataAsync(dataStream, format, cancellationToken);
            if (schema == null)
            {
                return new SeatMapImportPreview
                {
                    IsValid = false,
                    Errors = { $"Failed to parse data in format {format}" }
                };
            }

            var venue = await _venueRepository.GetByIdAsync(venueId, cancellationToken);
            if (venue == null)
            {
                return new SeatMapImportPreview
                {
                    IsValid = false,
                    Errors = { $"Venue {venueId} not found" }
                };
            }

            // Validate schema
            var validationResult = await _schemaValidator.ValidateSchemaAsync(schema, cancellationToken);

            // Calculate changes
            var totalSeatsToImport = schema.Sections.Sum(s => s.Rows.Sum(r => r.Seats.Count));
            var currentSeats = venue.HasSeatMap ? venue.Seats.Count : 0;

            var preview = new SeatMapImportPreview
            {
                IsValid = validationResult.IsValid,
                TotalSeatsToImport = totalSeatsToImport,
                TotalSeatsToAdd = options.ReplaceExisting ? totalSeatsToImport : Math.Max(0, totalSeatsToImport - currentSeats),
                TotalSeatsToUpdate = options.ReplaceExisting ? 0 : Math.Min(currentSeats, totalSeatsToImport),
                TotalSeatsToRemove = options.ReplaceExisting ? currentSeats : 0,
                ValidationResult = validationResult,
                Errors = validationResult.Errors,
                Warnings = validationResult.Warnings
            };

            if (options.ReplaceExisting)
            {
                preview.Changes.Add($"Will replace existing seat map ({currentSeats} seats) with new seat map ({totalSeatsToImport} seats)");
            }
            else
            {
                preview.Changes.Add($"Will merge with existing seat map. Current: {currentSeats}, Import: {totalSeatsToImport}");
            }

            return preview;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating import preview for venue {VenueId}", venueId);
            return new SeatMapImportPreview
            {
                IsValid = false,
                Errors = { $"Preview generation failed: {ex.Message}" }
            };
        }
    }

    public async Task<List<SeatMapFormatInfo>> GetSupportedFormatsAsync(CancellationToken cancellationToken = default)
    {
        return new List<SeatMapFormatInfo>
        {
            new()
            {
                Format = SeatMapFormat.Json,
                Name = "JSON",
                Description = "JavaScript Object Notation - full schema support",
                FileExtensions = { ".json" },
                SupportsImport = true,
                SupportsExport = true,
                SupportsValidation = true,
                SupportsLayout = true,
                MaxFileSize = 50 * 1024 * 1024, // 50MB
                Capabilities = { ["full_schema"] = true, ["compression"] = true }
            },
            new()
            {
                Format = SeatMapFormat.Csv,
                Name = "CSV",
                Description = "Comma Separated Values - basic seat data only",
                FileExtensions = { ".csv" },
                SupportsImport = true,
                SupportsExport = true,
                SupportsValidation = true,
                SupportsLayout = false,
                MaxFileSize = 10 * 1024 * 1024, // 10MB
                Capabilities = { ["bulk_import"] = true, ["simple_format"] = true }
            },
            new()
            {
                Format = SeatMapFormat.Excel,
                Name = "Excel",
                Description = "Microsoft Excel format - enhanced CSV with multiple sheets",
                FileExtensions = { ".xlsx", ".xls" },
                SupportsImport = true,
                SupportsExport = true,
                SupportsValidation = true,
                SupportsLayout = false,
                MaxFileSize = 25 * 1024 * 1024, // 25MB
                Capabilities = { ["multiple_sheets"] = true, ["templates"] = true }
            },
            new()
            {
                Format = SeatMapFormat.Xml,
                Name = "XML",
                Description = "Extensible Markup Language - structured data format",
                FileExtensions = { ".xml" },
                SupportsImport = true,
                SupportsExport = true,
                SupportsValidation = true,
                SupportsLayout = true,
                MaxFileSize = 20 * 1024 * 1024, // 20MB
                Capabilities = { ["schema_validation"] = true, ["namespaces"] = true }
            }
        };
    }

    public async Task<Stream> GenerateTemplateAsync(
        SeatMapFormat format,
        SeatMapTemplateOptions options,
        CancellationToken cancellationToken = default)
    {
        return format switch
        {
            SeatMapFormat.Json => await GenerateJsonTemplateAsync(options),
            SeatMapFormat.Csv => await GenerateCsvTemplateAsync(options),
            SeatMapFormat.Excel => await GenerateExcelTemplateAsync(options),
            _ => await GenerateJsonTemplateAsync(options)
        };
    }

    public async Task<BulkSeatMapImportResult> BulkImportSeatMapsAsync(
        List<BulkSeatMapImportItem> imports,
        BulkImportOptions options,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting bulk import of {Count} seat maps", imports.Count);

        var result = new BulkSeatMapImportResult
        {
            TotalRequested = imports.Count
        };

        var startTime = DateTime.UtcNow;

        try
        {
            // Validate all first if requested
            if (options.ValidateAllFirst)
            {
                foreach (var import in imports)
                {
                    var validationResult = await ValidateSeatMapSchemaAsync(import.DataStream, import.Format, cancellationToken);
                    if (!validationResult.IsValid)
                    {
                        result.Results.Add(new BulkSeatMapImportItemResult
                        {
                            VenueId = import.VenueId,
                            VenueName = import.VenueName,
                            Success = false,
                            Errors = { $"Validation failed: {string.Join(", ", validationResult.Errors)}" }
                        });

                        if (!options.ContinueOnError)
                        {
                            result.Failed = result.Results.Count(r => !r.Success);
                            result.TotalDuration = DateTime.UtcNow - startTime;
                            return result;
                        }
                    }
                }
            }

            // Process imports with controlled concurrency
            var semaphore = new SemaphoreSlim(options.MaxConcurrentImports, options.MaxConcurrentImports);
            var tasks = imports.Select(async import =>
            {
                await semaphore.WaitAsync(cancellationToken);
                try
                {
                    return await ProcessSingleImportAsync(import, cancellationToken);
                }
                finally
                {
                    semaphore.Release();
                }
            });

            var importResults = await Task.WhenAll(tasks);
            result.Results.AddRange(importResults);

            result.Successful = result.Results.Count(r => r.Success);
            result.Failed = result.Results.Count(r => !r.Success);
            result.TotalDuration = DateTime.UtcNow - startTime;

            _logger.LogInformation("Bulk import completed. Success: {Successful}, Failed: {Failed}, Duration: {Duration}",
                result.Successful, result.Failed, result.TotalDuration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during bulk import");
            result.Failed = result.TotalRequested - result.Successful;
        }

        return result;
    }

    #region Private Helper Methods

    private async Task<SeatMapSchema?> ParseInputDataAsync(Stream dataStream, SeatMapFormat format, CancellationToken cancellationToken)
    {
        return format switch
        {
            SeatMapFormat.Json => await ParseJsonDataAsync(dataStream, cancellationToken),
            SeatMapFormat.Csv => await ParseCsvDataAsync(dataStream, cancellationToken),
            SeatMapFormat.Excel => await ParseExcelDataAsync(dataStream, cancellationToken),
            SeatMapFormat.Xml => await ParseXmlDataAsync(dataStream, cancellationToken),
            _ => throw new NotSupportedException($"Format {format} is not supported")
        };
    }

    private async Task<SeatMapSchema?> ParseJsonDataAsync(Stream dataStream, CancellationToken cancellationToken)
    {
        try
        {
            return await JsonSerializer.DeserializeAsync<SeatMapSchema>(dataStream, cancellationToken: cancellationToken);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Error parsing JSON seat map data");
            return null;
        }
    }

    private async Task<SeatMapSchema?> ParseCsvDataAsync(Stream dataStream, CancellationToken cancellationToken)
    {
        // Implement CSV parsing logic
        // This would involve reading CSV rows and mapping to SeatMapSchema
        await Task.Delay(1, cancellationToken); // Placeholder
        throw new NotImplementedException("CSV parsing not yet implemented");
    }

    private async Task<SeatMapSchema?> ParseExcelDataAsync(Stream dataStream, CancellationToken cancellationToken)
    {
        // Implement Excel parsing logic using libraries like EPPlus or ClosedXML
        await Task.Delay(1, cancellationToken); // Placeholder
        throw new NotImplementedException("Excel parsing not yet implemented");
    }

    private async Task<SeatMapSchema?> ParseXmlDataAsync(Stream dataStream, CancellationToken cancellationToken)
    {
        // Implement XML parsing logic
        await Task.Delay(1, cancellationToken); // Placeholder
        throw new NotImplementedException("XML parsing not yet implemented");
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

    private List<SeatMapRowDto> ConvertSeatsToSeatMapRows(List<Seat> seats, SeatMapExportOptions options)
    {
        return seats.Select(seat => new SeatMapRowDto
        {
            Section = seat.Position.Section,
            Row = seat.Position.Row,
            SeatNumber = seat.Position.Number,
            IsAccessible = seat.IsAccessible,
            HasRestrictedView = seat.HasRestrictedView,
            PriceCategory = seat.PriceCategory,
            Notes = seat.Notes
        }).ToList();
    }

    private string GenerateChecksum(SeatMapSchema schema)
    {
        var json = JsonSerializer.Serialize(schema, new JsonSerializerOptions { WriteIndented = false });
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(json));
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    private async Task MergeSeatMapDataAsync(Venue venue, List<SeatMapRow> newRows, SeatMapImportOptions options)
    {
        // Implement merge logic based on options
        // This would preserve existing seat statuses, allocations, etc.
        await Task.CompletedTask;
    }

    private async Task<Stream> ConvertToJsonStreamAsync(SeatMapExportResult result, SeatMapExportOptions options)
    {
        var json = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        return await Task.FromResult(stream);
    }

    private async Task<Stream> ConvertToCsvStreamAsync(SeatMapExportResult result, SeatMapExportOptions options)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Section,Row,SeatNumber,IsAccessible,HasRestrictedView,PriceCategory,Notes");

        foreach (var row in result.SeatMapData)
        {
            sb.AppendLine($"{row.Section},{row.Row},{row.SeatNumber},{row.IsAccessible},{row.HasRestrictedView},{row.PriceCategory},{row.Notes}");
        }

        var stream = new MemoryStream(Encoding.UTF8.GetBytes(sb.ToString()));
        return await Task.FromResult(stream);
    }

    private async Task<Stream> ConvertToExcelStreamAsync(SeatMapExportResult result, SeatMapExportOptions options)
    {
        // Implement Excel export using EPPlus or similar
        await Task.Delay(1);
        throw new NotImplementedException("Excel export not yet implemented");
    }

    private async Task<Stream> GenerateJsonTemplateAsync(SeatMapTemplateOptions options)
    {
        var template = CreateSampleSeatMapSchema(options);
        var json = JsonSerializer.Serialize(template, new JsonSerializerOptions { WriteIndented = true });
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        return await Task.FromResult(stream);
    }

    private async Task<Stream> GenerateCsvTemplateAsync(SeatMapTemplateOptions options)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Section,Row,SeatNumber,IsAccessible,HasRestrictedView,PriceCategory,Notes");
        
        if (options.IncludeSampleData)
        {
            sb.AppendLine("Orchestra,A,1,false,false,Premium,");
            sb.AppendLine("Orchestra,A,2,false,false,Premium,");
            sb.AppendLine("Balcony,B,1,true,false,Standard,Wheelchair accessible");
        }

        var stream = new MemoryStream(Encoding.UTF8.GetBytes(sb.ToString()));
        return await Task.FromResult(stream);
    }

    private async Task<Stream> GenerateExcelTemplateAsync(SeatMapTemplateOptions options)
    {
        // Implement Excel template generation
        await Task.Delay(1);
        throw new NotImplementedException("Excel template generation not yet implemented");
    }

    private SeatMapSchema CreateSampleSeatMapSchema(SeatMapTemplateOptions options)
    {
        var sampleSeats = options.TemplateSize switch
        {
            "Small" => 50,
            "Medium" => 200,
            "Large" => 1000,
            _ => 200
        };

        return new SeatMapSchema
        {
            SchemaVersion = "1.0",
            Venue = new VenueSchemaInfo
            {
                Name = "Sample Venue",
                Description = "Template venue for seat map import"
            },
            Metadata = new SeatMapMetadataSchema
            {
                TotalSeats = sampleSeats,
                TotalSections = 2,
                Source = "Template Generator"
            },
            Sections = CreateSampleSections(sampleSeats),
            PriceCategories = new List<PriceCategorySchema>
            {
                new() { Id = "premium", Name = "Premium", BasePrice = 100 },
                new() { Id = "standard", Name = "Standard", BasePrice = 50 }
            }
        };
    }

    private List<SectionSchema> CreateSampleSections(int totalSeats)
    {
        var sections = new List<SectionSchema>();
        var seatsPerSection = totalSeats / 2;

        for (int i = 1; i <= 2; i++)
        {
            var section = new SectionSchema
            {
                Name = $"Section{i}",
                Type = i == 1 ? "Orchestra" : "Balcony",
                Capacity = seatsPerSection,
                DefaultPriceCategory = i == 1 ? "premium" : "standard",
                Rows = CreateSampleRows(seatsPerSection)
            };
            sections.Add(section);
        }

        return sections;
    }

    private List<RowSchema> CreateSampleRows(int seatsInSection)
    {
        var rows = new List<RowSchema>();
        var seatsPerRow = 10;
        var rowCount = (int)Math.Ceiling((double)seatsInSection / seatsPerRow);

        for (int i = 1; i <= rowCount; i++)
        {
            var seatsInRow = Math.Min(seatsPerRow, seatsInSection - (i - 1) * seatsPerRow);
            var row = new RowSchema
            {
                Name = ((char)('A' + i - 1)).ToString(),
                Capacity = seatsInRow,
                Seats = CreateSampleSeats(seatsInRow)
            };
            rows.Add(row);
        }

        return rows;
    }

    private List<SeatSchema> CreateSampleSeats(int count)
    {
        var seats = new List<SeatSchema>();

        for (int i = 1; i <= count; i++)
        {
            seats.Add(new SeatSchema
            {
                Number = i.ToString(),
                IsAccessible = i % 20 == 0, // Every 20th seat is accessible
                HasRestrictedView = i % 30 == 0, // Every 30th seat has restricted view
                PriceCategory = i <= count / 2 ? "premium" : "standard"
            });
        }

        return seats;
    }

    private async Task<BulkSeatMapImportItemResult> ProcessSingleImportAsync(
        BulkSeatMapImportItem import,
        CancellationToken cancellationToken)
    {
        var startTime = DateTime.UtcNow;

        try
        {
            var importResult = await ImportSeatMapAsync(
                import.VenueId,
                import.DataStream,
                import.Format,
                import.Options,
                cancellationToken);

            return new BulkSeatMapImportItemResult
            {
                VenueId = import.VenueId,
                VenueName = import.VenueName,
                Success = importResult.Success,
                ImportResult = importResult,
                Errors = importResult.Errors,
                Duration = DateTime.UtcNow - startTime
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing import for venue {VenueId}", import.VenueId);
            return new BulkSeatMapImportItemResult
            {
                VenueId = import.VenueId,
                VenueName = import.VenueName,
                Success = false,
                Errors = { ex.Message },
                Duration = DateTime.UtcNow - startTime
            };
        }
    }

    #endregion
}
