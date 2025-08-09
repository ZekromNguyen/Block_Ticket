using FluentValidation;
using Identity.Application.Features.Mfa.Commands;

namespace Identity.Application.Features.Mfa.Validators;

public class SetupTotpCommandValidator : AbstractValidator<SetupTotpCommand>
{
    public SetupTotpCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required");
    }
}

public class VerifyTotpSetupCommandValidator : AbstractValidator<VerifyTotpSetupCommand>
{
    public VerifyTotpSetupCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required");

        RuleFor(x => x.Secret)
            .NotEmpty().WithMessage("Secret is required")
            .MinimumLength(16).WithMessage("Secret must be at least 16 characters");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("TOTP code is required")
            .Length(6).WithMessage("TOTP code must be 6 digits")
            .Must(BeNumeric).WithMessage("TOTP code must contain only numbers");

        RuleFor(x => x.DeviceName)
            .NotEmpty().WithMessage("Device name is required")
            .MaximumLength(100).WithMessage("Device name must not exceed 100 characters");
    }

    private static bool BeNumeric(string code)
    {
        return code.All(char.IsDigit);
    }
}

public class SetupEmailOtpCommandValidator : AbstractValidator<SetupEmailOtpCommand>
{
    public SetupEmailOtpCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required");

        RuleFor(x => x.DeviceName)
            .NotEmpty().WithMessage("Device name is required")
            .MaximumLength(100).WithMessage("Device name must not exceed 100 characters");
    }
}

public class VerifyEmailOtpSetupCommandValidator : AbstractValidator<VerifyEmailOtpSetupCommand>
{
    public VerifyEmailOtpSetupCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("OTP code is required")
            .Length(6).WithMessage("OTP code must be 6 digits")
            .Must(BeNumeric).WithMessage("OTP code must contain only numbers");
    }

    private static bool BeNumeric(string code)
    {
        return code.All(char.IsDigit);
    }
}

public class InitiateWebAuthnSetupCommandValidator : AbstractValidator<InitiateWebAuthnSetupCommand>
{
    public InitiateWebAuthnSetupCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required");
    }
}

public class CompleteWebAuthnSetupCommandValidator : AbstractValidator<CompleteWebAuthnSetupCommand>
{
    public CompleteWebAuthnSetupCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required");

        RuleFor(x => x.DeviceName)
            .NotEmpty().WithMessage("Device name is required")
            .MaximumLength(100).WithMessage("Device name must not exceed 100 characters");

        RuleFor(x => x.Challenge)
            .NotEmpty().WithMessage("Challenge is required");

        RuleFor(x => x.CredentialId)
            .NotEmpty().WithMessage("Credential ID is required");

        RuleFor(x => x.PublicKey)
            .NotEmpty().WithMessage("Public key is required");
    }
}

public class RemoveMfaDeviceCommandValidator : AbstractValidator<RemoveMfaDeviceCommand>
{
    public RemoveMfaDeviceCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required");

        RuleFor(x => x.DeviceId)
            .NotEmpty().WithMessage("Device ID is required");
    }
}

public class GenerateBackupCodesCommandValidator : AbstractValidator<GenerateBackupCodesCommand>
{
    public GenerateBackupCodesCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required");
    }
}

public class DisableMfaCommandValidator : AbstractValidator<DisableMfaCommand>
{
    public DisableMfaCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required");
    }
}
