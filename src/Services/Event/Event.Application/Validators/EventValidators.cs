using Event.Application.Common.Models;
using FluentValidation;

namespace Event.Application.Validators;

/// <summary>
/// Validator for CreateEventRequest
/// </summary>
public class CreateEventRequestValidator : AbstractValidator<CreateEventRequest>
{
    public CreateEventRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Event title is required")
            .MaximumLength(200).WithMessage("Event title cannot exceed 200 characters")
            .MinimumLength(3).WithMessage("Event title must be at least 3 characters");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Event description is required")
            .MaximumLength(5000).WithMessage("Event description cannot exceed 5000 characters")
            .MinimumLength(10).WithMessage("Event description must be at least 10 characters");

        RuleFor(x => x.Slug)
            .NotEmpty().WithMessage("Event slug is required")
            .MaximumLength(100).WithMessage("Event slug cannot exceed 100 characters")
            .Matches(@"^[a-z0-9-]+$").WithMessage("Event slug can only contain lowercase letters, numbers, and hyphens")
            .Must(slug => !slug.StartsWith("-") && !slug.EndsWith("-"))
            .WithMessage("Event slug cannot start or end with a hyphen");

        RuleFor(x => x.OrganizationId)
            .NotEmpty().WithMessage("Organization ID is required");

        RuleFor(x => x.PromoterId)
            .NotEmpty().WithMessage("Promoter ID is required");

        RuleFor(x => x.VenueId)
            .NotEmpty().WithMessage("Venue ID is required");

        RuleFor(x => x.EventDate)
            .GreaterThan(DateTime.UtcNow.AddHours(1))
            .WithMessage("Event date must be at least 1 hour in the future");

        RuleFor(x => x.TimeZone)
            .NotEmpty().WithMessage("Time zone is required")
            .Must(BeValidTimeZone).WithMessage("Invalid time zone");

        RuleFor(x => x.PublishStartDate)
            .LessThan(x => x.EventDate)
            .When(x => x.PublishStartDate.HasValue)
            .WithMessage("Publish start date must be before event date");

        RuleFor(x => x.PublishEndDate)
            .GreaterThan(x => x.PublishStartDate)
            .When(x => x.PublishEndDate.HasValue && x.PublishStartDate.HasValue)
            .WithMessage("Publish end date must be after publish start date");

        RuleFor(x => x.Categories)
            .Must(categories => categories.Count <= 10)
            .WithMessage("Cannot have more than 10 categories");

        RuleFor(x => x.Tags)
            .Must(tags => tags.Count <= 20)
            .WithMessage("Cannot have more than 20 tags");

        RuleForEach(x => x.Categories)
            .NotEmpty().WithMessage("Category cannot be empty")
            .MaximumLength(50).WithMessage("Category cannot exceed 50 characters");

        RuleForEach(x => x.Tags)
            .NotEmpty().WithMessage("Tag cannot be empty")
            .MaximumLength(30).WithMessage("Tag cannot exceed 30 characters");

        When(x => !string.IsNullOrEmpty(x.ImageUrl), () =>
        {
            RuleFor(x => x.ImageUrl)
                .Must(BeValidUrl).WithMessage("Invalid image URL");
        });

        When(x => !string.IsNullOrEmpty(x.BannerUrl), () =>
        {
            RuleFor(x => x.BannerUrl)
                .Must(BeValidUrl).WithMessage("Invalid banner URL");
        });

        When(x => !string.IsNullOrEmpty(x.SeoTitle), () =>
        {
            RuleFor(x => x.SeoTitle)
                .MaximumLength(60).WithMessage("SEO title cannot exceed 60 characters");
        });

        When(x => !string.IsNullOrEmpty(x.SeoDescription), () =>
        {
            RuleFor(x => x.SeoDescription)
                .MaximumLength(160).WithMessage("SEO description cannot exceed 160 characters");
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
/// Validator for UpdateEventRequest
/// </summary>
public class UpdateEventRequestValidator : AbstractValidator<UpdateEventRequest>
{
    public UpdateEventRequestValidator()
    {
        When(x => !string.IsNullOrEmpty(x.Title), () =>
        {
            RuleFor(x => x.Title)
                .MaximumLength(200).WithMessage("Event title cannot exceed 200 characters")
                .MinimumLength(3).WithMessage("Event title must be at least 3 characters");
        });

        When(x => !string.IsNullOrEmpty(x.Description), () =>
        {
            RuleFor(x => x.Description)
                .MaximumLength(5000).WithMessage("Event description cannot exceed 5000 characters")
                .MinimumLength(10).WithMessage("Event description must be at least 10 characters");
        });

        When(x => x.EventDate.HasValue, () =>
        {
            RuleFor(x => x.EventDate)
                .GreaterThan(DateTime.UtcNow.AddHours(1))
                .WithMessage("Event date must be at least 1 hour in the future");
        });

        When(x => !string.IsNullOrEmpty(x.TimeZone), () =>
        {
            RuleFor(x => x.TimeZone)
                .Must(BeValidTimeZone).WithMessage("Invalid time zone");
        });

        When(x => x.Categories != null, () =>
        {
            RuleFor(x => x.Categories)
                .Must(categories => categories!.Count <= 10)
                .WithMessage("Cannot have more than 10 categories");

            RuleForEach(x => x.Categories)
                .NotEmpty().WithMessage("Category cannot be empty")
                .MaximumLength(50).WithMessage("Category cannot exceed 50 characters");
        });

        When(x => x.Tags != null, () =>
        {
            RuleFor(x => x.Tags)
                .Must(tags => tags!.Count <= 20)
                .WithMessage("Cannot have more than 20 tags");

            RuleForEach(x => x.Tags)
                .NotEmpty().WithMessage("Tag cannot be empty")
                .MaximumLength(30).WithMessage("Tag cannot exceed 30 characters");
        });

        When(x => !string.IsNullOrEmpty(x.ImageUrl), () =>
        {
            RuleFor(x => x.ImageUrl)
                .Must(BeValidUrl).WithMessage("Invalid image URL");
        });

        When(x => !string.IsNullOrEmpty(x.BannerUrl), () =>
        {
            RuleFor(x => x.BannerUrl)
                .Must(BeValidUrl).WithMessage("Invalid banner URL");
        });

        When(x => !string.IsNullOrEmpty(x.SeoTitle), () =>
        {
            RuleFor(x => x.SeoTitle)
                .MaximumLength(60).WithMessage("SEO title cannot exceed 60 characters");
        });

        When(x => !string.IsNullOrEmpty(x.SeoDescription), () =>
        {
            RuleFor(x => x.SeoDescription)
                .MaximumLength(160).WithMessage("SEO description cannot exceed 160 characters");
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
/// Validator for SearchEventsRequest
/// </summary>
public class SearchEventsRequestValidator : AbstractValidator<SearchEventsRequest>
{
    public SearchEventsRequestValidator()
    {
        When(x => !string.IsNullOrEmpty(x.SearchTerm), () =>
        {
            RuleFor(x => x.SearchTerm)
                .MinimumLength(2).WithMessage("Search term must be at least 2 characters")
                .MaximumLength(100).WithMessage("Search term cannot exceed 100 characters");
        });

        RuleFor(x => x.PageNumber)
            .GreaterThan(0).WithMessage("Page number must be greater than 0");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100).WithMessage("Page size must be between 1 and 100");

        When(x => x.StartDate.HasValue && x.EndDate.HasValue, () =>
        {
            RuleFor(x => x.EndDate)
                .GreaterThan(x => x.StartDate)
                .WithMessage("End date must be after start date");
        });

        When(x => x.MinPrice.HasValue && x.MaxPrice.HasValue, () =>
        {
            RuleFor(x => x.MaxPrice)
                .GreaterThan(x => x.MinPrice)
                .WithMessage("Max price must be greater than min price");
        });

        When(x => x.MinPrice.HasValue, () =>
        {
            RuleFor(x => x.MinPrice)
                .GreaterThanOrEqualTo(0).WithMessage("Min price must be non-negative");
        });

        When(x => x.MaxPrice.HasValue, () =>
        {
            RuleFor(x => x.MaxPrice)
                .GreaterThan(0).WithMessage("Max price must be greater than 0");
        });
    }
}

/// <summary>
/// Validator for GetEventsRequest
/// </summary>
public class GetEventsRequestValidator : AbstractValidator<GetEventsRequest>
{
    public GetEventsRequestValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThan(0).WithMessage("Page number must be greater than 0");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100).WithMessage("Page size must be between 1 and 100");

        When(x => x.StartDate.HasValue && x.EndDate.HasValue, () =>
        {
            RuleFor(x => x.EndDate)
                .GreaterThan(x => x.StartDate)
                .WithMessage("End date must be after start date");
        });
    }
}
