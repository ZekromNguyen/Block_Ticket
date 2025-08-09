using Identity.Domain.Entities;
using Identity.Domain.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Identity.Infrastructure.Services;

public class TokenService : ITokenService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<TokenService> _logger;
    private readonly JwtSecurityTokenHandler _tokenHandler;

    public TokenService(IConfiguration configuration, ILogger<TokenService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        _tokenHandler = new JwtSecurityTokenHandler();
    }

    public string GenerateAccessToken(User user, IEnumerable<string> scopes, TimeSpan? expiry = null)
    {
        try
        {
            var jwtSettings = _configuration.GetSection("JWT");
            var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey not configured");
            var issuer = jwtSettings["Issuer"] ?? "BlockTicket.Identity";
            var audience = jwtSettings["Audience"] ?? "BlockTicket.Api";
            
            var expirationMinutes = expiry?.TotalMinutes ?? 
                                   double.Parse(jwtSettings["ExpirationMinutes"] ?? "60");

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new(JwtRegisteredClaimNames.Email, user.Email.Value),
                new(JwtRegisteredClaimNames.GivenName, user.FirstName),
                new(JwtRegisteredClaimNames.FamilyName, user.LastName),
                new("user_type", user.UserType.ToString()),
                new("email_confirmed", user.EmailConfirmed.ToString()),
                new("mfa_enabled", user.MfaEnabled.ToString()),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
            };

            // Add wallet address if available
            if (user.WalletAddress != null)
            {
                claims.Add(new Claim("wallet_address", user.WalletAddress.Value));
            }

            // Add scopes
            foreach (var scope in scopes)
            {
                claims.Add(new Claim("scope", scope));
            }

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(expirationMinutes),
                Issuer = issuer,
                Audience = audience,
                SigningCredentials = credentials
            };

            var token = _tokenHandler.CreateToken(tokenDescriptor);
            return _tokenHandler.WriteToken(token);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating access token for user {UserId}", user.Id);
            throw;
        }
    }

    public string GenerateRefreshToken()
    {
        try
        {
            // Generate a cryptographically secure random refresh token
            byte[] randomBytes = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);
            return Convert.ToBase64String(randomBytes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating refresh token");
            throw;
        }
    }

    public bool ValidateAccessToken(string token)
    {
        try
        {
            var jwtSettings = _configuration.GetSection("JWT");
            var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey not configured");
            var issuer = jwtSettings["Issuer"] ?? "BlockTicket.Identity";
            var audience = jwtSettings["Audience"] ?? "BlockTicket.Api";

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = issuer,
                ValidAudience = audience,
                IssuerSigningKey = key,
                ClockSkew = TimeSpan.Zero
            };

            _tokenHandler.ValidateToken(token, validationParameters, out _);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Token validation failed");
            return false;
        }
    }

    public bool ValidateRefreshToken(string token)
    {
        // Refresh tokens are opaque tokens validated against the database
        // This is a placeholder - actual validation would check the database
        return !string.IsNullOrEmpty(token) && token.Length >= 32;
    }

    public async Task<bool> IsTokenRevokedAsync(string tokenId)
    {
        try
        {
            // TODO: Implement token revocation list check
            // This would typically check a Redis cache or database table
            // for revoked token IDs
            
            _logger.LogDebug("Checking if token {TokenId} is revoked", tokenId);
            await Task.Delay(1); // Placeholder
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking token revocation status");
            return true; // Assume revoked on error for security
        }
    }

    public async Task RevokeTokenAsync(string tokenId)
    {
        try
        {
            // TODO: Implement token revocation
            // This would typically add the token ID to a Redis cache or database table
            // with an expiration time matching the token's expiration
            
            _logger.LogInformation("Revoking token {TokenId}", tokenId);
            await Task.Delay(1); // Placeholder
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
            // TODO: Implement user token revocation
            // This would typically add a user revocation timestamp to cache/database
            // All tokens issued before this timestamp would be considered invalid
            
            _logger.LogInformation("Revoking all tokens for user {UserId}", userId);
            await Task.Delay(1); // Placeholder
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking all tokens for user {UserId}", userId);
            throw;
        }
    }

    public string GenerateEmailConfirmationToken(Guid userId)
    {
        try
        {
            // Generate a secure token for email confirmation
            var payload = $"{userId}:{DateTime.UtcNow.AddHours(24):O}"; // Valid for 24 hours
            var payloadBytes = Encoding.UTF8.GetBytes(payload);
            
            // Add some random bytes for additional security
            byte[] randomBytes = new byte[16];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);
            
            var tokenBytes = new byte[payloadBytes.Length + randomBytes.Length];
            Array.Copy(payloadBytes, 0, tokenBytes, 0, payloadBytes.Length);
            Array.Copy(randomBytes, 0, tokenBytes, payloadBytes.Length, randomBytes.Length);
            
            return Convert.ToBase64String(tokenBytes);
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
            var tokenBytes = Convert.FromBase64String(token);
            var payloadBytes = new byte[tokenBytes.Length - 16]; // Remove random bytes
            Array.Copy(tokenBytes, 0, payloadBytes, 0, payloadBytes.Length);
            
            var payload = Encoding.UTF8.GetString(payloadBytes);
            var parts = payload.Split(':');
            
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
            var payload = $"{userId}:{DateTime.UtcNow.AddHours(1):O}";
            var payloadBytes = Encoding.UTF8.GetBytes(payload);
            
            byte[] randomBytes = new byte[16];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);
            
            var tokenBytes = new byte[payloadBytes.Length + randomBytes.Length];
            Array.Copy(payloadBytes, 0, tokenBytes, 0, payloadBytes.Length);
            Array.Copy(randomBytes, 0, tokenBytes, payloadBytes.Length, randomBytes.Length);
            
            return Convert.ToBase64String(tokenBytes);
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
            var tokenBytes = Convert.FromBase64String(token);
            var payloadBytes = new byte[tokenBytes.Length - 16];
            Array.Copy(tokenBytes, 0, payloadBytes, 0, payloadBytes.Length);
            
            var payload = Encoding.UTF8.GetString(payloadBytes);
            var parts = payload.Split(':');
            
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
