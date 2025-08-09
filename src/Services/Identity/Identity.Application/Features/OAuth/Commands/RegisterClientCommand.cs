using Identity.Application.Common.Interfaces;
using Identity.Application.Common.Models;
using Identity.Application.DTOs;
using Identity.Domain.Entities;
using Identity.Domain.Repositories;
using Identity.Domain.Services;
using Microsoft.Extensions.Logging;

namespace Identity.Application.Features.OAuth.Commands;

public record RegisterClientCommand(
    string ClientId,
    string Name,
    string Description,
    string Type, // "Public", "Confidential", "Machine"
    string[] RedirectUris,
    string[] Scopes,
    string[] GrantTypes,
    bool RequirePkce = true,
    bool RequireClientSecret = true,
    string? LogoUri = null,
    string? ClientUri = null,
    string? TosUri = null,
    string? PolicyUri = null,
    string? IpAddress = null,
    string? UserAgent = null) : ICommand<Result<OAuthClientDto>>;

public class RegisterClientCommandHandler : ICommandHandler<RegisterClientCommand, Result<OAuthClientDto>>
{
    private readonly IOAuthClientRepository _clientRepository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IPasswordService _passwordService;
    private readonly ILogger<RegisterClientCommandHandler> _logger;

    public RegisterClientCommandHandler(
        IOAuthClientRepository clientRepository,
        IAuditLogRepository auditLogRepository,
        IPasswordService passwordService,
        ILogger<RegisterClientCommandHandler> logger)
    {
        _clientRepository = clientRepository;
        _auditLogRepository = auditLogRepository;
        _passwordService = passwordService;
        _logger = logger;
    }

    public async Task<Result<OAuthClientDto>> Handle(RegisterClientCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Check if client ID already exists
            if (await _clientRepository.ExistsAsync(request.ClientId, cancellationToken))
            {
                return Result<OAuthClientDto>.Failure("Client ID already exists");
            }

            // Parse client type
            if (!Enum.TryParse<ClientType>(request.Type, true, out var clientType))
            {
                return Result<OAuthClientDto>.Failure("Invalid client type");
            }

            // Create OAuth client
            var client = new OAuthClient(
                request.ClientId,
                request.Name,
                request.Description,
                clientType,
                request.RequirePkce,
                request.RequireClientSecret);

            // Set URIs
            client.SetUris(request.LogoUri, request.ClientUri, request.TosUri, request.PolicyUri);

            // Add redirect URIs
            foreach (var redirectUri in request.RedirectUris)
            {
                client.AddRedirectUri(redirectUri);
            }

            // Add scopes
            foreach (var scope in request.Scopes)
            {
                client.AddScope(scope);
            }

            // Add grant types (or use defaults)
            if (request.GrantTypes.Any())
            {
                foreach (var grantType in request.GrantTypes)
                {
                    client.AddGrantType(grantType);
                }
            }

            // Generate client secret if required
            string? clientSecret = null;
            if (request.RequireClientSecret)
            {
                clientSecret = _passwordService.GenerateRandomPassword(32);
                var hashedSecret = _passwordService.HashPassword(clientSecret);
                client.SetClientSecret(hashedSecret);
            }

            // Save client
            await _clientRepository.AddAsync(client, cancellationToken);

            // Create audit log
            var auditLog = AuditLog.CreateAdminAction(
                Guid.Empty, // System action
                "CLIENT_REGISTERED",
                "OAUTH_CLIENT",
                request.IpAddress ?? "Unknown",
                request.UserAgent ?? "Unknown",
                $"{{\"clientId\":\"{request.ClientId}\",\"name\":\"{request.Name}\",\"type\":\"{request.Type}\"}}");

            await _auditLogRepository.AddAsync(auditLog, cancellationToken);

            _logger.LogInformation("OAuth client {ClientId} registered successfully", request.ClientId);

            // Map to DTO
            var clientDto = new OAuthClientDto
            {
                Id = client.Id,
                ClientId = client.ClientId,
                ClientSecret = clientSecret, // Only return on creation
                Name = client.Name,
                Description = client.Description,
                Type = client.Type.ToString(),
                IsActive = client.IsActive,
                RequirePkce = client.RequirePkce,
                RequireClientSecret = client.RequireClientSecret,
                RedirectUris = client.RedirectUris.ToArray(),
                Scopes = client.Scopes.ToArray(),
                GrantTypes = client.GrantTypes.ToArray(),
                LogoUri = client.LogoUri,
                ClientUri = client.ClientUri,
                TosUri = client.TosUri,
                PolicyUri = client.PolicyUri,
                CreatedAt = client.CreatedAt
            };

            return Result<OAuthClientDto>.Success(clientDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering OAuth client {ClientId}", request.ClientId);
            return Result<OAuthClientDto>.Failure("An error occurred while registering the OAuth client");
        }
    }
}

public record UpdateClientCommand(
    string ClientId,
    string Name,
    string Description,
    string[] RedirectUris,
    string[] Scopes,
    bool RequirePkce,
    string? LogoUri = null,
    string? ClientUri = null,
    string? TosUri = null,
    string? PolicyUri = null,
    string? IpAddress = null,
    string? UserAgent = null) : ICommand<Result<OAuthClientDto>>;

public class UpdateClientCommandHandler : ICommandHandler<UpdateClientCommand, Result<OAuthClientDto>>
{
    private readonly IOAuthClientRepository _clientRepository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly ILogger<UpdateClientCommandHandler> _logger;

    public UpdateClientCommandHandler(
        IOAuthClientRepository clientRepository,
        IAuditLogRepository auditLogRepository,
        ILogger<UpdateClientCommandHandler> logger)
    {
        _clientRepository = clientRepository;
        _auditLogRepository = auditLogRepository;
        _logger = logger;
    }

    public async Task<Result<OAuthClientDto>> Handle(UpdateClientCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var client = await _clientRepository.GetByClientIdAsync(request.ClientId, cancellationToken);
            if (client == null)
            {
                return Result<OAuthClientDto>.Failure("OAuth client not found");
            }

            // Update basic details
            client.UpdateDetails(request.Name, request.Description);
            client.SetUris(request.LogoUri, request.ClientUri, request.TosUri, request.PolicyUri);

            // Update redirect URIs
            var currentRedirectUris = client.RedirectUris.ToList();
            foreach (var uri in currentRedirectUris)
            {
                client.RemoveRedirectUri(uri);
            }
            foreach (var uri in request.RedirectUris)
            {
                client.AddRedirectUri(uri);
            }

            // Update scopes
            var currentScopes = client.Scopes.ToList();
            foreach (var scope in currentScopes)
            {
                client.RemoveScope(scope);
            }
            foreach (var scope in request.Scopes)
            {
                client.AddScope(scope);
            }

            await _clientRepository.UpdateAsync(client, cancellationToken);

            // Create audit log
            var auditLog = AuditLog.CreateAdminAction(
                Guid.Empty,
                "CLIENT_UPDATED",
                "OAUTH_CLIENT",
                request.IpAddress ?? "Unknown",
                request.UserAgent ?? "Unknown",
                $"{{\"clientId\":\"{request.ClientId}\"}}");

            await _auditLogRepository.AddAsync(auditLog, cancellationToken);

            _logger.LogInformation("OAuth client {ClientId} updated successfully", request.ClientId);

            // Map to DTO (without client secret)
            var clientDto = new OAuthClientDto
            {
                Id = client.Id,
                ClientId = client.ClientId,
                Name = client.Name,
                Description = client.Description,
                Type = client.Type.ToString(),
                IsActive = client.IsActive,
                RequirePkce = client.RequirePkce,
                RequireClientSecret = client.RequireClientSecret,
                RedirectUris = client.RedirectUris.ToArray(),
                Scopes = client.Scopes.ToArray(),
                GrantTypes = client.GrantTypes.ToArray(),
                LogoUri = client.LogoUri,
                ClientUri = client.ClientUri,
                TosUri = client.TosUri,
                PolicyUri = client.PolicyUri,
                CreatedAt = client.CreatedAt,
                UpdatedAt = client.UpdatedAt
            };

            return Result<OAuthClientDto>.Success(clientDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating OAuth client {ClientId}", request.ClientId);
            return Result<OAuthClientDto>.Failure("An error occurred while updating the OAuth client");
        }
    }
}
