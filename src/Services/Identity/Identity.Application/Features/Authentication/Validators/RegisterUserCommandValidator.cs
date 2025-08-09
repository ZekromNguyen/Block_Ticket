using FluentValidation;
using Identity.Application.Features.Authentication.Commands;
using Identity.Domain.Entities;
using System.Text.RegularExpressions;

namespace Identity.Application.Features.Authentication.Validators;

public class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
{
    private static readonly Regex EmailRegex = new(
        @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex PasswordRegex = new(
        @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$",
        RegexOptions.Compiled);

    private static readonly Regex EthereumAddressRegex = new(
        @"^0x[a-fA-F0-9]{40}$",
        RegexOptions.Compiled);

    public RegisterUserCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .MaximumLength(254).WithMessage("Email must not exceed 254 characters")
            .Must(BeValidEmail).WithMessage("Invalid email format");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters long")
            .Must(BeStrongPassword).WithMessage("Password must contain at least one uppercase letter, one lowercase letter, one digit, and one special character");

        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required")
            .MaximumLength(50).WithMessage("First name must not exceed 50 characters")
            .Must(BeValidName).WithMessage("First name contains invalid characters");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required")
            .MaximumLength(50).WithMessage("Last name must not exceed 50 characters")
            .Must(BeValidName).WithMessage("Last name contains invalid characters");

        RuleFor(x => x.UserType)
            .NotEmpty().WithMessage("User type is required")
            .Must(BeValidUserType).WithMessage("Invalid user type");

        RuleFor(x => x.WalletAddress)
            .Must(BeValidWalletAddress).WithMessage("Invalid wallet address format")
            .When(x => !string.IsNullOrEmpty(x.WalletAddress));
    }

    private static bool BeValidEmail(string email)
    {
        return !string.IsNullOrWhiteSpace(email) && EmailRegex.IsMatch(email);
    }

    private static bool BeStrongPassword(string password)
    {
        return !string.IsNullOrWhiteSpace(password) && PasswordRegex.IsMatch(password);
    }

    private static bool BeValidName(string name)
    {
        return !string.IsNullOrWhiteSpace(name) && 
               name.All(c => char.IsLetter(c) || char.IsWhiteSpace(c) || c == '-' || c == '\'');
    }

    private static bool BeValidUserType(string userType)
    {
        return Enum.TryParse<UserType>(userType, true, out _);
    }

    private static bool BeValidWalletAddress(string? walletAddress)
    {
        return string.IsNullOrEmpty(walletAddress) || EthereumAddressRegex.IsMatch(walletAddress);
    }
}
