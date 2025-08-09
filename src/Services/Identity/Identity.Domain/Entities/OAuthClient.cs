using Shared.Common.Models;

namespace Identity.Domain.Entities;

public class OAuthClient : BaseAuditableEntity
{
    private readonly List<string> _redirectUris = new();
    private readonly List<string> _scopes = new();
    private readonly List<string> _grantTypes = new();

    public string ClientId { get; private set; } = string.Empty;
    public string ClientSecret { get; private set; } = string.Empty; // Hashed
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public ClientType Type { get; private set; }
    public bool IsActive { get; private set; }
    public bool RequirePkce { get; private set; }
    public bool RequireClientSecret { get; private set; }
    public TimeSpan AccessTokenLifetime { get; private set; }
    public TimeSpan RefreshTokenLifetime { get; private set; }
    public string? LogoUri { get; private set; }
    public string? ClientUri { get; private set; }
    public string? TosUri { get; private set; }
    public string? PolicyUri { get; private set; }

    public IReadOnlyCollection<string> RedirectUris => _redirectUris.AsReadOnly();
    public IReadOnlyCollection<string> Scopes => _scopes.AsReadOnly();
    public IReadOnlyCollection<string> GrantTypes => _grantTypes.AsReadOnly();

    private OAuthClient() { } // For EF Core

    public OAuthClient(
        string clientId,
        string name,
        string description,
        ClientType type,
        bool requirePkce = true,
        bool requireClientSecret = true)
    {
        ClientId = clientId;
        Name = name;
        Description = description;
        Type = type;
        RequirePkce = requirePkce;
        RequireClientSecret = requireClientSecret;
        IsActive = true;
        AccessTokenLifetime = TimeSpan.FromHours(1);
        RefreshTokenLifetime = TimeSpan.FromDays(30);

        // Set default grant types based on client type
        SetDefaultGrantTypes();
    }

    public void SetClientSecret(string hashedSecret)
    {
        ClientSecret = hashedSecret;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateDetails(string name, string description)
    {
        Name = name;
        Description = description;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetTokenLifetimes(TimeSpan accessTokenLifetime, TimeSpan refreshTokenLifetime)
    {
        AccessTokenLifetime = accessTokenLifetime;
        RefreshTokenLifetime = refreshTokenLifetime;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetUris(string? logoUri, string? clientUri, string? tosUri, string? policyUri)
    {
        LogoUri = logoUri;
        ClientUri = clientUri;
        TosUri = tosUri;
        PolicyUri = policyUri;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddRedirectUri(string uri)
    {
        if (string.IsNullOrWhiteSpace(uri))
            throw new ArgumentException("Redirect URI cannot be null or empty");

        if (!Uri.TryCreate(uri, UriKind.Absolute, out _))
            throw new ArgumentException("Invalid redirect URI format");

        if (!_redirectUris.Contains(uri))
        {
            _redirectUris.Add(uri);
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public void RemoveRedirectUri(string uri)
    {
        if (_redirectUris.Remove(uri))
        {
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public void AddScope(string scope)
    {
        if (string.IsNullOrWhiteSpace(scope))
            throw new ArgumentException("Scope cannot be null or empty");

        if (!_scopes.Contains(scope))
        {
            _scopes.Add(scope);
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public void RemoveScope(string scope)
    {
        if (_scopes.Remove(scope))
        {
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public void AddGrantType(string grantType)
    {
        if (string.IsNullOrWhiteSpace(grantType))
            throw new ArgumentException("Grant type cannot be null or empty");

        if (!_grantTypes.Contains(grantType))
        {
            _grantTypes.Add(grantType);
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public void RemoveGrantType(string grantType)
    {
        if (_grantTypes.Remove(grantType))
        {
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public bool IsRedirectUriAllowed(string uri)
    {
        return _redirectUris.Contains(uri);
    }

    public bool IsScopeAllowed(string scope)
    {
        return _scopes.Contains(scope);
    }

    public bool IsGrantTypeAllowed(string grantType)
    {
        return _grantTypes.Contains(grantType);
    }

    private void SetDefaultGrantTypes()
    {
        _grantTypes.Clear();
        
        switch (Type)
        {
            case ClientType.Public:
                _grantTypes.Add("authorization_code");
                _grantTypes.Add("refresh_token");
                break;
            case ClientType.Confidential:
                _grantTypes.Add("authorization_code");
                _grantTypes.Add("client_credentials");
                _grantTypes.Add("refresh_token");
                break;
            case ClientType.Machine:
                _grantTypes.Add("client_credentials");
                break;
        }
    }
}

public enum ClientType
{
    Public = 0,      // SPA, Mobile apps
    Confidential = 1, // Server-side web apps
    Machine = 2      // Service-to-service
}
