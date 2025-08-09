using Identity.Application.Services;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using System.Collections.Immutable;
using System.Security.Claims;
using static OpenIddict.Abstractions.OpenIddictConstants;
using AuthenticationService = Identity.Application.Services.IAuthenticationService;

namespace Identity.API.Controllers;

/// <summary>
/// OpenIddict OAuth 2.0 / OpenID Connect endpoints
/// </summary>
[ApiController]
[Route("connect")]
public class ConnectController : ControllerBase
{
    private readonly AuthenticationService _authenticationService;
    private readonly IOpenIddictApplicationManager _applicationManager;
    private readonly ILogger<ConnectController> _logger;

    public ConnectController(
        AuthenticationService authenticationService,
        IOpenIddictApplicationManager applicationManager,
        ILogger<ConnectController> logger)
    {
        _authenticationService = authenticationService;
        _applicationManager = applicationManager;
        _logger = logger;
    }

    /// <summary>
    /// OAuth 2.0 Authorization Endpoint
    /// </summary>
    [HttpGet("authorize")]
    [HttpPost("authorize")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Authorize()
    {
        var request = HttpContext.GetOpenIddictServerRequest() ??
            throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");

        // Retrieve the user principal stored in the authentication cookie
        var result = await HttpContext.AuthenticateAsync();
        if (!result.Succeeded)
        {
            // If the client application requested promptless authentication,
            // return an error indicating that the user is not logged in
            // TODO: Fix prompt checking in OpenIddict 5.x
            // if (request.HasPrompt(Prompts.None))
            // {
            //     return Forbid(
            //         authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
            //         properties: new AuthenticationProperties(new Dictionary<string, string?>
            //         {
            //             [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.LoginRequired,
            //             [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "The user is not logged in."
            //         }));
            // }

            // Redirect the user to the login page
            return Challenge();
        }

        // Create a new claims principal
        var claims = new List<Claim>
        {
            new(Claims.Subject, result.Principal!.GetClaim(Claims.Subject)!),
            new(Claims.Email, result.Principal.GetClaim(Claims.Email)!),
            new(Claims.Name, result.Principal.GetClaim(Claims.Name)!)
        };

        // Add role claims
        var roles = result.Principal.GetClaims(Claims.Role);
        claims.AddRange(roles.Select(role => new Claim(Claims.Role, role)));

        var claimsIdentity = new ClaimsIdentity(claims, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

        // Set the list of scopes granted to the client application
        claimsPrincipal.SetScopes(request.GetScopes());
        claimsPrincipal.SetResources(await GetResourcesAsync(request.GetScopes()));

        // Allow all claims to be added in the access tokens
        claimsPrincipal.SetDestinations(GetDestinations);

        return SignIn(claimsPrincipal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    /// <summary>
    /// OAuth 2.0 Token Endpoint
    /// </summary>
    [HttpPost("token")]
    [IgnoreAntiforgeryToken]
    [Produces("application/json")]
    public async Task<IActionResult> Exchange()
    {
        var request = HttpContext.GetOpenIddictServerRequest() ??
            throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");

        if (request.IsPasswordGrantType())
        {
            return await HandlePasswordFlow(request);
        }

        if (request.IsClientCredentialsGrantType())
        {
            return await HandleClientCredentialsFlow(request);
        }

        if (request.IsAuthorizationCodeGrantType() || request.IsRefreshTokenGrantType())
        {
            return await HandleAuthorizationCodeOrRefreshTokenFlow(request);
        }

        throw new InvalidOperationException("The specified grant type is not supported.");
    }

    /// <summary>
    /// OAuth 2.0 UserInfo Endpoint
    /// </summary>
    [Authorize(AuthenticationSchemes = OpenIddictServerAspNetCoreDefaults.AuthenticationScheme)]
    [HttpGet("userinfo")]
    [HttpPost("userinfo")]
    [Produces("application/json")]
    public async Task<IActionResult> Userinfo()
    {
        var claimsPrincipal = (await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme)).Principal;

        return Ok(new
        {
            sub = claimsPrincipal!.GetClaim(Claims.Subject),
            email = claimsPrincipal.GetClaim(Claims.Email),
            email_verified = true,
            name = claimsPrincipal.GetClaim(Claims.Name),
            given_name = claimsPrincipal.GetClaim(Claims.GivenName),
            family_name = claimsPrincipal.GetClaim(Claims.FamilyName),
            roles = claimsPrincipal.GetClaims(Claims.Role)
        });
    }

    /// <summary>
    /// OAuth 2.0 Logout Endpoint
    /// </summary>
    [HttpGet("logout")]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        // Ask OpenIddict to return a logout response using the appropriate response_mode
        return SignOut(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    private async Task<IActionResult> HandlePasswordFlow(OpenIddictRequest request)
    {
        var loginDto = new Identity.Application.DTOs.LoginDto
        {
            Email = request.Username!,
            Password = request.Password!
        };

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = HttpContext.Request.Headers.UserAgent.ToString();

        var result = await _authenticationService.LoginAsync(loginDto, ipAddress, userAgent);

        if (!result.IsSuccess)
        {
            var properties = new AuthenticationProperties(new Dictionary<string, string?>
            {
                [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidGrant,
                [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = result.Error
            });

            return Forbid(properties, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        var loginResult = result.Value!;

        if (loginResult.RequiresMfa)
        {
            var properties = new AuthenticationProperties(new Dictionary<string, string?>
            {
                [OpenIddictServerAspNetCoreConstants.Properties.Error] = "mfa_required",
                [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "Multi-factor authentication is required."
            });

            return Forbid(properties, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        var user = loginResult.User;
        var claims = new List<Claim>
        {
            new(Claims.Subject, user.Id.ToString()),
            new(Claims.Email, user.Email),
            new(Claims.Name, $"{user.FirstName} {user.LastName}".Trim()),
            new(Claims.GivenName, user.FirstName),
            new(Claims.FamilyName, user.LastName)
        };

        // Add role claims (this would come from user roles)
        claims.Add(new Claim(Claims.Role, "fan")); // Default role

        var claimsIdentity = new ClaimsIdentity(claims, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

        claimsPrincipal.SetScopes(request.GetScopes());
        claimsPrincipal.SetResources(await GetResourcesAsync(request.GetScopes()));
        claimsPrincipal.SetDestinations(GetDestinations);

        return SignIn(claimsPrincipal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    private async Task<IActionResult> HandleClientCredentialsFlow(OpenIddictRequest request)
    {
        // Note: the client credentials are automatically validated by OpenIddict:
        // if client_id or client_secret are invalid, this action won't be invoked.

        var application = await _applicationManager.FindByClientIdAsync(request.ClientId!);
        if (application == null)
        {
            throw new InvalidOperationException("The application details cannot be found in the database.");
        }

        // Create a new ClaimsIdentity containing the claims that
        // will be used to create an id_token, a token or a code.
        var identity = new ClaimsIdentity(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

        // Use the client_id as the subject identifier
        identity.AddClaim(Claims.Subject, await _applicationManager.GetClientIdAsync(application) ?? throw new InvalidOperationException());
        identity.AddClaim(Claims.Name, await _applicationManager.GetDisplayNameAsync(application) ?? throw new InvalidOperationException());

        var principal = new ClaimsPrincipal(identity);

        principal.SetScopes(request.GetScopes());
        principal.SetResources(await GetResourcesAsync(request.GetScopes()));
        principal.SetDestinations(GetDestinations);

        return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    private async Task<IActionResult> HandleAuthorizationCodeOrRefreshTokenFlow(OpenIddictRequest request)
    {
        // Retrieve the claims principal stored in the authorization code/refresh token
        var result = await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

        // Retrieve the user profile corresponding to the authorization code/refresh token
        // Note: if you want to automatically invalidate the authorization code/refresh token
        // when the user password/roles change, use the following line instead:
        // var user = _signInManager.ValidateSecurityStampAsync(result.Principal);
        var user = result.Principal;

        if (user == null)
        {
            return Forbid(
                authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                properties: new AuthenticationProperties(new Dictionary<string, string?>
                {
                    [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidGrant,
                    [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "The token is no longer valid."
                }));
        }

        // Ensure the user is still allowed to sign in
        // if (!await _signInManager.CanSignInAsync(user))
        // {
        //     return Forbid(...);
        // }

        user.SetDestinations(GetDestinations);

        return SignIn(user, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    private async Task<IEnumerable<string>> GetResourcesAsync(IEnumerable<string> scopes)
    {
        // Note: the sample only uses the resource server URLs (defined in the configuration)
        // but in a real world application, you'd probably want to handle the scopes in a more
        // flexible way and resolve the actual resource server URLs from the scopes.
        return new[] { "blockticket-api" };
    }

    private static IEnumerable<string> GetDestinations(Claim claim)
    {
        // Note: by default, claims are NOT automatically included in the access and identity tokens.
        // To allow OpenIddict to serialize them, you must attach them a destination, that specifies
        // whether they should be included in access tokens, in identity tokens or in both.

        switch (claim.Type)
        {
            case Claims.Name:
                yield return Destinations.AccessToken;

                if (claim.Subject!.HasScope(Scopes.Profile))
                    yield return Destinations.IdentityToken;

                yield break;

            case Claims.Email:
                yield return Destinations.AccessToken;

                if (claim.Subject!.HasScope(Scopes.Email))
                    yield return Destinations.IdentityToken;

                yield break;

            case Claims.Role:
                yield return Destinations.AccessToken;

                if (claim.Subject!.HasScope(Scopes.Profile))
                    yield return Destinations.IdentityToken;

                yield break;

            // Never include the security stamp in the access and identity tokens, as it's a secret value.
            case "AspNet.Identity.SecurityStamp": yield break;

            default:
                yield return Destinations.AccessToken;
                yield break;
        }
    }
}
