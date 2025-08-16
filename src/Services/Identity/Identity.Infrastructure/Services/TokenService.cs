using Identity.Domain.Entities;
using Identity.Domain.Repositories;
using Identity.Domain.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Identity.Infrastructure.Services;

public class TokenService : ITokenService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<TokenService> _logger;
    private readonly JwtSecurityTokenHandler _tokenHandler;
    private readonly IReferenceTokenRepository _referenceTokenRepository;

    public TokenService(
        IConfiguration configuration,
        ILogger<TokenService> logger,
        IReferenceTokenRepository referenceTokenRepository)
    {
        _configuration = configuration;
        _logger = logger;
        _tokenHandler = new JwtSecurityTokenHandler();
        _referenceTokenRepository = referenceTokenRepository;
    }

    public async Task<string> GenerateReferenceAccessTokenAsync(User user, IEnumerable<string> scopes, TimeSpan? expiry = null)
    {
        try
        {
            var jwtSettings = _configuration.GetSection("JWT");
            var expirationMinutes = expiry?.TotalMinutes ??
                                   double.Parse(jwtSettings["ExpirationMinutes"] ?? "60");

            var tokenId = GenerateSecureToken();
            var expiresAt = DateTime.UtcNow.AddMinutes(expirationMinutes);

            // Get user roles and permissions
            var activeRoles = user.GetActiveRoles().ToList();
            var roleNames = activeRoles.Select(r => r.Name).ToArray();
            var permissions = activeRoles
                .SelectMany(r => r.GetActivePermissions().Where(p => p.IsActive))
                .Select(p => $"{p.Resource}:{p.Action}")
                .Distinct()
                .ToArray();

            // Create claims dictionary
            var claims = new Dictionary<string, object>
            {
                ["sub"] = user.Id.ToString(),
                ["email"] = user.Email.Value,
                ["given_name"] = user.FirstName,
                ["family_name"] = user.LastName,
                ["user_type"] = user.UserType.ToString(),
                ["email_confirmed"] = user.EmailConfirmed,
                ["mfa_enabled"] = user.MfaEnabled,
                ["jti"] = Guid.NewGuid().ToString(),
                ["iat"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                ["scope"] = scopes.ToArray(),
                ["roles"] = roleNames,
                ["permissions"] = permissions
            };

            // Add wallet address if available
            if (user.WalletAddress != null)
            {
                claims["wallet_address"] = user.WalletAddress.Value;
            }

            var claimsJson = JsonSerializer.Serialize(claims);
            var scopesString = string.Join(" ", scopes);

            var referenceToken = new ReferenceToken(
                tokenId: tokenId,
                userId: user.Id,
                tokenType: TokenTypes.AccessToken,
                expiresAt: expiresAt,
                claims: claimsJson,
                scopes: scopesString
            );

            await _referenceTokenRepository.AddAsync(referenceToken);

            _logger.LogDebug("Reference access token generated for user {UserId} with ID {TokenId} with roles: {Roles}", user.Id, tokenId, string.Join(", ", roleNames));
            return tokenId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating reference access token for user {UserId}", user.Id);
            throw;
        }
    }

    public async Task<string> GenerateReferenceRefreshTokenAsync(Guid userId, string? sessionId = null)
    {
        try
        {
            var jwtSettings = _configuration.GetSection("JWT");
            var refreshExpirationDays = double.Parse(jwtSettings["RefreshExpirationDays"] ?? "30");

            var tokenId = GenerateSecureToken();
            var expiresAt = DateTime.UtcNow.AddDays(refreshExpirationDays);

            var referenceToken = new ReferenceToken(
                tokenId: tokenId,
                userId: userId,
                tokenType: TokenTypes.RefreshToken,
                expiresAt: expiresAt,
                sessionId: sessionId
            );

            await _referenceTokenRepository.AddAsync(referenceToken);

            _logger.LogDebug("Reference refresh token generated for user {UserId} with ID {TokenId}", userId, tokenId);
            return tokenId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating reference refresh token for user {UserId}", userId);
            throw;
        }
    }

    public async Task<bool> ValidateReferenceAccessTokenAsync(string token)
    {
        try
        {
            var referenceToken = await _referenceTokenRepository.GetValidTokenAsync(token);
            if (referenceToken == null || referenceToken.TokenType != TokenTypes.AccessToken)
            {
                _logger.LogDebug("Reference access token {TokenId} not found or invalid", token);
                return false;
            }

            _logger.LogDebug("Reference access token {TokenId} validated successfully", token);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating reference access token {TokenId}", token);
            return false;
        }
    }

    public async Task<bool> ValidateReferenceRefreshTokenAsync(string token)
    {
        try
        {
            var referenceToken = await _referenceTokenRepository.GetValidTokenAsync(token);
            if (referenceToken == null || referenceToken.TokenType != TokenTypes.RefreshToken)
            {
                _logger.LogDebug("Reference refresh token {TokenId} not found or invalid", token);
                return false;
            }

            _logger.LogDebug("Reference refresh token {TokenId} validated successfully", token);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating reference refresh token {TokenId}", token);
            return false;
        }
    }

    public async Task<ReferenceToken?> ValidateReferenceTokenAsync(string token)
    {
        try
        {
            var referenceToken = await _referenceTokenRepository.GetValidTokenAsync(token);
            if (referenceToken == null)
            {
                _logger.LogDebug("Reference token {TokenId} not found or invalid", token);
                return null;
            }

            _logger.LogDebug("Reference token {TokenId} validated successfully", token);
            return referenceToken;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating reference token {TokenId}", token);
            return null;
        }
    }

    public async Task<TokenInfo?> GetTokenInfoAsync(string token)
    {
        try
        {
            var referenceToken = await _referenceTokenRepository.GetValidTokenAsync(token);
            if (referenceToken == null || referenceToken.TokenType != TokenTypes.AccessToken)
            {
                return null;
            }

            if (string.IsNullOrEmpty(referenceToken.Claims))
            {
                return null;
            }

            var claims = JsonSerializer.Deserialize<Dictionary<string, object>>(referenceToken.Claims);
            if (claims == null)
            {
                return null;
            }

            var scopes = !string.IsNullOrEmpty(referenceToken.Scopes)
                ? referenceToken.Scopes.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                : Array.Empty<string>();

            // Extract roles and permissions from claims
            var roles = ExtractStringArrayFromClaims(claims, "roles");
            var permissions = ExtractStringArrayFromClaims(claims, "permissions");

            return new TokenInfo
            {
                UserId = referenceToken.UserId,
                Email = claims.GetValueOrDefault("email")?.ToString() ?? string.Empty,
                FirstName = claims.GetValueOrDefault("given_name")?.ToString() ?? string.Empty,
                LastName = claims.GetValueOrDefault("family_name")?.ToString() ?? string.Empty,
                UserType = claims.GetValueOrDefault("user_type")?.ToString() ?? string.Empty,
                EmailConfirmed = bool.Parse(claims.GetValueOrDefault("email_confirmed")?.ToString() ?? "false"),
                MfaEnabled = bool.Parse(claims.GetValueOrDefault("mfa_enabled")?.ToString() ?? "false"),
                WalletAddress = claims.GetValueOrDefault("wallet_address")?.ToString(),
                Scopes = scopes,
                Roles = roles,
                Permissions = permissions,
                ExpiresAt = referenceToken.ExpiresAt,
                SessionId = referenceToken.SessionId
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting token info for {TokenId}", token);
            return null;
        }
    }

    private static string[] ExtractStringArrayFromClaims(Dictionary<string, object> claims, string claimName)
    {
        try
        {
            var claimValue = claims.GetValueOrDefault(claimName);
            if (claimValue == null)
                return Array.Empty<string>();

            // Handle JsonElement array (from deserialization)
            if (claimValue is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.Array)
            {
                return jsonElement.EnumerateArray()
                    .Select(e => e.GetString() ?? string.Empty)
                    .Where(s => !string.IsNullOrEmpty(s))
                    .ToArray();
            }

            // Handle string array directly
            if (claimValue is string[] stringArray)
            {
                return stringArray;
            }

            // Handle single string value
            if (claimValue is string singleValue)
            {
                return new[] { singleValue };
            }

            return Array.Empty<string>();
        }
        catch
        {
            return Array.Empty<string>();
        }
    }

    public async Task<bool> IsTokenRevokedAsync(string tokenId)
    {
        try
        {
            var referenceToken = await _referenceTokenRepository.GetByTokenIdAsync(tokenId);
            if (referenceToken == null)
            {
                _logger.LogDebug("Token {TokenId} not found", tokenId);
                return true; // Consider non-existent tokens as revoked
            }

            var isRevoked = referenceToken.IsRevoked || referenceToken.IsExpired();
            _logger.LogDebug("Token {TokenId} revocation status: {IsRevoked}", tokenId, isRevoked);
            return isRevoked;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking token revocation status for {TokenId}", tokenId);
            return true; // Assume revoked on error for security
        }
    }

    public async Task RevokeTokenAsync(string tokenId)
    {
        try
        {
            await _referenceTokenRepository.RevokeTokenAsync(tokenId, reason: "Manual revocation");
            _logger.LogInformation("Token {TokenId} revoked successfully", tokenId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking token {TokenId}", tokenId);
            throw;
        }
    }

    public async Task RevokeAllUserTokensAsync(Guid userId)
    {
        try
        {
            await _referenceTokenRepository.RevokeUserTokensAsync(userId, reason: "All user tokens revoked");
            _logger.LogInformation("All tokens revoked for user {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking all tokens for user {UserId}", userId);
            throw;
        }
    }

    public async Task RevokeSessionTokensAsync(string sessionId)
    {
        try
        {
            await _referenceTokenRepository.RevokeSessionTokensAsync(sessionId, reason: "Session tokens revoked");
            _logger.LogInformation("All tokens revoked for session {SessionId}", sessionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking session tokens for {SessionId}", sessionId);
            throw;
        }
    }

    private static string GenerateSecureToken()
    {
        // Generate a cryptographically secure random token
        byte[] randomBytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes).Replace("+", "-").Replace("/", "_").Replace("=", "");
    }

    public string GenerateEmailConfirmationToken(Guid userId)
    {
        try
        {
            // Generate a secure token for email confirmation
            var payload = $"{userId}|{DateTime.UtcNow.AddHours(24):O}"; // Valid for 24 hours, using | separator
            var payloadBytes = Encoding.UTF8.GetBytes(payload);

            // Add some random bytes for additional security
            byte[] randomBytes = new byte[16];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);

            var tokenBytes = new byte[payloadBytes.Length + randomBytes.Length];
            Array.Copy(payloadBytes, 0, tokenBytes, 0, payloadBytes.Length);
            Array.Copy(randomBytes, 0, tokenBytes, payloadBytes.Length, randomBytes.Length);

            // Use URL-safe Base64 encoding
            return Convert.ToBase64String(tokenBytes)
                .Replace('+', '-')
                .Replace('/', '_')
                .TrimEnd('=');
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating email confirmation token for user {UserId}", userId);
            throw;
        }
    }

    public bool ValidateEmailConfirmationToken(string token, Guid userId)
    {
        try
        {
            // Convert URL-safe Base64 back to regular Base64
            var base64Token = token.Replace('-', '+').Replace('_', '/');
            // Add padding if needed
            switch (base64Token.Length % 4)
            {
                case 2: base64Token += "=="; break;
                case 3: base64Token += "="; break;
            }

            var tokenBytes = Convert.FromBase64String(base64Token);
            var payloadBytes = new byte[tokenBytes.Length - 16]; // Remove random bytes
            Array.Copy(tokenBytes, 0, payloadBytes, 0, payloadBytes.Length);

            var payload = Encoding.UTF8.GetString(payloadBytes);
            var parts = payload.Split('|'); // Use | separator instead of :

            if (parts.Length != 2)
                return false;

            if (!Guid.TryParse(parts[0], out var tokenUserId) || tokenUserId != userId)
                return false;

            if (!DateTime.TryParse(parts[1], out var expiryTime) || expiryTime < DateTime.UtcNow)
                return false;

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Email confirmation token validation failed");
            return false;
        }
    }

    public string GeneratePasswordResetToken(Guid userId)
    {
        try
        {
            // Generate a secure token for password reset (valid for 1 hour)
            var payload = $"{userId}|{DateTime.UtcNow.AddHours(1):O}"; // Using | separator
            var payloadBytes = Encoding.UTF8.GetBytes(payload);

            byte[] randomBytes = new byte[16];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);

            var tokenBytes = new byte[payloadBytes.Length + randomBytes.Length];
            Array.Copy(payloadBytes, 0, tokenBytes, 0, payloadBytes.Length);
            Array.Copy(randomBytes, 0, tokenBytes, payloadBytes.Length, randomBytes.Length);

            // Use URL-safe Base64 encoding
            return Convert.ToBase64String(tokenBytes)
                .Replace('+', '-')
                .Replace('/', '_')
                .TrimEnd('=');
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating password reset token for user {UserId}", userId);
            throw;
        }
    }

    public bool ValidatePasswordResetToken(string token, Guid userId)
    {
        try
        {
            // Convert URL-safe Base64 back to regular Base64
            var base64Token = token.Replace('-', '+').Replace('_', '/');
            // Add padding if needed
            switch (base64Token.Length % 4)
            {
                case 2: base64Token += "=="; break;
                case 3: base64Token += "="; break;
            }

            var tokenBytes = Convert.FromBase64String(base64Token);
            var payloadBytes = new byte[tokenBytes.Length - 16];
            Array.Copy(tokenBytes, 0, payloadBytes, 0, payloadBytes.Length);

            var payload = Encoding.UTF8.GetString(payloadBytes);
            var parts = payload.Split('|'); // Use | separator instead of :

            if (parts.Length != 2)
                return false;

            if (!Guid.TryParse(parts[0], out var tokenUserId) || tokenUserId != userId)
                return false;

            if (!DateTime.TryParse(parts[1], out var expiryTime) || expiryTime < DateTime.UtcNow)
                return false;

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Password reset token validation failed");
            return false;
        }
    }
}
