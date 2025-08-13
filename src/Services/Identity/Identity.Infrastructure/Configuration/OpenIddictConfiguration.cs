using Identity.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Identity.Infrastructure.Configuration;

public static class OpenIddictConfiguration
{
    public static IServiceCollection AddOpenIddictConfiguration(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        services.AddOpenIddict()
            .AddCore(options =>
            {
                // Configure the Entity Framework Core stores and models
                options.UseEntityFrameworkCore()
                    .UseDbContext<IdentityDbContext>();

                // Enable Quartz.NET integration (commented out - not available in this version)
                // options.UseQuartz();
            })
            .AddServer(options =>
            {
                // Enable the authorization, logout, token and userinfo endpoints
                options.SetAuthorizationEndpointUris("/connect/authorize")
                       .SetLogoutEndpointUris("/connect/logout")
                       .SetTokenEndpointUris("/connect/token")
                       .SetUserinfoEndpointUris("/connect/userinfo")
                       .SetIntrospectionEndpointUris("/connect/introspect")
                       .SetRevocationEndpointUris("/connect/revoke")
                       .SetConfigurationEndpointUris("/.well-known/openid_configuration")
                       .SetCryptographyEndpointUris("/.well-known/jwks");

                // Enable the authorization code, client credentials, refresh token and password flows
                options.AllowAuthorizationCodeFlow()
                       .AllowClientCredentialsFlow()
                       .AllowRefreshTokenFlow()
                       .AllowPasswordFlow();

                // Enable PKCE for public clients
                options.RequireProofKeyForCodeExchange();

                // Configure token lifetimes
                var tokenSettings = configuration.GetSection("OpenIddict:TokenLifetimes");
                if (int.TryParse(tokenSettings["AccessToken"], out var accessTokenLifetime))
                {
                    options.SetAccessTokenLifetime(TimeSpan.FromMinutes(accessTokenLifetime));
                }
                else
                {
                    options.SetAccessTokenLifetime(TimeSpan.FromMinutes(60)); // Default 1 hour
                }

                if (int.TryParse(tokenSettings["RefreshToken"], out var refreshTokenLifetime))
                {
                    options.SetRefreshTokenLifetime(TimeSpan.FromDays(refreshTokenLifetime));
                }
                else
                {
                    options.SetRefreshTokenLifetime(TimeSpan.FromDays(30)); // Default 30 days
                }

                if (int.TryParse(tokenSettings["IdentityToken"], out var identityTokenLifetime))
                {
                    options.SetIdentityTokenLifetime(TimeSpan.FromMinutes(identityTokenLifetime));
                }
                else
                {
                    options.SetIdentityTokenLifetime(TimeSpan.FromMinutes(15)); // Default 15 minutes
                }

                // Configure signing and encryption credentials
                if (environment.IsDevelopment())
                {
                    // Use development certificates for development
                    options.AddDevelopmentEncryptionCertificate()
                           .AddDevelopmentSigningCertificate();
                }
                else
                {
                    // Use production certificates
                    var certificateSettings = configuration.GetSection("OpenIddict:Certificates");
                    var signingCertPath = certificateSettings["SigningCertificatePath"];
                    var signingCertPassword = certificateSettings["SigningCertificatePassword"];
                    var encryptionCertPath = certificateSettings["EncryptionCertificatePath"];
                    var encryptionCertPassword = certificateSettings["EncryptionCertificatePassword"];

                    if (!string.IsNullOrEmpty(signingCertPath) && File.Exists(signingCertPath))
                    {
                        using var stream = File.OpenRead(signingCertPath);
                        options.AddSigningCertificate(stream, signingCertPassword);
                    }
                    else
                    {
                        // Fallback to development certificates with warning
                        options.AddDevelopmentSigningCertificate();
                    }

                    if (!string.IsNullOrEmpty(encryptionCertPath) && File.Exists(encryptionCertPath))
                    {
                        using var stream = File.OpenRead(encryptionCertPath);
                        options.AddEncryptionCertificate(stream, encryptionCertPassword);
                    }
                    else
                    {
                        // Fallback to development certificates with warning
                        options.AddDevelopmentEncryptionCertificate();
                    }
                }

                // Configure ASP.NET Core host
                var aspNetCoreBuilder = options.UseAspNetCore()
                       .EnableAuthorizationEndpointPassthrough()
                       .EnableLogoutEndpointPassthrough()
                       .EnableTokenEndpointPassthrough()
                       .EnableUserinfoEndpointPassthrough()
                       .EnableStatusCodePagesIntegration();

                // Disable HTTPS requirement in development
                if (environment.IsDevelopment())
                {
                    aspNetCoreBuilder.DisableTransportSecurityRequirement();
                }

                // Configure scopes
                options.RegisterScopes(
                    Scopes.OpenId,
                    Scopes.Profile,
                    Scopes.Email,
                    Scopes.OfflineAccess,
                    "api:read",
                    "api:write",
                    "events:read",
                    "events:write",
                    "tickets:read",
                    "tickets:write",
                    "wallet:read",
                    "wallet:write"
                );

                // Configure claims
                options.RegisterClaims(
                    Claims.Subject,
                    Claims.Name,
                    Claims.GivenName,
                    Claims.FamilyName,
                    Claims.Email,
                    Claims.EmailVerified,
                    Claims.Role,
                    "user_type",
                    "wallet_address",
                    "mfa_enabled"
                );

                // Configure token formats
                options.UseReferenceAccessTokens()
                       .UseReferenceRefreshTokens();

                // Configure security features
                options.DisableAccessTokenEncryption(); // Use reference tokens instead
            })
            .AddValidation(options =>
            {
                // Import the configuration from the local OpenIddict server instance
                options.UseLocalServer();

                // Register the ASP.NET Core host
                options.UseAspNetCore();
            });

        return services;
    }

    public static async Task SeedOpenIddictDataAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var manager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();
        var scopeManager = scope.ServiceProvider.GetRequiredService<IOpenIddictScopeManager>();

        // Seed default scopes
        await SeedScopesAsync(scopeManager);

        // Seed default applications
        await SeedApplicationsAsync(manager);
    }

    private static async Task SeedScopesAsync(IOpenIddictScopeManager manager)
    {
        var scopes = new[]
        {
            new { Name = Scopes.OpenId, DisplayName = "OpenID", Description = "OpenID Connect scope" },
            new { Name = Scopes.Profile, DisplayName = "Profile", Description = "Access to user profile information" },
            new { Name = Scopes.Email, DisplayName = "Email", Description = "Access to user email address" },
            new { Name = Scopes.OfflineAccess, DisplayName = "Offline Access", Description = "Access to refresh tokens" },
            new { Name = "api:read", DisplayName = "API Read", Description = "Read access to API resources" },
            new { Name = "api:write", DisplayName = "API Write", Description = "Write access to API resources" },
            new { Name = "events:read", DisplayName = "Events Read", Description = "Read access to events" },
            new { Name = "events:write", DisplayName = "Events Write", Description = "Write access to events" },
            new { Name = "tickets:read", DisplayName = "Tickets Read", Description = "Read access to tickets" },
            new { Name = "tickets:write", DisplayName = "Tickets Write", Description = "Write access to tickets" },
            new { Name = "wallet:read", DisplayName = "Wallet Read", Description = "Read access to wallet information" },
            new { Name = "wallet:write", DisplayName = "Wallet Write", Description = "Write access to wallet operations" }
        };

        foreach (var scope in scopes)
        {
            if (await manager.FindByNameAsync(scope.Name) == null)
            {
                await manager.CreateAsync(new OpenIddictScopeDescriptor
                {
                    Name = scope.Name,
                    DisplayName = scope.DisplayName,
                    Description = scope.Description
                });
            }
        }
    }

    private static async Task SeedApplicationsAsync(IOpenIddictApplicationManager manager)
    {
        // Seed default web application
        if (await manager.FindByClientIdAsync("blockticket-web") == null)
        {
            await manager.CreateAsync(new OpenIddictApplicationDescriptor
            {
                ClientId = "blockticket-web",
                ClientSecret = "blockticket-web-secret", // Should be configured securely
                ConsentType = ConsentTypes.Explicit,
                DisplayName = "BlockTicket Web Application",
                ClientType = ClientTypes.Confidential,
                PostLogoutRedirectUris =
                {
                    new Uri("https://localhost:3000/signout-callback-oidc"),
                    new Uri("https://blockticket.com/signout-callback-oidc")
                },
                RedirectUris =
                {
                    new Uri("https://localhost:3000/signin-oidc"),
                    new Uri("https://blockticket.com/signin-oidc")
                },
                Permissions =
                {
                    Permissions.Endpoints.Authorization,
                    Permissions.Endpoints.Logout,
                    Permissions.Endpoints.Token,
                    Permissions.GrantTypes.AuthorizationCode,
                    Permissions.GrantTypes.Password,
                    Permissions.GrantTypes.RefreshToken,
                    Permissions.ResponseTypes.Code,
                    Permissions.Scopes.Email,
                    Permissions.Scopes.Profile,
                    Permissions.Scopes.Profile,
                    Permissions.Prefixes.Scope + "api:read",
                    Permissions.Prefixes.Scope + "api:write",
                    Permissions.Prefixes.Scope + "events:read",
                    Permissions.Prefixes.Scope + "events:write",
                    Permissions.Prefixes.Scope + "tickets:read",
                    Permissions.Prefixes.Scope + "tickets:write"
                },
                Requirements =
                {
                    Requirements.Features.ProofKeyForCodeExchange
                }
            });
        }

        // Seed mobile application
        if (await manager.FindByClientIdAsync("blockticket-mobile") == null)
        {
            await manager.CreateAsync(new OpenIddictApplicationDescriptor
            {
                ClientId = "blockticket-mobile",
                ConsentType = ConsentTypes.Explicit,
                DisplayName = "BlockTicket Mobile Application",
                ClientType = ClientTypes.Public,
                RedirectUris =
                {
                    new Uri("com.blockticket.mobile://callback")
                },
                Permissions =
                {
                    Permissions.Endpoints.Authorization,
                    Permissions.Endpoints.Token,
                    Permissions.GrantTypes.AuthorizationCode,
                    Permissions.GrantTypes.RefreshToken,
                    Permissions.ResponseTypes.Code,
                    Permissions.Scopes.Email,
                    Permissions.Scopes.Profile,
                    Permissions.Scopes.Profile,
                    Permissions.Prefixes.Scope + "api:read",
                    Permissions.Prefixes.Scope + "tickets:read",
                    Permissions.Prefixes.Scope + "wallet:read",
                    Permissions.Prefixes.Scope + "wallet:write"
                },
                Requirements =
                {
                    Requirements.Features.ProofKeyForCodeExchange
                }
            });
        }
    }
}
