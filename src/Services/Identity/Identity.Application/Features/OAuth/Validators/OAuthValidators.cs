using FluentValidation;
using Identity.Application.Features.OAuth.Commands;
using System.Text.RegularExpressions;

namespace Identity.Application.Features.OAuth.Validators;

public class RegisterClientCommandValidator : AbstractValidator<RegisterClientCommand>
{
    private static readonly Regex ClientIdRegex = new(@"^[a-zA-Z0-9_-]+$", RegexOptions.Compiled);
    private static readonly string[] ValidGrantTypes = { "authorization_code", "client_credentials", "refresh_token", "password" };
    private static readonly string[] ValidClientTypes = { "Public", "Confidential", "Machine" };

    public RegisterClientCommandValidator()
    {
        RuleFor(x => x.ClientId)
            .NotEmpty().WithMessage("Client ID is required")
            .MaximumLength(100).WithMessage("Client ID must not exceed 100 characters")
            .Must(BeValidClientId).WithMessage("Client ID can only contain letters, numbers, hyphens, and underscores");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Client name is required")
            .MaximumLength(200).WithMessage("Client name must not exceed 200 characters");

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Description must not exceed 1000 characters");

        RuleFor(x => x.Type)
            .NotEmpty().WithMessage("Client type is required")
            .Must(BeValidClientType).WithMessage("Invalid client type");

        RuleFor(x => x.RedirectUris)
            .NotEmpty().WithMessage("At least one redirect URI is required")
            .Must(HaveValidRedirectUris).WithMessage("All redirect URIs must be valid URLs");

        RuleFor(x => x.Scopes)
            .NotEmpty().WithMessage("At least one scope is required")
            .Must(HaveValidScopes).WithMessage("All scopes must be valid");

        RuleFor(x => x.GrantTypes)
            .Must(HaveValidGrantTypes).WithMessage("Invalid grant types specified")
            .When(x => x.GrantTypes.Any());

        RuleFor(x => x.LogoUri)
            .Must(BeValidUri).WithMessage("Logo URI must be a valid URL")
            .When(x => !string.IsNullOrEmpty(x.LogoUri));

        RuleFor(x => x.ClientUri)
            .Must(BeValidUri).WithMessage("Client URI must be a valid URL")
            .When(x => !string.IsNullOrEmpty(x.ClientUri));

        RuleFor(x => x.TosUri)
            .Must(BeValidUri).WithMessage("Terms of Service URI must be a valid URL")
            .When(x => !string.IsNullOrEmpty(x.TosUri));

        RuleFor(x => x.PolicyUri)
            .Must(BeValidUri).WithMessage("Privacy Policy URI must be a valid URL")
            .When(x => !string.IsNullOrEmpty(x.PolicyUri));
    }

    private static bool BeValidClientId(string clientId)
    {
        return ClientIdRegex.IsMatch(clientId);
    }

    private static bool BeValidClientType(string type)
    {
        return ValidClientTypes.Contains(type, StringComparer.OrdinalIgnoreCase);
    }

    private static bool HaveValidRedirectUris(string[] redirectUris)
    {
        return redirectUris.All(uri => Uri.TryCreate(uri, UriKind.Absolute, out _));
    }

    private static bool HaveValidScopes(string[] scopes)
    {
        return scopes.All(scope => !string.IsNullOrWhiteSpace(scope) && scope.Length <= 100);
    }

    private static bool HaveValidGrantTypes(string[] grantTypes)
    {
        return grantTypes.All(gt => ValidGrantTypes.Contains(gt, StringComparer.OrdinalIgnoreCase));
    }

    private static bool BeValidUri(string? uri)
    {
        return string.IsNullOrEmpty(uri) || Uri.TryCreate(uri, UriKind.Absolute, out _);
    }
}

public class UpdateClientCommandValidator : AbstractValidator<UpdateClientCommand>
{
    public UpdateClientCommandValidator()
    {
        RuleFor(x => x.ClientId)
            .NotEmpty().WithMessage("Client ID is required");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Client name is required")
            .MaximumLength(200).WithMessage("Client name must not exceed 200 characters");

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Description must not exceed 1000 characters");

        RuleFor(x => x.RedirectUris)
            .NotEmpty().WithMessage("At least one redirect URI is required")
            .Must(HaveValidRedirectUris).WithMessage("All redirect URIs must be valid URLs");

        RuleFor(x => x.Scopes)
            .NotEmpty().WithMessage("At least one scope is required")
            .Must(HaveValidScopes).WithMessage("All scopes must be valid");

        RuleFor(x => x.LogoUri)
            .Must(BeValidUri).WithMessage("Logo URI must be a valid URL")
            .When(x => !string.IsNullOrEmpty(x.LogoUri));

        RuleFor(x => x.ClientUri)
            .Must(BeValidUri).WithMessage("Client URI must be a valid URL")
            .When(x => !string.IsNullOrEmpty(x.ClientUri));

        RuleFor(x => x.TosUri)
            .Must(BeValidUri).WithMessage("Terms of Service URI must be a valid URL")
            .When(x => !string.IsNullOrEmpty(x.TosUri));

        RuleFor(x => x.PolicyUri)
            .Must(BeValidUri).WithMessage("Privacy Policy URI must be a valid URL")
            .When(x => !string.IsNullOrEmpty(x.PolicyUri));
    }

    private static bool HaveValidRedirectUris(string[] redirectUris)
    {
        return redirectUris.All(uri => Uri.TryCreate(uri, UriKind.Absolute, out _));
    }

    private static bool HaveValidScopes(string[] scopes)
    {
        return scopes.All(scope => !string.IsNullOrWhiteSpace(scope) && scope.Length <= 100);
    }

    private static bool BeValidUri(string? uri)
    {
        return string.IsNullOrEmpty(uri) || Uri.TryCreate(uri, UriKind.Absolute, out _);
    }
}

public class CreateScopeCommandValidator : AbstractValidator<CreateScopeCommand>
{
    private static readonly Regex ScopeNameRegex = new(@"^[a-zA-Z0-9_:-]+$", RegexOptions.Compiled);
    private static readonly string[] ValidScopeTypes = { "Identity", "Resource" };

    public CreateScopeCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Scope name is required")
            .MaximumLength(100).WithMessage("Scope name must not exceed 100 characters")
            .Must(BeValidScopeName).WithMessage("Scope name can only contain letters, numbers, underscores, hyphens, and colons");

        RuleFor(x => x.DisplayName)
            .NotEmpty().WithMessage("Display name is required")
            .MaximumLength(200).WithMessage("Display name must not exceed 200 characters");

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Description must not exceed 1000 characters");

        RuleFor(x => x.Type)
            .Must(BeValidScopeType).WithMessage("Invalid scope type");
    }

    private static bool BeValidScopeName(string name)
    {
        return ScopeNameRegex.IsMatch(name);
    }

    private static bool BeValidScopeType(string type)
    {
        return ValidScopeTypes.Contains(type, StringComparer.OrdinalIgnoreCase);
    }
}

public class UpdateScopeCommandValidator : AbstractValidator<UpdateScopeCommand>
{
    public UpdateScopeCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Scope name is required");

        RuleFor(x => x.DisplayName)
            .NotEmpty().WithMessage("Display name is required")
            .MaximumLength(200).WithMessage("Display name must not exceed 200 characters");

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Description must not exceed 1000 characters");
    }
}

public class DeleteScopeCommandValidator : AbstractValidator<DeleteScopeCommand>
{
    public DeleteScopeCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Scope name is required");
    }
}
