namespace Identity.Application.DTOs;

public record OAuthClientDto
{
    public Guid Id { get; init; }
    public string ClientId { get; init; } = string.Empty;
    public string? ClientSecret { get; init; } // Only returned on creation
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public bool RequirePkce { get; init; }
    public bool RequireClientSecret { get; init; }
    public string[] RedirectUris { get; init; } = Array.Empty<string>();
    public string[] Scopes { get; init; } = Array.Empty<string>();
    public string[] GrantTypes { get; init; } = Array.Empty<string>();
    public string? LogoUri { get; init; }
    public string? ClientUri { get; init; }
    public string? TosUri { get; init; }
    public string? PolicyUri { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public record CreateOAuthClientDto
{
    public string ClientId { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty; // "Public", "Confidential", "Machine"
    public string[] RedirectUris { get; init; } = Array.Empty<string>();
    public string[] Scopes { get; init; } = Array.Empty<string>();
    public string[] GrantTypes { get; init; } = Array.Empty<string>();
    public bool RequirePkce { get; init; } = true;
    public bool RequireClientSecret { get; init; } = true;
    public string? LogoUri { get; init; }
    public string? ClientUri { get; init; }
    public string? TosUri { get; init; }
    public string? PolicyUri { get; init; }
}

public record UpdateOAuthClientDto
{
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string[] RedirectUris { get; init; } = Array.Empty<string>();
    public string[] Scopes { get; init; } = Array.Empty<string>();
    public bool RequirePkce { get; init; }
    public string? LogoUri { get; init; }
    public string? ClientUri { get; init; }
    public string? TosUri { get; init; }
    public string? PolicyUri { get; init; }
}

public record ScopeDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public bool IsRequired { get; init; }
    public bool IsDefault { get; init; }
    public bool ShowInDiscoveryDocument { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public record CreateScopeDto
{
    public string Name { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Type { get; init; } = "Resource"; // "Identity" or "Resource"
    public bool IsRequired { get; init; }
    public bool IsDefault { get; init; }
    public bool ShowInDiscoveryDocument { get; init; } = true;
}

public record UpdateScopeDto
{
    public string DisplayName { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public bool IsRequired { get; init; }
    public bool IsDefault { get; init; }
    public bool ShowInDiscoveryDocument { get; init; }
}

public record AuthorizationRequestDto
{
    public string ClientId { get; init; } = string.Empty;
    public string RedirectUri { get; init; } = string.Empty;
    public string ResponseType { get; init; } = string.Empty;
    public string Scope { get; init; } = string.Empty;
    public string State { get; init; } = string.Empty;
    public string? CodeChallenge { get; init; }
    public string? CodeChallengeMethod { get; init; }
    public string? Nonce { get; init; }
}

public record AuthorizationResponseDto
{
    public string Code { get; init; } = string.Empty;
    public string State { get; init; } = string.Empty;
    public string? Error { get; init; }
    public string? ErrorDescription { get; init; }
}

public record TokenRequestDto
{
    public string GrantType { get; init; } = string.Empty;
    public string? Code { get; init; }
    public string? RedirectUri { get; init; }
    public string? ClientId { get; init; }
    public string? ClientSecret { get; init; }
    public string? CodeVerifier { get; init; }
    public string? RefreshToken { get; init; }
    public string? Scope { get; init; }
    public string? Username { get; init; }
    public string? Password { get; init; }
}

public record TokenResponseDto
{
    public string AccessToken { get; init; } = string.Empty;
    public string TokenType { get; init; } = "Bearer";
    public int ExpiresIn { get; init; }
    public string? RefreshToken { get; init; }
    public string? Scope { get; init; }
    public string? IdToken { get; init; }
    public string? Error { get; init; }
    public string? ErrorDescription { get; init; }
}

public record UserInfoDto
{
    public Guid Id { get; init; }
    public string Email { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string GivenName { get; init; } = string.Empty;
    public string FamilyName { get; init; } = string.Empty;
    public string[] Roles { get; init; } = Array.Empty<string>();
    public string[] Permissions { get; init; } = Array.Empty<string>();
    public string[] Scopes { get; init; } = Array.Empty<string>();
    public DateTime? ExpiresAt { get; init; }
    public bool EmailVerified { get; init; }
    public string UserType { get; init; } = string.Empty;
    public string? WalletAddress { get; init; }
    public bool MfaEnabled { get; init; }
}
