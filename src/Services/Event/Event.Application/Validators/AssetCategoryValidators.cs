using FluentValidation;
using Event.Application.Features.AssetCategories.Commands;
using Event.Application.Features.AssetCategories.Queries;
using System;

namespace Event.Application.Validators
{
    public class CreateAssetCategoryCommandValidator : AbstractValidator<CreateAssetCategoryCommand>
    {
        public CreateAssetCategoryCommandValidator()
        {
            RuleFor(v => v.Name)
                .NotEmpty().WithMessage("Name is required.")
                .MaximumLength(100).WithMessage("Name must not exceed 100 characters.");

            RuleFor(v => v.Description)
                .MaximumLength(500).WithMessage("Description must not exceed 500 characters.");

            RuleFor(v => v.OrganizationId)
                .NotEmpty().WithMessage("Organization ID is required.");
        }
    }

    public class UpdateAssetCategoryCommandValidator : AbstractValidator<UpdateAssetCategoryCommand>
    {
        public UpdateAssetCategoryCommandValidator()
        {
            RuleFor(v => v.Id)
                .NotEmpty().WithMessage("Id is required.");

            RuleFor(v => v.Name)
                .NotEmpty().WithMessage("Name is required.")
                .MaximumLength(100).WithMessage("Name must not exceed 100 characters.");

            RuleFor(v => v.Description)
                .MaximumLength(500).WithMessage("Description must not exceed 500 characters.");

            RuleFor(v => v.OrganizationId)
                .NotEmpty().WithMessage("Organization ID is required.");
        }
    }

    public class DeleteAssetCategoryCommandValidator : AbstractValidator<DeleteAssetCategoryCommand>
    {
        public DeleteAssetCategoryCommandValidator()
        {
            RuleFor(v => v.Id)
                .NotEmpty().WithMessage("Id is required.");

            RuleFor(v => v.OrganizationId)
                .NotEmpty().WithMessage("Organization ID is required.");
        }
    }

    public class SearchAssetCategoriesQueryValidator : AbstractValidator<SearchAssetCategoriesQuery>
    {
        public SearchAssetCategoriesQueryValidator()
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