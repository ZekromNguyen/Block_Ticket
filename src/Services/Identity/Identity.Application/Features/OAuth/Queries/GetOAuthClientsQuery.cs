using Identity.Application.Common.Interfaces;
using Identity.Application.Common.Models;
using Identity.Application.DTOs;
using Identity.Domain.Repositories;
using Identity.Domain.Services;
using Microsoft.Extensions.Logging;

namespace Identity.Application.Features.OAuth.Queries;

public record GetOAuthClientsQuery(bool ActiveOnly = false) : IQuery<Result<IEnumerable<OAuthClientDto>>>;

public class GetOAuthClientsQueryHandler : IQueryHandler<GetOAuthClientsQuery, Result<IEnumerable<OAuthClientDto>>>
{
    private readonly IOAuthClientRepository _clientRepository;
    private readonly ILogger<GetOAuthClientsQueryHandler> _logger;

    public GetOAuthClientsQueryHandler(
        IOAuthClientRepository clientRepository,
        ILogger<GetOAuthClientsQueryHandler> logger)
    {
        _clientRepository = clientRepository;
        _logger = logger;
    }

    public async Task<Result<IEnumerable<OAuthClientDto>>> Handle(GetOAuthClientsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var clients = request.ActiveOnly
                ? await _clientRepository.GetActiveClientsAsync(cancellationToken)
                : await _clientRepository.GetAllAsync(cancellationToken);

            var clientDtos = clients.Select(c => new OAuthClientDto
            {
                Id = c.Id,
                ClientId = c.ClientId,
                Name = c.Name,
                Description = c.Description,
                Type = c.Type.ToString(),
                IsActive = c.IsActive,
                RequirePkce = c.RequirePkce,
                RequireClientSecret = c.RequireClientSecret,
                RedirectUris = c.RedirectUris.ToArray(),
                Scopes = c.Scopes.ToArray(),
                GrantTypes = c.GrantTypes.ToArray(),
                LogoUri = c.LogoUri,
                ClientUri = c.ClientUri,
                TosUri = c.TosUri,
                PolicyUri = c.PolicyUri,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt
            });

            return Result<IEnumerable<OAuthClientDto>>.Success(clientDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving OAuth clients");
            return Result<IEnumerable<OAuthClientDto>>.Failure("An error occurred while retrieving OAuth clients");
        }
    }
}

public record GetOAuthClientByIdQuery(string ClientId) : IQuery<Result<OAuthClientDto>>;

public class GetOAuthClientByIdQueryHandler : IQueryHandler<GetOAuthClientByIdQuery, Result<OAuthClientDto>>
{
    private readonly IOAuthClientRepository _clientRepository;
    private readonly ILogger<GetOAuthClientByIdQueryHandler> _logger;

    public GetOAuthClientByIdQueryHandler(
        IOAuthClientRepository clientRepository,
        ILogger<GetOAuthClientByIdQueryHandler> logger)
    {
        _clientRepository = clientRepository;
        _logger = logger;
    }

    public async Task<Result<OAuthClientDto>> Handle(GetOAuthClientByIdQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var client = await _clientRepository.GetByClientIdAsync(request.ClientId, cancellationToken);
            if (client == null)
            {
                return Result<OAuthClientDto>.Failure("OAuth client not found");
            }

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
            _logger.LogError(ex, "Error retrieving OAuth client {ClientId}", request.ClientId);
            return Result<OAuthClientDto>.Failure("An error occurred while retrieving the OAuth client");
        }
    }
}

public record GetScopesQuery(bool DiscoveryOnly = false) : IQuery<Result<IEnumerable<ScopeDto>>>;

public class GetScopesQueryHandler : IQueryHandler<GetScopesQuery, Result<IEnumerable<ScopeDto>>>
{
    private readonly IScopeRepository _scopeRepository;
    private readonly ILogger<GetScopesQueryHandler> _logger;

    public GetScopesQueryHandler(
        IScopeRepository scopeRepository,
        ILogger<GetScopesQueryHandler> logger)
    {
        _scopeRepository = scopeRepository;
        _logger = logger;
    }

    public async Task<Result<IEnumerable<ScopeDto>>> Handle(GetScopesQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var scopes = request.DiscoveryOnly
                ? await _scopeRepository.GetDiscoveryScopesAsync(cancellationToken)
                : await _scopeRepository.GetAllAsync(cancellationToken);

            var scopeDtos = scopes.Select(s => new ScopeDto
            {
                Id = s.Id,
                Name = s.Name,
                DisplayName = s.DisplayName,
                Description = s.Description,
                Type = s.Type.ToString(),
                IsRequired = s.IsRequired,
                IsDefault = s.IsDefault,
                ShowInDiscoveryDocument = s.ShowInDiscoveryDocument,
                CreatedAt = s.CreatedAt,
                UpdatedAt = s.UpdatedAt
            });

            return Result<IEnumerable<ScopeDto>>.Success(scopeDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving scopes");
            return Result<IEnumerable<ScopeDto>>.Failure("An error occurred while retrieving scopes");
        }
    }
}

public record ValidateClientQuery(string ClientId, string? ClientSecret = null) : IQuery<Result<bool>>;

public class ValidateClientQueryHandler : IQueryHandler<ValidateClientQuery, Result<bool>>
{
    private readonly IOAuthClientRepository _clientRepository;
    private readonly IPasswordService _passwordService;
    private readonly ILogger<ValidateClientQueryHandler> _logger;

    public ValidateClientQueryHandler(
        IOAuthClientRepository clientRepository,
        IPasswordService passwordService,
        ILogger<ValidateClientQueryHandler> logger)
    {
        _clientRepository = clientRepository;
        _passwordService = passwordService;
        _logger = logger;
    }

    public async Task<Result<bool>> Handle(ValidateClientQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var client = await _clientRepository.GetByClientIdAsync(request.ClientId, cancellationToken);
            if (client == null || !client.IsActive)
            {
                return Result<bool>.Success(false);
            }

            // If client requires secret, validate it
            if (client.RequireClientSecret)
            {
                if (string.IsNullOrEmpty(request.ClientSecret))
                {
                    return Result<bool>.Success(false);
                }

                if (!_passwordService.VerifyPassword(request.ClientSecret, client.ClientSecret))
                {
                    return Result<bool>.Success(false);
                }
            }

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating client {ClientId}", request.ClientId);
            return Result<bool>.Failure("An error occurred while validating the client");
        }
    }
}
