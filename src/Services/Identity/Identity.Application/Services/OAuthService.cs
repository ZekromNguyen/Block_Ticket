using Identity.Application.Common.Models;
using Identity.Application.DTOs;
using Identity.Application.Features.OAuth.Commands;
using Identity.Application.Features.OAuth.Queries;
using Identity.Domain.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Identity.Application.Services;

public class OAuthService : IOAuthService
{
    private readonly IMediator _mediator;
    private readonly ILogger<OAuthService> _logger;
    private readonly ITokenService _tokenService;

    public OAuthService(IMediator mediator, ILogger<OAuthService> logger, ITokenService tokenService)
    {
        _mediator = mediator;
        _logger = logger;
        _tokenService = tokenService;
    }

    // Client Management
    public async Task<Result<OAuthClientDto>> RegisterClientAsync(CreateOAuthClientDto createClientDto, string? ipAddress = null, string? userAgent = null)
    {
        var command = new RegisterClientCommand(
            createClientDto.ClientId,
            createClientDto.Name,
            createClientDto.Description,
            createClientDto.Type,
            createClientDto.RedirectUris,
            createClientDto.Scopes,
            createClientDto.GrantTypes,
            createClientDto.RequirePkce,
            createClientDto.RequireClientSecret,
            createClientDto.LogoUri,
            createClientDto.ClientUri,
            createClientDto.TosUri,
            createClientDto.PolicyUri,
            ipAddress,
            userAgent);

        return await _mediator.Send(command);
    }

    public async Task<Result<OAuthClientDto>> UpdateClientAsync(string clientId, UpdateOAuthClientDto updateClientDto, string? ipAddress = null, string? userAgent = null)
    {
        var command = new UpdateClientCommand(
            clientId,
            updateClientDto.Name,
            updateClientDto.Description,
            updateClientDto.RedirectUris,
            updateClientDto.Scopes,
            updateClientDto.RequirePkce,
            updateClientDto.LogoUri,
            updateClientDto.ClientUri,
            updateClientDto.TosUri,
            updateClientDto.PolicyUri,
            ipAddress,
            userAgent);

        return await _mediator.Send(command);
    }

    public async Task<Result> DeleteClientAsync(string clientId, string? ipAddress = null, string? userAgent = null)
    {
        // TODO: Implement DeleteClientCommand
        _logger.LogInformation("DeleteClientAsync called for client {ClientId}", clientId);
        return Result.Failure("Client deletion not implemented yet");
    }

    public async Task<Result<OAuthClientDto>> GetClientAsync(string clientId)
    {
        var query = new GetOAuthClientByIdQuery(clientId);
        return await _mediator.Send(query);
    }

    public async Task<Result<IEnumerable<OAuthClientDto>>> GetClientsAsync(bool activeOnly = false)
    {
        var query = new GetOAuthClientsQuery(activeOnly);
        return await _mediator.Send(query);
    }

    public async Task<Result<bool>> ValidateClientAsync(string clientId, string? clientSecret = null)
    {
        var query = new ValidateClientQuery(clientId, clientSecret);
        return await _mediator.Send(query);
    }

    // Scope Management
    public async Task<Result<ScopeDto>> CreateScopeAsync(CreateScopeDto createScopeDto, string? ipAddress = null, string? userAgent = null)
    {
        var command = new CreateScopeCommand(
            createScopeDto.Name,
            createScopeDto.DisplayName,
            createScopeDto.Description,
            createScopeDto.Type,
            createScopeDto.IsRequired,
            createScopeDto.IsDefault,
            createScopeDto.ShowInDiscoveryDocument,
            ipAddress,
            userAgent);

        return await _mediator.Send(command);
    }

    public async Task<Result<ScopeDto>> UpdateScopeAsync(string scopeName, UpdateScopeDto updateScopeDto, string? ipAddress = null, string? userAgent = null)
    {
        var command = new UpdateScopeCommand(
            scopeName,
            updateScopeDto.DisplayName,
            updateScopeDto.Description,
            updateScopeDto.IsRequired,
            updateScopeDto.IsDefault,
            updateScopeDto.ShowInDiscoveryDocument,
            ipAddress,
            userAgent);

        return await _mediator.Send(command);
    }

    public async Task<Result> DeleteScopeAsync(string scopeName, string? ipAddress = null, string? userAgent = null)
    {
        var command = new DeleteScopeCommand(scopeName, ipAddress, userAgent);
        return await _mediator.Send(command);
    }

    public async Task<Result<IEnumerable<ScopeDto>>> GetScopesAsync(bool discoveryOnly = false)
    {
        var query = new GetScopesQuery(discoveryOnly);
        return await _mediator.Send(query);
    }

    public async Task<Result<IEnumerable<ScopeDto>>> GetDefaultScopesAsync()
    {
        // TODO: Implement GetDefaultScopesQuery
        _logger.LogInformation("GetDefaultScopesAsync called");
        return Result<IEnumerable<ScopeDto>>.Failure("Default scopes query not implemented yet");
    }

    // Authorization Flow
    public async Task<Result<AuthorizationResponseDto>> AuthorizeAsync(AuthorizationRequestDto authorizationRequest, Guid userId, string? ipAddress = null, string? userAgent = null)
    {
        // TODO: Implement authorization flow
        _logger.LogInformation("AuthorizeAsync called for client {ClientId} and user {UserId}", authorizationRequest.ClientId, userId);
        return Result<AuthorizationResponseDto>.Failure("Authorization flow not implemented yet");
    }

    public async Task<Result<TokenResponseDto>> ExchangeCodeForTokenAsync(TokenRequestDto tokenRequest, string? ipAddress = null, string? userAgent = null)
    {
        // TODO: Implement token exchange
        _logger.LogInformation("ExchangeCodeForTokenAsync called for client {ClientId}", tokenRequest.ClientId);
        return Result<TokenResponseDto>.Failure("Token exchange not implemented yet");
    }

    public async Task<Result<TokenResponseDto>> RefreshTokenAsync(TokenRequestDto tokenRequest, string? ipAddress = null, string? userAgent = null)
    {
        // TODO: Implement refresh token flow
        _logger.LogInformation("RefreshTokenAsync called");
        return Result<TokenResponseDto>.Failure("Refresh token flow not implemented yet");
    }

    public async Task<Result<TokenResponseDto>> ClientCredentialsAsync(TokenRequestDto tokenRequest, string? ipAddress = null, string? userAgent = null)
    {
        // TODO: Implement client credentials flow
        _logger.LogInformation("ClientCredentialsAsync called for client {ClientId}", tokenRequest.ClientId);
        return Result<TokenResponseDto>.Failure("Client credentials flow not implemented yet");
    }

    // Token Management
    public async Task<Result> RevokeTokenAsync(string token, string? tokenTypeHint = null, string? clientId = null)
    {
        // TODO: Implement token revocation
        _logger.LogInformation("RevokeTokenAsync called");
        return Result.Failure("Token revocation not implemented yet");
    }

    public async Task<Result<bool>> IntrospectTokenAsync(string token, string? clientId = null)
    {
        try
        {
            _logger.LogInformation("IntrospectTokenAsync called");

            // Check if it's a reference token
            var referenceToken = await _tokenService.ValidateReferenceTokenAsync(token);
            if (referenceToken != null)
            {
                _logger.LogDebug("Reference token {TokenId} is valid", token);
                return Result<bool>.Success(true);
            }

            _logger.LogDebug("Token {Token} is invalid or expired", token);
            return Result<bool>.Success(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error introspecting token {Token}", token);
            return Result<bool>.Failure("Error validating token");
        }
    }

    public async Task<Result<UserInfoDto>> GetUserInfoAsync(string accessToken)
    {
        try
        {
            _logger.LogInformation("GetUserInfoAsync called");

            // Validate the reference token and get token info
            var referenceToken = await _tokenService.ValidateReferenceTokenAsync(accessToken);
            if (referenceToken == null)
            {
                _logger.LogDebug("Invalid or expired access token {Token}", accessToken);
                return Result<UserInfoDto>.Failure("Invalid or expired access token");
            }

            // Get token info which includes user claims
            var tokenInfo = await _tokenService.GetTokenInfoAsync(accessToken);
            if (tokenInfo == null)
            {
                _logger.LogDebug("Token info not found for token {Token}", accessToken);
                return Result<UserInfoDto>.Failure("Token info not found");
            }

            // Extract user information from claims
            var userInfo = new UserInfoDto
            {
                Id = tokenInfo.UserId,
                Email = tokenInfo.Email,
                Name = $"{tokenInfo.FirstName} {tokenInfo.LastName}".Trim(),
                GivenName = tokenInfo.FirstName,
                FamilyName = tokenInfo.LastName,
                EmailVerified = tokenInfo.EmailConfirmed,
                UserType = tokenInfo.UserType,
                WalletAddress = tokenInfo.WalletAddress,
                MfaEnabled = tokenInfo.MfaEnabled,
                Scopes = tokenInfo.Scopes.ToArray(),
                ExpiresAt = tokenInfo.ExpiresAt,
                Roles = Array.Empty<string>() // TODO: Extract roles from claims if needed
            };

            _logger.LogDebug("User info retrieved for token {Token}", accessToken);
            return Result<UserInfoDto>.Success(userInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user info for token {Token}", accessToken);
            return Result<UserInfoDto>.Failure("Error retrieving user information");
        }
    }

    // Discovery
    public async Task<Result<object>> GetDiscoveryDocumentAsync()
    {
        // TODO: Implement discovery document
        _logger.LogInformation("GetDiscoveryDocumentAsync called");
        return Result<object>.Failure("Discovery document not implemented yet");
    }

    public async Task<Result<object>> GetJwksAsync()
    {
        // TODO: Implement JWKS endpoint
        _logger.LogInformation("GetJwksAsync called");
        return Result<object>.Failure("JWKS endpoint not implemented yet");
    }
}
