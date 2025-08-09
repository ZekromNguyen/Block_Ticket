using Identity.Application.Common.Interfaces;
using Identity.Application.Common.Models;
using Identity.Application.DTOs;
using Identity.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Identity.Application.Features.Users.Queries;

public record GetUserByIdQuery(Guid UserId) : IQuery<Result<UserDto>>;

public class GetUserByIdQueryHandler : IQueryHandler<GetUserByIdQuery, Result<UserDto>>
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<GetUserByIdQueryHandler> _logger;

    public GetUserByIdQueryHandler(
        IUserRepository userRepository,
        ILogger<GetUserByIdQueryHandler> logger)
    {
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<Result<UserDto>> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
            if (user == null)
            {
                return Result<UserDto>.Failure("User not found");
            }

            var userDto = new UserDto
            {
                Id = user.Id,
                Email = user.Email.Value,
                FirstName = user.FirstName,
                LastName = user.LastName,
                WalletAddress = user.WalletAddress?.Value,
                UserType = user.UserType.ToString(),
                Status = user.Status.ToString(),
                EmailConfirmed = user.EmailConfirmed,
                MfaEnabled = user.MfaEnabled,
                LastLoginAt = user.LastLoginAt,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt
            };

            return Result<UserDto>.Success(userDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user {UserId}", request.UserId);
            return Result<UserDto>.Failure("An error occurred while retrieving user");
        }
    }
}

public record GetUserByEmailQuery(string Email) : IQuery<Result<UserDto>>;

public class GetUserByEmailQueryHandler : IQueryHandler<GetUserByEmailQuery, Result<UserDto>>
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<GetUserByEmailQueryHandler> _logger;

    public GetUserByEmailQueryHandler(
        IUserRepository userRepository,
        ILogger<GetUserByEmailQueryHandler> logger)
    {
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<Result<UserDto>> Handle(GetUserByEmailQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
            if (user == null)
            {
                return Result<UserDto>.Failure("User not found");
            }

            var userDto = new UserDto
            {
                Id = user.Id,
                Email = user.Email.Value,
                FirstName = user.FirstName,
                LastName = user.LastName,
                WalletAddress = user.WalletAddress?.Value,
                UserType = user.UserType.ToString(),
                Status = user.Status.ToString(),
                EmailConfirmed = user.EmailConfirmed,
                MfaEnabled = user.MfaEnabled,
                LastLoginAt = user.LastLoginAt,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt
            };

            return Result<UserDto>.Success(userDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user by email {Email}", request.Email);
            return Result<UserDto>.Failure("An error occurred while retrieving user");
        }
    }
}
