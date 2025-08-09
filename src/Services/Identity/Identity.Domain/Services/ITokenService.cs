using Identity.Domain.Entities;

namespace Identity.Domain.Services;

public interface ITokenService
{
    string GenerateAccessToken(User user, IEnumerable<string> scopes, TimeSpan? expiry = null);
    string GenerateRefreshToken();
    bool ValidateAccessToken(string token);
    bool ValidateRefreshToken(string token);
    Task<bool> IsTokenRevokedAsync(string tokenId);
    Task RevokeTokenAsync(string tokenId);
    Task RevokeAllUserTokensAsync(Guid userId);
    string GenerateEmailConfirmationToken(Guid userId);
    bool ValidateEmailConfirmationToken(string token, Guid userId);
    string GeneratePasswordResetToken(Guid userId);
    bool ValidatePasswordResetToken(string token, Guid userId);
}
