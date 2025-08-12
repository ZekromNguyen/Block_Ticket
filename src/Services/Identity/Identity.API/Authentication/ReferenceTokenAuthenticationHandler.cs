using System.Security.Claims;
using System.Text.Encodings.Web;
using Identity.Domain.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Identity.API.Authentication;

public class ReferenceTokenAuthenticationSchemeOptions : AuthenticationSchemeOptions
{
    public const string DefaultScheme = "ReferenceToken";
    public string Scheme => DefaultScheme;
}

public class ReferenceTokenAuthenticationHandler : AuthenticationHandler<ReferenceTokenAuthenticationSchemeOptions>
{
    private readonly ITokenService _tokenService;

    public ReferenceTokenAuthenticationHandler(
        IOptionsMonitor<ReferenceTokenAuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ITokenService tokenService)
        : base(options, logger, encoder)
    {
        _tokenService = tokenService;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        try
        {
            // Check if Authorization header exists
            if (!Request.Headers.ContainsKey("Authorization"))
            {
                return AuthenticateResult.NoResult();
            }

            var authHeader = Request.Headers["Authorization"].ToString();
            if (string.IsNullOrEmpty(authHeader))
            {
                return AuthenticateResult.NoResult();
            }

            // Extract the token - support both "Bearer token" and just "token" formats
            string token;
            if (authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                token = authHeader.Substring("Bearer ".Length).Trim();
            }
            else
            {
                // Direct token without Bearer prefix
                token = authHeader.Trim();
            }

            if (string.IsNullOrEmpty(token))
            {
                return AuthenticateResult.Fail("Invalid token format");
            }

            // Validate the reference token
            var isValid = await _tokenService.ValidateReferenceAccessTokenAsync(token);
            if (!isValid)
            {
                Logger.LogDebug("Reference token validation failed for token: {Token}", token);
                return AuthenticateResult.Fail("Invalid or expired token");
            }

            // Get token information
            var tokenInfo = await _tokenService.GetTokenInfoAsync(token);
            if (tokenInfo == null)
            {
                Logger.LogDebug("Could not retrieve token info for token: {Token}", token);
                return AuthenticateResult.Fail("Token information not found");
            }

            // Create claims from token info
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, tokenInfo.UserId.ToString()),
                new(ClaimTypes.Email, tokenInfo.Email),
                new(ClaimTypes.GivenName, tokenInfo.FirstName),
                new(ClaimTypes.Surname, tokenInfo.LastName),
                new("user_type", tokenInfo.UserType),
                new("email_confirmed", tokenInfo.EmailConfirmed.ToString()),
                new("mfa_enabled", tokenInfo.MfaEnabled.ToString()),
                new("jti", Guid.NewGuid().ToString()),
                new("iat", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString())
            };

            // Add wallet address if available
            if (!string.IsNullOrEmpty(tokenInfo.WalletAddress))
            {
                claims.Add(new Claim("wallet_address", tokenInfo.WalletAddress));
            }

            // Add session ID if available
            if (!string.IsNullOrEmpty(tokenInfo.SessionId))
            {
                claims.Add(new Claim("session_id", tokenInfo.SessionId));
            }

            // Add scopes
            foreach (var scope in tokenInfo.Scopes)
            {
                claims.Add(new Claim("scope", scope));
            }

            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);

            Logger.LogDebug("Reference token authentication successful for user: {UserId}", tokenInfo.UserId);
            return AuthenticateResult.Success(ticket);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error during reference token authentication");
            return AuthenticateResult.Fail("Authentication error occurred");
        }
    }
}
