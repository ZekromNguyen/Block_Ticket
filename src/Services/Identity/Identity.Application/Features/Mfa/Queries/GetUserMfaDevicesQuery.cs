using Identity.Application.Common.Interfaces;
using Identity.Application.Common.Models;
using Identity.Application.DTOs;
using Identity.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Identity.Application.Features.Mfa.Queries;

public record GetUserMfaDevicesQuery(Guid UserId) : IQuery<Result<IEnumerable<MfaDeviceDto>>>;

public class GetUserMfaDevicesQueryHandler : IQueryHandler<GetUserMfaDevicesQuery, Result<IEnumerable<MfaDeviceDto>>>
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<GetUserMfaDevicesQueryHandler> _logger;

    public GetUserMfaDevicesQueryHandler(
        IUserRepository userRepository,
        ILogger<GetUserMfaDevicesQueryHandler> logger)
    {
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<Result<IEnumerable<MfaDeviceDto>>> Handle(GetUserMfaDevicesQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
            if (user == null)
            {
                return Result<IEnumerable<MfaDeviceDto>>.Failure("User not found");
            }

            var deviceDtos = user.MfaDevices
                .Where(d => d.Type != Domain.Entities.MfaDeviceType.BackupCodes) // Don't expose backup codes
                .Select(d => new MfaDeviceDto
                {
                    Id = d.Id,
                    Type = d.Type.ToString(),
                    Name = d.Name,
                    IsActive = d.IsActive,
                    LastUsedAt = d.LastUsedAt,
                    UsageCount = d.UsageCount,
                    CreatedAt = d.CreatedAt
                })
                .OrderBy(d => d.Type)
                .ThenBy(d => d.CreatedAt);

            return Result<IEnumerable<MfaDeviceDto>>.Success(deviceDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving MFA devices for user {UserId}", request.UserId);
            return Result<IEnumerable<MfaDeviceDto>>.Failure("An error occurred while retrieving MFA devices");
        }
    }
}

public record GetMfaStatusQuery(Guid UserId) : IQuery<Result<MfaStatusDto>>;

public class GetMfaStatusQueryHandler : IQueryHandler<GetMfaStatusQuery, Result<MfaStatusDto>>
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<GetMfaStatusQueryHandler> _logger;

    public GetMfaStatusQueryHandler(
        IUserRepository userRepository,
        ILogger<GetMfaStatusQueryHandler> logger)
    {
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<Result<MfaStatusDto>> Handle(GetMfaStatusQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
            if (user == null)
            {
                return Result<MfaStatusDto>.Failure("User not found");
            }

            var activeDevices = user.MfaDevices
                .Where(d => d.IsActive && d.CanBeUsed())
                .ToList();

            var hasBackupCodes = user.MfaDevices
                .Any(d => d.Type == Domain.Entities.MfaDeviceType.BackupCodes && d.IsActive);

            var availableMethods = new List<string>();
            
            if (activeDevices.Any(d => d.Type == Domain.Entities.MfaDeviceType.Totp))
                availableMethods.Add("TOTP");
            
            if (activeDevices.Any(d => d.Type == Domain.Entities.MfaDeviceType.EmailOtp))
                availableMethods.Add("EmailOTP");
            
            if (activeDevices.Any(d => d.Type == Domain.Entities.MfaDeviceType.WebAuthn))
                availableMethods.Add("WebAuthn");

            if (hasBackupCodes)
                availableMethods.Add("BackupCodes");

            var mfaStatus = new MfaStatusDto
            {
                IsEnabled = user.MfaEnabled,
                DeviceCount = activeDevices.Count,
                AvailableMethods = availableMethods.ToArray(),
                HasBackupCodes = hasBackupCodes,
                LastUsedAt = activeDevices.Max(d => d.LastUsedAt)
            };

            return Result<MfaStatusDto>.Success(mfaStatus);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving MFA status for user {UserId}", request.UserId);
            return Result<MfaStatusDto>.Failure("An error occurred while retrieving MFA status");
        }
    }
}

// Add the missing DTO
public record MfaStatusDto
{
    public bool IsEnabled { get; init; }
    public int DeviceCount { get; init; }
    public string[] AvailableMethods { get; init; } = Array.Empty<string>();
    public bool HasBackupCodes { get; init; }
    public DateTime? LastUsedAt { get; init; }
}
