using Identity.Domain.Entities;

namespace Identity.Domain.Services;

public interface ITokenService
{
    // Reference Token Methods
    Task<string> GenerateReferenceAccessTokenAsync(User user, IEnumerable<string> scopes, TimeSpan? expiry = null);
    Task<string> GenerateReferenceRefreshTokenAsync(Guid userId, string? sessionId = null);
    Task<bool> ValidateReferenceAccessTokenAsync(string token);
    Task<bool> ValidateReferenceRefreshTokenAsync(string token);
    Task<ReferenceToken?> ValidateReferenceTokenAsync(string token);
    Task<TokenInfo?> GetTokenInfoAsync(string token);

    // Token Revocation
    Task<bool> IsTokenRevokedAsync(string tokenId);
    Task RevokeTokenAsync(string tokenId);
    Task RevokeAllUserTokensAsync(Guid userId);
    Task RevokeSessionTokensAsync(string sessionId);

    // Utility Token Methods (for email confirmation, password reset, etc.)
    string GenerateEmailConfirmationToken(Guid userId);
    bool ValidateEmailConfirmationToken(string token, Guid userId);
    string GeneratePasswordResetToken(Guid userId);
    bool ValidatePasswordResetToken(string token, Guid userId);
}

public class TokenInfo
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string UserType { get; set; } = string.Empty;
    public bool EmailConfirmed { get; set; }
    public bool MfaEnabled { get; set; }
    public string? WalletAddress { get; set; }
    public IEnumerable<string> Scopes { get; set; } = new List<string>();
    public IEnumerable<string> Roles { get; set; } = new List<string>();
    public IEnumerable<string> Permissions { get; set; } = new List<string>();
    public DateTime ExpiresAt { get; set; }
    public string? SessionId { get; set; }
}
