using Identity.Application.Common.Interfaces;
using Identity.Application.Common.Models;
using Identity.Application.DTOs;
using Identity.Domain.Entities;
using Identity.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Identity.Application.Features.OAuth.Commands;

public record CreateScopeCommand(
    string Name,
    string DisplayName,
    string Description,
    string Type = "Resource",
    bool IsRequired = false,
    bool IsDefault = false,
    bool ShowInDiscoveryDocument = true,
    string? IpAddress = null,
    string? UserAgent = null) : ICommand<Result<ScopeDto>>;

public class CreateScopeCommandHandler : ICommandHandler<CreateScopeCommand, Result<ScopeDto>>
{
    private readonly IScopeRepository _scopeRepository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly ILogger<CreateScopeCommandHandler> _logger;

    public CreateScopeCommandHandler(
        IScopeRepository scopeRepository,
        IAuditLogRepository auditLogRepository,
        ILogger<CreateScopeCommandHandler> logger)
    {
        _scopeRepository = scopeRepository;
        _auditLogRepository = auditLogRepository;
        _logger = logger;
    }

    public async Task<Result<ScopeDto>> Handle(CreateScopeCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Check if scope already exists
            if (await _scopeRepository.ExistsAsync(request.Name, cancellationToken))
            {
                return Result<ScopeDto>.Failure("Scope already exists");
            }

            // Parse scope type
            if (!Enum.TryParse<ScopeType>(request.Type, true, out var scopeType))
            {
                return Result<ScopeDto>.Failure("Invalid scope type");
            }

            // Create scope
            var scope = new Scope(request.Name, request.DisplayName, request.Description, scopeType);
            
            scope.SetAsRequired(request.IsRequired);
            scope.SetAsDefault(request.IsDefault);
            scope.SetDiscoveryVisibility(request.ShowInDiscoveryDocument);

            await _scopeRepository.AddAsync(scope, cancellationToken);

            // Create audit log
            var auditLog = AuditLog.CreateAdminAction(
                Guid.Empty,
                "SCOPE_CREATED",
                "OAUTH_SCOPE",
                request.IpAddress ?? "Unknown",
                request.UserAgent ?? "Unknown",
                $"{{\"name\":\"{request.Name}\",\"type\":\"{request.Type}\"}}");

            await _auditLogRepository.AddAsync(auditLog, cancellationToken);

            _logger.LogInformation("Scope {ScopeName} created successfully", request.Name);

            // Map to DTO
            var scopeDto = new ScopeDto
            {
                Id = scope.Id,
                Name = scope.Name,
                DisplayName = scope.DisplayName,
                Description = scope.Description,
                Type = scope.Type.ToString(),
                IsRequired = scope.IsRequired,
                IsDefault = scope.IsDefault,
                ShowInDiscoveryDocument = scope.ShowInDiscoveryDocument,
                CreatedAt = scope.CreatedAt
            };

            return Result<ScopeDto>.Success(scopeDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating scope {ScopeName}", request.Name);
            return Result<ScopeDto>.Failure("An error occurred while creating the scope");
        }
    }
}

public record UpdateScopeCommand(
    string Name,
    string DisplayName,
    string Description,
    bool IsRequired,
    bool IsDefault,
    bool ShowInDiscoveryDocument,
    string? IpAddress = null,
    string? UserAgent = null) : ICommand<Result<ScopeDto>>;

public class UpdateScopeCommandHandler : ICommandHandler<UpdateScopeCommand, Result<ScopeDto>>
{
    private readonly IScopeRepository _scopeRepository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly ILogger<UpdateScopeCommandHandler> _logger;

    public UpdateScopeCommandHandler(
        IScopeRepository scopeRepository,
        IAuditLogRepository auditLogRepository,
        ILogger<UpdateScopeCommandHandler> logger)
    {
        _scopeRepository = scopeRepository;
        _auditLogRepository = auditLogRepository;
        _logger = logger;
    }

    public async Task<Result<ScopeDto>> Handle(UpdateScopeCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var scope = await _scopeRepository.GetByNameAsync(request.Name, cancellationToken);
            if (scope == null)
            {
                return Result<ScopeDto>.Failure("Scope not found");
            }

            // Update scope
            scope.UpdateDetails(request.DisplayName, request.Description);
            scope.SetAsRequired(request.IsRequired);
            scope.SetAsDefault(request.IsDefault);
            scope.SetDiscoveryVisibility(request.ShowInDiscoveryDocument);

            await _scopeRepository.UpdateAsync(scope, cancellationToken);

            // Create audit log
            var auditLog = AuditLog.CreateAdminAction(
                Guid.Empty,
                "SCOPE_UPDATED",
                "OAUTH_SCOPE",
                request.IpAddress ?? "Unknown",
                request.UserAgent ?? "Unknown",
                $"{{\"name\":\"{request.Name}\"}}");

            await _auditLogRepository.AddAsync(auditLog, cancellationToken);

            _logger.LogInformation("Scope {ScopeName} updated successfully", request.Name);

            // Map to DTO
            var scopeDto = new ScopeDto
            {
                Id = scope.Id,
                Name = scope.Name,
                DisplayName = scope.DisplayName,
                Description = scope.Description,
                Type = scope.Type.ToString(),
                IsRequired = scope.IsRequired,
                IsDefault = scope.IsDefault,
                ShowInDiscoveryDocument = scope.ShowInDiscoveryDocument,
                CreatedAt = scope.CreatedAt,
                UpdatedAt = scope.UpdatedAt
            };

            return Result<ScopeDto>.Success(scopeDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating scope {ScopeName}", request.Name);
            return Result<ScopeDto>.Failure("An error occurred while updating the scope");
        }
    }
}

public record DeleteScopeCommand(
    string Name,
    string? IpAddress = null,
    string? UserAgent = null) : ICommand<Result>;

public class DeleteScopeCommandHandler : ICommandHandler<DeleteScopeCommand, Result>
{
    private readonly IScopeRepository _scopeRepository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly ILogger<DeleteScopeCommandHandler> _logger;

    public DeleteScopeCommandHandler(
        IScopeRepository scopeRepository,
        IAuditLogRepository auditLogRepository,
        ILogger<DeleteScopeCommandHandler> logger)
    {
        _scopeRepository = scopeRepository;
        _auditLogRepository = auditLogRepository;
        _logger = logger;
    }

    public async Task<Result> Handle(DeleteScopeCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var scope = await _scopeRepository.GetByNameAsync(request.Name, cancellationToken);
            if (scope == null)
            {
                return Result.Failure("Scope not found");
            }

            // Check if it's a system scope that shouldn't be deleted
            var systemScopes = new[] { "openid", "profile", "email", "offline_access" };
            if (systemScopes.Contains(scope.Name.ToLower()))
            {
                return Result.Failure("System scopes cannot be deleted");
            }

            await _scopeRepository.DeleteAsync(scope, cancellationToken);

            // Create audit log
            var auditLog = AuditLog.CreateAdminAction(
                Guid.Empty,
                "SCOPE_DELETED",
                "OAUTH_SCOPE",
                request.IpAddress ?? "Unknown",
                request.UserAgent ?? "Unknown",
                $"{{\"name\":\"{request.Name}\"}}");

            await _auditLogRepository.AddAsync(auditLog, cancellationToken);

            _logger.LogInformation("Scope {ScopeName} deleted successfully", request.Name);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting scope {ScopeName}", request.Name);
            return Result.Failure("An error occurred while deleting the scope");
        }
    }
}
