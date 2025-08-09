using FluentValidation;
using Identity.Application.Features.Authentication.Commands;
using System.Text.RegularExpressions;

namespace Identity.Application.Features.Authentication.Validators;

public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    private static readonly Regex EmailRegex = new(
        @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public LoginCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .Must(BeValidEmail).WithMessage("Invalid email format");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required");

        RuleFor(x => x.MfaCode)
            .Length(6).WithMessage("MFA code must be 6 digits")
            .Must(BeNumeric).WithMessage("MFA code must contain only numbers")
            .When(x => !string.IsNullOrEmpty(x.MfaCode));
    }

    private static bool BeValidEmail(string email)
    {
        return !string.IsNullOrWhiteSpace(email) && EmailRegex.IsMatch(email);
    }

    private static bool BeNumeric(string? code)
    {
        return string.IsNullOrEmpty(code) || code.All(char.IsDigit);
    }
}
