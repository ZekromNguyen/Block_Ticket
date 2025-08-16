using FluentValidation;

namespace Event.Application.Features.Events.Commands.CreateEvent;

/// <summary>
/// Validator for CreateEventCommand
/// </summary>
public class CreateEventCommandValidator : AbstractValidator<CreateEventCommand>
{
    public CreateEventCommandValidator()
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
            .WithMessage("Event slug cannot start or end with a hyphen")
            .Must(slug => !slug.Contains("--"))
            .WithMessage("Event slug cannot contain consecutive hyphens");

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

        RuleFor(x => x.PublishEndDate)
            .LessThanOrEqualTo(x => x.EventDate)
            .When(x => x.PublishEndDate.HasValue)
            .WithMessage("Publish end date must be before or equal to event date");

        RuleFor(x => x.Categories)
            .Must(categories => categories.Count <= 10)
            .WithMessage("Cannot have more than 10 categories");

        RuleFor(x => x.Tags)
            .Must(tags => tags.Count <= 20)
            .WithMessage("Cannot have more than 20 tags");

        RuleForEach(x => x.Categories)
            .NotEmpty().WithMessage("Category cannot be empty")
            .MaximumLength(50).WithMessage("Category cannot exceed 50 characters")
            .Matches(@"^[a-zA-Z0-9\s\-_]+$").WithMessage("Category can only contain letters, numbers, spaces, hyphens, and underscores");

        RuleForEach(x => x.Tags)
            .NotEmpty().WithMessage("Tag cannot be empty")
            .MaximumLength(30).WithMessage("Tag cannot exceed 30 characters")
            .Matches(@"^[a-zA-Z0-9\s\-_]+$").WithMessage("Tag can only contain letters, numbers, spaces, hyphens, and underscores");

        When(x => !string.IsNullOrEmpty(x.ImageUrl), () =>
        {
            RuleFor(x => x.ImageUrl)
                .Must(BeValidUrl).WithMessage("Invalid image URL")
                .MaximumLength(500).WithMessage("Image URL cannot exceed 500 characters");
        });

        When(x => !string.IsNullOrEmpty(x.BannerUrl), () =>
        {
            RuleFor(x => x.BannerUrl)
                .Must(BeValidUrl).WithMessage("Invalid banner URL")
                .MaximumLength(500).WithMessage("Banner URL cannot exceed 500 characters");
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

        // Business rules
        RuleFor(x => x)
            .Must(HaveValidPublishWindow)
            .WithMessage("Publish window must be at least 1 hour long")
            .When(x => x.PublishStartDate.HasValue && x.PublishEndDate.HasValue);

        RuleFor(x => x)
            .Must(HaveReasonableEventDate)
            .WithMessage("Event date cannot be more than 2 years in the future");
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

    private static bool HaveValidPublishWindow(CreateEventCommand command)
    {
        if (!command.PublishStartDate.HasValue || !command.PublishEndDate.HasValue)
            return true;

        var duration = command.PublishEndDate.Value - command.PublishStartDate.Value;
        return duration >= TimeSpan.FromHours(1);
    }

    private static bool HaveReasonableEventDate(CreateEventCommand command)
    {
        var maxFutureDate = DateTime.UtcNow.AddYears(2);
        return command.EventDate <= maxFutureDate;
    }
}
