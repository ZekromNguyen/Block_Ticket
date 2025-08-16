using Event.Application.Common.Models;
using FluentValidation;

namespace Event.Application.Validators;

/// <summary>
/// Validator for CreateVenueRequest
/// </summary>
public class CreateVenueRequestValidator : AbstractValidator<CreateVenueRequest>
{
    public CreateVenueRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Venue name is required")
            .MaximumLength(200).WithMessage("Venue name cannot exceed 200 characters")
            .MinimumLength(2).WithMessage("Venue name must be at least 2 characters");

        When(x => !string.IsNullOrEmpty(x.Description), () =>
        {
            RuleFor(x => x.Description)
                .MaximumLength(2000).WithMessage("Venue description cannot exceed 2000 characters");
        });

        RuleFor(x => x.Address)
            .NotNull().WithMessage("Address is required")
            .SetValidator(new AddressDtoValidator());

        RuleFor(x => x.TimeZone)
            .NotEmpty().WithMessage("Time zone is required")
            .Must(BeValidTimeZone).WithMessage("Invalid time zone");

        RuleFor(x => x.TotalCapacity)
            .GreaterThan(0).WithMessage("Total capacity must be greater than 0")
            .LessThanOrEqualTo(1000000).WithMessage("Total capacity cannot exceed 1,000,000");

        When(x => !string.IsNullOrEmpty(x.ContactEmail), () =>
        {
            RuleFor(x => x.ContactEmail)
                .EmailAddress().WithMessage("Invalid email address")
                .MaximumLength(100).WithMessage("Contact email cannot exceed 100 characters");
        });

        When(x => !string.IsNullOrEmpty(x.ContactPhone), () =>
        {
            RuleFor(x => x.ContactPhone)
                .MaximumLength(20).WithMessage("Contact phone cannot exceed 20 characters")
                .Matches(@"^[\+]?[0-9\-\(\)\s]+$").WithMessage("Invalid phone number format");
        });

        When(x => !string.IsNullOrEmpty(x.Website), () =>
        {
            RuleFor(x => x.Website)
                .Must(BeValidUrl).WithMessage("Invalid website URL")
                .MaximumLength(200).WithMessage("Website URL cannot exceed 200 characters");
        });
    }

    private static bool BeValidTimeZone(string timeZone)
    {
        try
        {
            TimeZoneInfo.FindSystemTimeZoneById(timeZone);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool BeValidUrl(string? url)
    {
        if (string.IsNullOrEmpty(url))
            return true;

        return Uri.TryCreate(url, UriKind.Absolute, out var result) &&
               (result.Scheme == Uri.UriSchemeHttp || result.Scheme == Uri.UriSchemeHttps);
    }
}

/// <summary>
/// Validator for UpdateVenueRequest
/// </summary>
public class UpdateVenueRequestValidator : AbstractValidator<UpdateVenueRequest>
{
    public UpdateVenueRequestValidator()
    {
        When(x => !string.IsNullOrEmpty(x.Name), () =>
        {
            RuleFor(x => x.Name)
                .MaximumLength(200).WithMessage("Venue name cannot exceed 200 characters")
                .MinimumLength(2).WithMessage("Venue name must be at least 2 characters");
        });

        When(x => !string.IsNullOrEmpty(x.Description), () =>
        {
            RuleFor(x => x.Description)
                .MaximumLength(2000).WithMessage("Venue description cannot exceed 2000 characters");
        });

        When(x => x.Address != null, () =>
        {
            RuleFor(x => x.Address)
                .SetValidator(new AddressDtoValidator()!);
        });

        When(x => !string.IsNullOrEmpty(x.TimeZone), () =>
        {
            RuleFor(x => x.TimeZone)
                .Must(BeValidTimeZone).WithMessage("Invalid time zone");
        });

        When(x => x.TotalCapacity.HasValue, () =>
        {
            RuleFor(x => x.TotalCapacity)
                .GreaterThan(0).WithMessage("Total capacity must be greater than 0")
                .LessThanOrEqualTo(1000000).WithMessage("Total capacity cannot exceed 1,000,000");
        });

        When(x => !string.IsNullOrEmpty(x.ContactEmail), () =>
        {
            RuleFor(x => x.ContactEmail)
                .EmailAddress().WithMessage("Invalid email address")
                .MaximumLength(100).WithMessage("Contact email cannot exceed 100 characters");
        });

        When(x => !string.IsNullOrEmpty(x.ContactPhone), () =>
        {
            RuleFor(x => x.ContactPhone)
                .MaximumLength(20).WithMessage("Contact phone cannot exceed 20 characters")
                .Matches(@"^[\+]?[0-9\-\(\)\s]+$").WithMessage("Invalid phone number format");
        });

        When(x => !string.IsNullOrEmpty(x.Website), () =>
        {
            RuleFor(x => x.Website)
                .Must(BeValidUrl).WithMessage("Invalid website URL")
                .MaximumLength(200).WithMessage("Website URL cannot exceed 200 characters");
        });
    }

    private static bool BeValidTimeZone(string timeZone)
    {
        try
        {
            TimeZoneInfo.FindSystemTimeZoneById(timeZone);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool BeValidUrl(string? url)
    {
        if (string.IsNullOrEmpty(url))
            return true;

        return Uri.TryCreate(url, UriKind.Absolute, out var result) &&
               (result.Scheme == Uri.UriSchemeHttp || result.Scheme == Uri.UriSchemeHttps);
    }
}

/// <summary>
/// Validator for AddressDto
/// </summary>
public class AddressDtoValidator : AbstractValidator<AddressDto>
{
    public AddressDtoValidator()
    {
        RuleFor(x => x.Street)
            .NotEmpty().WithMessage("Street address is required")
            .MaximumLength(200).WithMessage("Street address cannot exceed 200 characters");

        RuleFor(x => x.City)
            .NotEmpty().WithMessage("City is required")
            .MaximumLength(100).WithMessage("City cannot exceed 100 characters");

        RuleFor(x => x.State)
            .NotEmpty().WithMessage("State/Province is required")
            .MaximumLength(100).WithMessage("State/Province cannot exceed 100 characters");

        RuleFor(x => x.PostalCode)
            .NotEmpty().WithMessage("Postal code is required")
            .MaximumLength(20).WithMessage("Postal code cannot exceed 20 characters");

        RuleFor(x => x.Country)
            .NotEmpty().WithMessage("Country is required")
            .MaximumLength(100).WithMessage("Country cannot exceed 100 characters");

        When(x => x.Coordinates != null, () =>
        {
            RuleFor(x => x.Coordinates)
                .SetValidator(new CoordinatesDtoValidator()!);
        });
    }
}

/// <summary>
/// Validator for CoordinatesDto
/// </summary>
public class CoordinatesDtoValidator : AbstractValidator<CoordinatesDto>
{
    public CoordinatesDtoValidator()
    {
        RuleFor(x => x.Latitude)
            .InclusiveBetween(-90, 90).WithMessage("Latitude must be between -90 and 90 degrees");

        RuleFor(x => x.Longitude)
            .InclusiveBetween(-180, 180).WithMessage("Longitude must be between -180 and 180 degrees");
    }
}

/// <summary>
/// Validator for SearchVenuesRequest
/// </summary>
public class SearchVenuesRequestValidator : AbstractValidator<SearchVenuesRequest>
{
    public SearchVenuesRequestValidator()
    {
        RuleFor(x => x.SearchTerm)
            .NotEmpty().WithMessage("Search term is required")
            .MinimumLength(2).WithMessage("Search term must be at least 2 characters")
            .MaximumLength(100).WithMessage("Search term cannot exceed 100 characters");

        RuleFor(x => x.PageNumber)
            .GreaterThan(0).WithMessage("Page number must be greater than 0");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100).WithMessage("Page size must be between 1 and 100");

        When(x => x.MinCapacity.HasValue && x.MaxCapacity.HasValue, () =>
        {
            RuleFor(x => x.MaxCapacity)
                .GreaterThan(x => x.MinCapacity)
                .WithMessage("Max capacity must be greater than min capacity");
        });

        When(x => x.MinCapacity.HasValue, () =>
        {
            RuleFor(x => x.MinCapacity)
                .GreaterThan(0).WithMessage("Min capacity must be greater than 0");
        });

        When(x => x.MaxCapacity.HasValue, () =>
        {
            RuleFor(x => x.MaxCapacity)
                .GreaterThan(0).WithMessage("Max capacity must be greater than 0");
        });

        When(x => x.Latitude.HasValue && x.Longitude.HasValue && x.RadiusKm.HasValue, () =>
        {
            RuleFor(x => x.Latitude)
                .InclusiveBetween(-90, 90).WithMessage("Latitude must be between -90 and 90 degrees");

            RuleFor(x => x.Longitude)
                .InclusiveBetween(-180, 180).WithMessage("Longitude must be between -180 and 180 degrees");

            RuleFor(x => x.RadiusKm)
                .GreaterThan(0).WithMessage("Radius must be greater than 0")
                .LessThanOrEqualTo(1000).WithMessage("Radius cannot exceed 1000 km");
        });
    }
}

/// <summary>
/// Validator for ImportSeatMapRequest
/// </summary>
public class ImportSeatMapRequestValidator : AbstractValidator<ImportSeatMapRequest>
{
    public ImportSeatMapRequestValidator()
    {
        RuleFor(x => x.SeatMapData)
            .NotEmpty().WithMessage("Seat map data is required")
            .Must(data => data.Count <= 100000).WithMessage("Cannot import more than 100,000 seats at once");

        RuleForEach(x => x.SeatMapData)
            .SetValidator(new SeatMapRowDtoValidator());
    }
}

/// <summary>
/// Validator for SeatMapRowDto
/// </summary>
public class SeatMapRowDtoValidator : AbstractValidator<SeatMapRowDto>
{
    public SeatMapRowDtoValidator()
    {
        RuleFor(x => x.Section)
            .NotEmpty().WithMessage("Section is required")
            .MaximumLength(20).WithMessage("Section cannot exceed 20 characters")
            .Matches(@"^[A-Za-z0-9\-_]+$").WithMessage("Section can only contain letters, numbers, hyphens, and underscores");

        RuleFor(x => x.Row)
            .NotEmpty().WithMessage("Row is required")
            .MaximumLength(10).WithMessage("Row cannot exceed 10 characters")
            .Matches(@"^[A-Za-z0-9\-_]+$").WithMessage("Row can only contain letters, numbers, hyphens, and underscores");

        RuleFor(x => x.SeatNumber)
            .NotEmpty().WithMessage("Seat number is required")
            .MaximumLength(10).WithMessage("Seat number cannot exceed 10 characters")
            .Matches(@"^[A-Za-z0-9\-_]+$").WithMessage("Seat number can only contain letters, numbers, hyphens, and underscores");

        When(x => !string.IsNullOrEmpty(x.PriceCategory), () =>
        {
            RuleFor(x => x.PriceCategory)
                .MaximumLength(50).WithMessage("Price category cannot exceed 50 characters");
        });

        When(x => !string.IsNullOrEmpty(x.Notes), () =>
        {
            RuleFor(x => x.Notes)
                .MaximumLength(500).WithMessage("Notes cannot exceed 500 characters");
        });
    }
}
