using Identity.Application.Common.Models;
using Identity.Application.DTOs;

namespace Identity.Application.Services;

public interface IOAuthService
{
    // Client Management
    Task<Result<OAuthClientDto>> RegisterClientAsync(CreateOAuthClientDto createClientDto, string? ipAddress = null, string? userAgent = null);
    Task<Result<OAuthClientDto>> UpdateClientAsync(string clientId, UpdateOAuthClientDto updateClientDto, string? ipAddress = null, string? userAgent = null);
    Task<Result> DeleteClientAsync(string clientId, string? ipAddress = null, string? userAgent = null);
    Task<Result<OAuthClientDto>> GetClientAsync(string clientId);
    Task<Result<IEnumerable<OAuthClientDto>>> GetClientsAsync(bool activeOnly = false);
    Task<Result<bool>> ValidateClientAsync(string clientId, string? clientSecret = null);

    // Scope Management
    Task<Result<ScopeDto>> CreateScopeAsync(CreateScopeDto createScopeDto, string? ipAddress = null, string? userAgent = null);
    Task<Result<ScopeDto>> UpdateScopeAsync(string scopeName, UpdateScopeDto updateScopeDto, string? ipAddress = null, string? userAgent = null);
    Task<Result> DeleteScopeAsync(string scopeName, string? ipAddress = null, string? userAgent = null);
    Task<Result<IEnumerable<ScopeDto>>> GetScopesAsync(bool discoveryOnly = false);
    Task<Result<IEnumerable<ScopeDto>>> GetDefaultScopesAsync();

    // Authorization Flow
    Task<Result<AuthorizationResponseDto>> AuthorizeAsync(AuthorizationRequestDto authorizationRequest, Guid userId, string? ipAddress = null, string? userAgent = null);
    Task<Result<TokenResponseDto>> ExchangeCodeForTokenAsync(TokenRequestDto tokenRequest, string? ipAddress = null, string? userAgent = null);
    Task<Result<TokenResponseDto>> RefreshTokenAsync(TokenRequestDto tokenRequest, string? ipAddress = null, string? userAgent = null);
    Task<Result<TokenResponseDto>> ClientCredentialsAsync(TokenRequestDto tokenRequest, string? ipAddress = null, string? userAgent = null);

    // Token Management
    Task<Result> RevokeTokenAsync(string token, string? tokenTypeHint = null, string? clientId = null);
    Task<Result<bool>> IntrospectTokenAsync(string token, string? clientId = null);
    Task<Result<UserInfoDto>> GetUserInfoAsync(string accessToken);

    // Discovery
    Task<Result<object>> GetDiscoveryDocumentAsync();
    Task<Result<object>> GetJwksAsync();
}
