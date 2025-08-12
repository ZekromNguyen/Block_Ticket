using System.ComponentModel.DataAnnotations;

namespace Identity.Application.DTOs;

public record UserDto
{
    public Guid Id { get; init; }
    public string Email { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string? WalletAddress { get; init; }
    public string UserType { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public bool EmailConfirmed { get; init; }
    public bool MfaEnabled { get; init; }
    public DateTime? LastLoginAt { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public record CreateUserDto
{
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string? WalletAddress { get; init; }
    public string UserType { get; init; } = string.Empty;
}

public record UpdateUserDto
{
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string? WalletAddress { get; init; }
}

public record ChangePasswordDto
{
    [Required(ErrorMessage = "Current password is required")]
    [MinLength(1, ErrorMessage = "Current password cannot be empty")]
    public string CurrentPassword { get; init; } = string.Empty;

    [Required(ErrorMessage = "New password is required")]
    [MinLength(8, ErrorMessage = "New password must be at least 8 characters long")]
    public string NewPassword { get; init; } = string.Empty;

    [Required(ErrorMessage = "Password confirmation is required")]
    [MinLength(1, ErrorMessage = "Password confirmation cannot be empty")]
    public string ConfirmPassword { get; init; } = string.Empty;
}
