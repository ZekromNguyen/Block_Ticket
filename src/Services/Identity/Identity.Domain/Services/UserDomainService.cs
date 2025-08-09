using Identity.Domain.Entities;
using Identity.Domain.Exceptions;
using Identity.Domain.Repositories;
using Identity.Domain.ValueObjects;

namespace Identity.Domain.Services;

public class UserDomainService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordService _passwordService;

    public UserDomainService(IUserRepository userRepository, IPasswordService passwordService)
    {
        _userRepository = userRepository;
        _passwordService = passwordService;
    }

    public async Task<User> RegisterUserAsync(
        Email email, 
        string password, 
        string firstName, 
        string lastName, 
        UserType userType, 
        WalletAddress? walletAddress = null,
        CancellationToken cancellationToken = default)
    {
        // Check if user already exists
        if (await _userRepository.ExistsAsync(email, cancellationToken))
            throw new UserAlreadyExistsException(email.Value);

        // Check if wallet address is already in use
        if (walletAddress != null && await _userRepository.ExistsAsync(walletAddress, cancellationToken))
            throw new UserAlreadyExistsException($"Wallet address {walletAddress.Value} is already in use");

        // Validate password strength
        if (!_passwordService.IsPasswordStrong(password))
            throw new WeakPasswordException("Password must contain at least 8 characters, including uppercase, lowercase, digit, and special character");

        // Hash password
        var passwordHash = _passwordService.HashPassword(password);

        // Create user
        var user = new User(email, firstName, lastName, passwordHash, userType, walletAddress);

        return user;
    }

    public async Task<User> AuthenticateUserAsync(
        Email email, 
        string password, 
        CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByEmailAsync(email, cancellationToken);
        if (user == null)
            throw new UserNotFoundException(email.Value);

        // Check if account is locked
        if (user.IsLockedOut())
            throw new AccountLockedException(user.LockedOutUntil!.Value);

        // Verify password
        if (!_passwordService.VerifyPassword(password, user.PasswordHash))
        {
            user.RecordFailedLogin();
            await _userRepository.UpdateAsync(user, cancellationToken);
            throw new InvalidCredentialsException();
        }

        // Check if email is confirmed (optional based on business rules)
        if (!user.EmailConfirmed)
            throw new EmailNotConfirmedException();

        // Record successful login
        user.RecordSuccessfulLogin();
        await _userRepository.UpdateAsync(user, cancellationToken);

        return user;
    }

    public async Task ChangePasswordAsync(
        Guid userId, 
        string currentPassword, 
        string newPassword, 
        CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null)
            throw new UserNotFoundException(userId);

        // Verify current password
        if (!_passwordService.VerifyPassword(currentPassword, user.PasswordHash))
            throw new InvalidCredentialsException();

        // Validate new password strength
        if (!_passwordService.IsPasswordStrong(newPassword))
            throw new WeakPasswordException("Password must contain at least 8 characters, including uppercase, lowercase, digit, and special character");

        // Hash new password
        var newPasswordHash = _passwordService.HashPassword(newPassword);

        // Change password
        user.ChangePassword(newPasswordHash);

        await _userRepository.UpdateAsync(user, cancellationToken);
    }

    public async Task ResetPasswordAsync(
        Email email, 
        string newPassword, 
        CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByEmailAsync(email, cancellationToken);
        if (user == null)
            throw new UserNotFoundException(email.Value);

        // Validate new password strength
        if (!_passwordService.IsPasswordStrong(newPassword))
            throw new WeakPasswordException("Password must contain at least 8 characters, including uppercase, lowercase, digit, and special character");

        // Hash new password
        var newPasswordHash = _passwordService.HashPassword(newPassword);

        // Reset password
        user.ChangePassword(newPasswordHash);

        // End all sessions for security
        user.EndAllSessions();

        await _userRepository.UpdateAsync(user, cancellationToken);
    }

    public async Task<bool> CanUserAccessResourceAsync(
        Guid userId, 
        string resource, 
        string action, 
        CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null)
            return false;

        // Check if user is active
        if (user.Status != UserStatus.Active)
            return false;

        // Check if account is locked
        if (user.IsLockedOut())
            return false;

        // Basic role-based access control
        return user.UserType switch
        {
            UserType.Admin => true, // Admin can access everything
            UserType.Promoter => IsPromoterResource(resource, action),
            UserType.Fan => IsFanResource(resource, action),
            _ => false
        };
    }

    private static bool IsPromoterResource(string resource, string action)
    {
        // Define promoter permissions
        return resource.ToLower() switch
        {
            "events" => action.ToLower() is "create" or "read" or "update" or "delete",
            "tickets" => action.ToLower() is "read",
            "profile" => action.ToLower() is "read" or "update",
            _ => false
        };
    }

    private static bool IsFanResource(string resource, string action)
    {
        // Define fan permissions
        return resource.ToLower() switch
        {
            "events" => action.ToLower() is "read",
            "tickets" => action.ToLower() is "read" or "purchase",
            "profile" => action.ToLower() is "read" or "update",
            _ => false
        };
    }
}
