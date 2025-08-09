using Shared.Common.Models;

namespace Identity.Domain.Entities;

public class Scope : BaseAuditableEntity
{
    public string Name { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public bool IsRequired { get; private set; }
    public bool IsDefault { get; private set; }
    public bool ShowInDiscoveryDocument { get; private set; }
    public ScopeType Type { get; private set; }

    private Scope() { } // For EF Core

    public Scope(string name, string displayName, string description, ScopeType type = ScopeType.Resource)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Scope name cannot be null or empty");

        Name = name;
        DisplayName = displayName;
        Description = description;
        Type = type;
        IsRequired = false;
        IsDefault = false;
        ShowInDiscoveryDocument = true;
    }

    public void UpdateDetails(string displayName, string description)
    {
        DisplayName = displayName;
        Description = description;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetAsRequired(bool isRequired = true)
    {
        IsRequired = isRequired;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetAsDefault(bool isDefault = true)
    {
        IsDefault = isDefault;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetDiscoveryVisibility(bool showInDiscovery = true)
    {
        ShowInDiscoveryDocument = showInDiscovery;
        UpdatedAt = DateTime.UtcNow;
    }
}

public enum ScopeType
{
    Identity = 0,  // OpenID Connect identity scopes (profile, email, etc.)
    Resource = 1   // API resource scopes
}
