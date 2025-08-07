using Microsoft.AspNetCore.Identity;
using Shared.Common.Models;

namespace Identity.Api.Models;

public class ApplicationUser : IdentityUser
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? WalletAddress { get; set; }
    public UserType UserType { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

public enum UserType
{
    Fan,
    Promoter,
    Admin
}
