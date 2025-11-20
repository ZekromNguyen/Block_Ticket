using FluentValidation;
using Event.Application.Features.MarketingAssets.Commands;
using Event.Application.Features.MarketingAssets.Queries;
using System;
using Event.Domain.Enums;

namespace Event.Application.Validators
{
    public class CreateMarketingAssetCommandValidator : AbstractValidator<CreateMarketingAssetCommand>
    {
        public CreateMarketingAssetCommandValidator()
        {
            RuleFor(v => v.Name)
                .NotEmpty().WithMessage("Name is required.")
                .MaximumLength(200).WithMessage("Name must not exceed 200 characters.");

            RuleFor(v => v.Description)
                .MaximumLength(1000).WithMessage("Description must not exceed 1000 characters.");

            RuleFor(v => v.Type)
                .NotEmpty().WithMessage("Asset type is required.")
                .Must(BeAValidAssetType).WithMessage("Invalid asset type.");

            RuleFor(v => v.File)
                .NotNull().WithMessage("File is required.")
                .Must(file => file.Length > 0).WithMessage("File cannot be empty.");

            RuleFor(v => v.OrganizationId)
                .NotEmpty().WithMessage("Organization ID is required.");
        }

        private bool BeAValidAssetType(string type)
        {
            return Enum.TryParse(typeof(AssetType), type, true, out _);
        }
    }

    public class UpdateMarketingAssetCommandValidator : AbstractValidator<UpdateMarketingAssetCommand>
    {
        public UpdateMarketingAssetCommandValidator()
        {
            RuleFor(v => v.Id)
                .NotEmpty().WithMessage("Id is required.");

            RuleFor(v => v.Name)
                .NotEmpty().WithMessage("Name is required.")
                .MaximumLength(200).WithMessage("Name must not exceed 200 characters.");

            RuleFor(v => v.Description)
                .MaximumLength(1000).WithMessage("Description must not exceed 1000 characters.");

            RuleFor(v => v.OrganizationId)
                .NotEmpty().WithMessage("Organization ID is required.");
        }
    }

    public class DeleteMarketingAssetCommandValidator : AbstractValidator<DeleteMarketingAssetCommand>
    {
        public DeleteMarketingAssetCommandValidator()
        {
            RuleFor(v => v.Id)
                .NotEmpty().WithMessage("Id is required.");

            RuleFor(v => v.OrganizationId)
                .NotEmpty().WithMessage("Organization ID is required.");
        }
    }

    public class SearchMarketingAssetsQueryValidator : AbstractValidator<SearchMarketingAssetsQuery>
    {
        public SearchMarketingAssetsQueryValidator()
        {
            RuleFor(v => v.OrganizationId)
                .NotEmpty().WithMessage("Organization ID is required.");

            RuleFor(v => v.PageNumber)
                .GreaterThan(0).WithMessage("Page number must be greater than 0.");

            RuleFor(v => v.PageSize)
                .GreaterThan(0).WithMessage("Page size must be greater than 0.");
        }
    }
}