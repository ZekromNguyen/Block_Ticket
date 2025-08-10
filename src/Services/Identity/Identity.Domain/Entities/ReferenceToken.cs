using Shared.Common.Models;

namespace Identity.Domain.Entities;

public class ReferenceToken : BaseAuditableEntity
{
    public string TokenId { get; private set; } = string.Empty;
    public Guid UserId { get; private set; }
    public string TokenType { get; private set; } = string.Empty; // "access_token" or "refresh_token"
    public string? SessionId { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public bool IsRevoked { get; private set; }
    public DateTime? RevokedAt { get; private set; }
    public string? RevokedBy { get; private set; }
    public string? RevokedReason { get; private set; }
    
    // Token Claims (stored as JSON)
    public string? Claims { get; private set; }
    public string? Scopes { get; private set; }
    
    // Metadata
    public string? ClientId { get; private set; }
    public string? IpAddress { get; private set; }
    public string? UserAgent { get; private set; }

    private ReferenceToken() { } // For EF Core

    public ReferenceToken(
        string tokenId,
        Guid userId,
        string tokenType,
        DateTime expiresAt,
        string? sessionId = null,
        string? claims = null,
        string? scopes = null,
        string? clientId = null,
        string? ipAddress = null,
        string? userAgent = null)
    {
        TokenId = tokenId;
        UserId = userId;
        TokenType = tokenType;
        ExpiresAt = expiresAt;
        SessionId = sessionId;
        Claims = claims;
        Scopes = scopes;
        ClientId = clientId;
        IpAddress = ipAddress;
        UserAgent = userAgent;
        IsRevoked = false;
    }

    public void Revoke(string? revokedBy = null, string? reason = null)
    {
        IsRevoked = true;
        RevokedAt = DateTime.UtcNow;
        RevokedBy = revokedBy;
        RevokedReason = reason;
        UpdatedAt = DateTime.UtcNow;
    }

    public bool IsValid()
    {
        return !IsRevoked && ExpiresAt > DateTime.UtcNow;
    }

    public bool IsExpired()
    {
        return ExpiresAt <= DateTime.UtcNow;
    }

    public void UpdateClaims(string claims)
    {
        Claims = claims;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateScopes(string scopes)
    {
        Scopes = scopes;
        UpdatedAt = DateTime.UtcNow;
    }
}

public static class TokenTypes
{
    public const string AccessToken = "access_token";
    public const string RefreshToken = "refresh_token";
}
