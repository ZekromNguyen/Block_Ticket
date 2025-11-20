using FluentValidation;
using Event.Application.Features.MarketingCampaigns.Commands;
using Event.Application.Features.MarketingCampaigns.Queries;
using System;
using Event.Domain.Enums;

namespace Event.Application.Validators
{
    public class CreateMarketingCampaignCommandValidator : AbstractValidator<CreateMarketingCampaignCommand>
    {
        public CreateMarketingCampaignCommandValidator()
        {
            RuleFor(v => v.Name)
                .NotEmpty().WithMessage("Name is required.")
                .MaximumLength(200).WithMessage("Name must not exceed 200 characters.");

            RuleFor(v => v.Description)
                .MaximumLength(1000).WithMessage("Description must not exceed 1000 characters.");

            RuleFor(v => v.StartDate)
                .NotEmpty().WithMessage("Start date is required.");

            RuleFor(v => v.EndDate)
                .GreaterThanOrEqualTo(v => v.StartDate).WithMessage("End date must be after start date.")
                .When(v => v.EndDate.HasValue);

            RuleFor(v => v.OrganizationId)
                .NotEmpty().WithMessage("Organization ID is required.");
        }
    }

    public class UpdateMarketingCampaignCommandValidator : AbstractValidator<UpdateMarketingCampaignCommand>
    {
        public UpdateMarketingCampaignCommandValidator()
        {
            RuleFor(v => v.Id)
                .NotEmpty().WithMessage("Id is required.");

            RuleFor(v => v.Name)
                .NotEmpty().WithMessage("Name is required.")
                .MaximumLength(200).WithMessage("Name must not exceed 200 characters.");

            RuleFor(v => v.Description)
                .MaximumLength(1000).WithMessage("Description must not exceed 1000 characters.");

            RuleFor(v => v.StartDate)
                .NotEmpty().WithMessage("Start date is required.");

            RuleFor(v => v.EndDate)
                .GreaterThanOrEqualTo(v => v.StartDate).WithMessage("End date must be after start date.")
                .When(v => v.EndDate.HasValue);

            RuleFor(v => v.OrganizationId)
                .NotEmpty().WithMessage("Organization ID is required.");
        }
    }

    public class DeleteMarketingCampaignCommandValidator : AbstractValidator<DeleteMarketingCampaignCommand>
    {
        public DeleteMarketingCampaignCommandValidator()
        {
            RuleFor(v => v.Id)
                .NotEmpty().WithMessage("Id is required.");

            RuleFor(v => v.OrganizationId)
                .NotEmpty().WithMessage("Organization ID is required.");
        }
    }

    public class SearchMarketingCampaignsQueryValidator : AbstractValidator<SearchMarketingCampaignsQuery>
    {
        public SearchMarketingCampaignsQueryValidator()
        {
            RuleFor(v => v.OrganizationId)
                .NotEmpty().WithMessage("Organization ID is required.");

            RuleFor(v => v.PageNumber)
                .GreaterThan(0).WithMessage("Page number must be greater than 0.");

            RuleFor(v => v.PageSize)
                .GreaterThan(0).WithMessage("Page size must be greater than 0.");

            RuleFor(v => v.Status)
                .Must(BeAValidCampaignStatus).WithMessage("Invalid campaign status.")
                .When(v => !string.IsNullOrEmpty(v.Status));
        }

        private bool BeAValidCampaignStatus(string? status)
        {
            return Enum.TryParse(typeof(CampaignStatus), status, true, out _);
        }
    }
}