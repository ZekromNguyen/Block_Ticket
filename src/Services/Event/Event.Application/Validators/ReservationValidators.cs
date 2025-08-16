using Event.Application.Common.Models;
using FluentValidation;

namespace Event.Application.Validators;

/// <summary>
/// Validator for CreateReservationRequest
/// </summary>
public class CreateReservationRequestValidator : AbstractValidator<CreateReservationRequest>
{
    public CreateReservationRequestValidator()
    {
        RuleFor(x => x.EventId)
            .NotEmpty().WithMessage("Event ID is required");

        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required");

        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("At least one reservation item is required")
            .Must(items => items.Count <= 20).WithMessage("Cannot reserve more than 20 different ticket types at once");

        RuleForEach(x => x.Items)
            .SetValidator(new ReservationItemRequestDtoValidator());

        When(x => !string.IsNullOrEmpty(x.DiscountCode), () =>
        {
            RuleFor(x => x.DiscountCode)
                .MaximumLength(50).WithMessage("Discount code cannot exceed 50 characters")
                .Matches(@"^[A-Za-z0-9\-_]+$").WithMessage("Discount code can only contain letters, numbers, hyphens, and underscores");
        });

        When(x => x.CustomTTL.HasValue, () =>
        {
            RuleFor(x => x.CustomTTL)
                .GreaterThan(TimeSpan.FromMinutes(1)).WithMessage("Custom TTL must be at least 1 minute")
                .LessThanOrEqualTo(TimeSpan.FromHours(24)).WithMessage("Custom TTL cannot exceed 24 hours");
        });
    }
}

/// <summary>
/// Validator for ReservationItemRequestDto
/// </summary>
public class ReservationItemRequestDtoValidator : AbstractValidator<ReservationItemRequestDto>
{
    public ReservationItemRequestDtoValidator()
    {
        RuleFor(x => x.TicketTypeId)
            .NotEmpty().WithMessage("Ticket type ID is required");

        // For general admission tickets
        When(x => x.SeatIds == null || !x.SeatIds.Any(), () =>
        {
            RuleFor(x => x.Quantity)
                .GreaterThan(0).WithMessage("Quantity must be greater than 0 for general admission")
                .LessThanOrEqualTo(50).WithMessage("Cannot reserve more than 50 tickets of the same type at once");
        });

        // For reserved seating tickets
        When(x => x.SeatIds != null && x.SeatIds.Any(), () =>
        {
            RuleFor(x => x.SeatIds)
                .Must(seatIds => seatIds!.Count <= 50).WithMessage("Cannot reserve more than 50 seats at once")
                .Must(seatIds => seatIds!.Distinct().Count() == seatIds!.Count).WithMessage("Cannot reserve duplicate seats");

            RuleFor(x => x.Quantity)
                .Equal(0).WithMessage("Quantity should be 0 when specific seats are selected")
                .When(x => x.SeatIds != null);
        });

        // Ensure either quantity or seat IDs are provided, but not both
        RuleFor(x => x)
            .Must(x => (x.Quantity > 0 && (x.SeatIds == null || !x.SeatIds.Any())) ||
                      (x.Quantity == 0 && x.SeatIds != null && x.SeatIds.Any()))
            .WithMessage("Either specify quantity for general admission or seat IDs for reserved seating, but not both");
    }
}

/// <summary>
/// Validator for CreateTicketTypeRequest
/// </summary>
public class CreateTicketTypeRequestValidator : AbstractValidator<CreateTicketTypeRequest>
{
    public CreateTicketTypeRequestValidator()
    {
        RuleFor(x => x.EventId)
            .NotEmpty().WithMessage("Event ID is required");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Ticket type name is required")
            .MaximumLength(100).WithMessage("Ticket type name cannot exceed 100 characters")
            .MinimumLength(2).WithMessage("Ticket type name must be at least 2 characters");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Ticket type code is required")
            .MaximumLength(20).WithMessage("Ticket type code cannot exceed 20 characters")
            .Matches(@"^[A-Z0-9_]+$").WithMessage("Ticket type code can only contain uppercase letters, numbers, and underscores");

        When(x => !string.IsNullOrEmpty(x.Description), () =>
        {
            RuleFor(x => x.Description)
                .MaximumLength(1000).WithMessage("Description cannot exceed 1000 characters");
        });

        RuleFor(x => x.InventoryType)
            .NotEmpty().WithMessage("Inventory type is required")
            .Must(type => type == "GeneralAdmission" || type == "ReservedSeating")
            .WithMessage("Inventory type must be either 'GeneralAdmission' or 'ReservedSeating'");

        RuleFor(x => x.BasePrice)
            .NotNull().WithMessage("Base price is required")
            .SetValidator(new MoneyDtoValidator());

        When(x => x.ServiceFee != null, () =>
        {
            RuleFor(x => x.ServiceFee)
                .SetValidator(new MoneyDtoValidator()!);
        });

        When(x => x.TaxAmount != null, () =>
        {
            RuleFor(x => x.TaxAmount)
                .SetValidator(new MoneyDtoValidator()!);
        });

        RuleFor(x => x.TotalCapacity)
            .GreaterThan(0).WithMessage("Total capacity must be greater than 0")
            .LessThanOrEqualTo(100000).WithMessage("Total capacity cannot exceed 100,000");

        RuleFor(x => x.MinPurchaseQuantity)
            .GreaterThan(0).WithMessage("Minimum purchase quantity must be greater than 0")
            .LessThanOrEqualTo(x => x.MaxPurchaseQuantity)
            .WithMessage("Minimum purchase quantity cannot exceed maximum purchase quantity");

        RuleFor(x => x.MaxPurchaseQuantity)
            .GreaterThan(0).WithMessage("Maximum purchase quantity must be greater than 0")
            .LessThanOrEqualTo(50).WithMessage("Maximum purchase quantity cannot exceed 50");

        RuleFor(x => x.MaxPerCustomer)
            .GreaterThan(0).WithMessage("Max per customer must be greater than 0")
            .LessThanOrEqualTo(100).WithMessage("Max per customer cannot exceed 100");

        RuleFor(x => x.OnSaleWindows)
            .NotEmpty().WithMessage("At least one on-sale window is required");

        RuleForEach(x => x.OnSaleWindows)
            .SetValidator(new OnSaleWindowDtoValidator());

        // Validate that on-sale windows don't overlap
        RuleFor(x => x.OnSaleWindows)
            .Must(windows => !HasOverlappingWindows(windows))
            .WithMessage("On-sale windows cannot overlap");
    }

    private static bool HasOverlappingWindows(List<OnSaleWindowDto> windows)
    {
        if (windows.Count <= 1) return false;

        var sortedWindows = windows.OrderBy(w => w.StartDate).ToList();
        
        for (int i = 0; i < sortedWindows.Count - 1; i++)
        {
            if (sortedWindows[i].EndDate > sortedWindows[i + 1].StartDate)
                return true;
        }
        
        return false;
    }
}

/// <summary>
/// Validator for MoneyDto
/// </summary>
public class MoneyDtoValidator : AbstractValidator<MoneyDto>
{
    public MoneyDtoValidator()
    {
        RuleFor(x => x.Amount)
            .GreaterThanOrEqualTo(0).WithMessage("Amount must be non-negative")
            .LessThanOrEqualTo(999999.99m).WithMessage("Amount cannot exceed 999,999.99");

        RuleFor(x => x.Currency)
            .NotEmpty().WithMessage("Currency is required")
            .Length(3).WithMessage("Currency must be a 3-letter ISO code")
            .Matches(@"^[A-Z]{3}$").WithMessage("Currency must be uppercase letters only");
    }
}

/// <summary>
/// Validator for OnSaleWindowDto
/// </summary>
public class OnSaleWindowDtoValidator : AbstractValidator<OnSaleWindowDto>
{
    public OnSaleWindowDtoValidator()
    {
        RuleFor(x => x.StartDate)
            .NotEmpty().WithMessage("Start date is required");

        RuleFor(x => x.EndDate)
            .NotEmpty().WithMessage("End date is required")
            .GreaterThan(x => x.StartDate).WithMessage("End date must be after start date");

        RuleFor(x => x.TimeZone)
            .NotEmpty().WithMessage("Time zone is required")
            .Must(BeValidTimeZone).WithMessage("Invalid time zone");

        // Validate that the window is not too short
        RuleFor(x => x)
            .Must(window => window.EndDate - window.StartDate >= TimeSpan.FromMinutes(15))
            .WithMessage("On-sale window must be at least 15 minutes long");

        // Validate that the window is not too long
        RuleFor(x => x)
            .Must(window => window.EndDate - window.StartDate <= TimeSpan.FromDays(365))
            .WithMessage("On-sale window cannot exceed 365 days");
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
}

/// <summary>
/// Validator for CreatePricingRuleRequest
/// </summary>
public class CreatePricingRuleRequestValidator : AbstractValidator<CreatePricingRuleRequest>
{
    public CreatePricingRuleRequestValidator()
    {
        RuleFor(x => x.EventId)
            .NotEmpty().WithMessage("Event ID is required");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Pricing rule name is required")
            .MaximumLength(100).WithMessage("Pricing rule name cannot exceed 100 characters");

        When(x => !string.IsNullOrEmpty(x.Description), () =>
        {
            RuleFor(x => x.Description)
                .MaximumLength(500).WithMessage("Description cannot exceed 500 characters");
        });

        RuleFor(x => x.Type)
            .NotEmpty().WithMessage("Pricing rule type is required")
            .Must(type => new[] { "Discount", "EarlyBird", "LastMinute", "VolumeDiscount", "PromotionalCode" }.Contains(type))
            .WithMessage("Invalid pricing rule type");

        RuleFor(x => x.Priority)
            .InclusiveBetween(1, 100).WithMessage("Priority must be between 1 and 100");

        RuleFor(x => x.EffectiveFrom)
            .NotEmpty().WithMessage("Effective from date is required");

        When(x => x.EffectiveTo.HasValue, () =>
        {
            RuleFor(x => x.EffectiveTo)
                .GreaterThan(x => x.EffectiveFrom)
                .WithMessage("Effective to date must be after effective from date");
        });

        When(x => !string.IsNullOrEmpty(x.DiscountType), () =>
        {
            RuleFor(x => x.DiscountType)
                .Must(type => new[] { "Percentage", "FixedAmount" }.Contains(type))
                .WithMessage("Discount type must be either 'Percentage' or 'FixedAmount'");
        });

        When(x => x.DiscountValue.HasValue, () =>
        {
            RuleFor(x => x.DiscountValue)
                .GreaterThan(0).WithMessage("Discount value must be greater than 0");

            // For percentage discounts
            When(x => x.DiscountType == "Percentage", () =>
            {
                RuleFor(x => x.DiscountValue)
                    .LessThanOrEqualTo(100).WithMessage("Percentage discount cannot exceed 100%");
            });
        });

        When(x => x.MaxDiscountAmount != null, () =>
        {
            RuleFor(x => x.MaxDiscountAmount)
                .SetValidator(new MoneyDtoValidator()!);
        });

        When(x => x.MinOrderAmount != null, () =>
        {
            RuleFor(x => x.MinOrderAmount)
                .SetValidator(new MoneyDtoValidator()!);
        });

        When(x => x.MinQuantity.HasValue, () =>
        {
            RuleFor(x => x.MinQuantity)
                .GreaterThan(0).WithMessage("Minimum quantity must be greater than 0");
        });

        When(x => x.MaxQuantity.HasValue, () =>
        {
            RuleFor(x => x.MaxQuantity)
                .GreaterThan(0).WithMessage("Maximum quantity must be greater than 0")
                .GreaterThanOrEqualTo(x => x.MinQuantity ?? 1)
                .WithMessage("Maximum quantity must be greater than or equal to minimum quantity");
        });

        When(x => !string.IsNullOrEmpty(x.DiscountCode), () =>
        {
            RuleFor(x => x.DiscountCode)
                .MaximumLength(50).WithMessage("Discount code cannot exceed 50 characters")
                .Matches(@"^[A-Z0-9\-_]+$").WithMessage("Discount code can only contain uppercase letters, numbers, hyphens, and underscores");
        });

        When(x => x.MaxUses.HasValue, () =>
        {
            RuleFor(x => x.MaxUses)
                .GreaterThan(0).WithMessage("Maximum uses must be greater than 0");
        });
    }
}
