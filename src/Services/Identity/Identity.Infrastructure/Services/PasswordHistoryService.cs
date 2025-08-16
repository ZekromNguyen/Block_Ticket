using Identity.Domain.Configuration;
using Identity.Domain.Entities;
using Identity.Domain.Repositories;
using Identity.Domain.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Identity.Infrastructure.Services;

public class PasswordHistoryService : IPasswordHistoryService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHistoryRepository _passwordHistoryRepository;
    private readonly PasswordConfiguration _passwordConfig;
    private readonly ILogger<PasswordHistoryService> _logger;

    public PasswordHistoryService(
        IUserRepository userRepository,
        IPasswordHistoryRepository passwordHistoryRepository,
        IOptions<PasswordConfiguration> passwordConfig,
        ILogger<PasswordHistoryService> logger)
    {
        _userRepository = userRepository;
        _passwordHistoryRepository = passwordHistoryRepository;
        _passwordConfig = passwordConfig.Value;
        _logger = logger;
    }

    public async Task<bool> IsPasswordValidAsync(
        Guid userId, 
        string newPasswordHash, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            // If password history is disabled, allow any password
            if (!_passwordConfig.EnablePasswordHistory)
            {
                return true;
            }

            // Check if password exists in history
            var isInHistory = await _passwordHistoryRepository.IsPasswordInHistoryAsync(
                userId, 
                newPasswordHash, 
                _passwordConfig.PasswordHistoryCount, 
                cancellationToken);

            if (isInHistory)
            {
                _logger.LogWarning("Password reuse attempted for user {UserId}", userId);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating password history for user {UserId}", userId);
            return false;
        }
    }

    public async Task ChangePasswordWithHistoryAsync(
        Guid userId, 
        string newPasswordHash, 
        CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            throw new ArgumentException("User not found", nameof(userId));
        }

        // Store current password in history if enabled and user has a current password
        if (_passwordConfig.EnablePasswordHistory && !string.IsNullOrEmpty(user.PasswordHash))
        {
            var historyEntry = new PasswordHistory(userId, user.PasswordHash);
            await _passwordHistoryRepository.AddAsync(historyEntry, cancellationToken);
            
            _logger.LogDebug("Added password to history for user {UserId}", userId);
        }

        // Change password using the new method that doesn't store in history
        // (since we already stored it above)
        user.ChangePasswordWithHistory(newPasswordHash, storeCurrentPasswordInHistory: false);

        await _userRepository.UpdateAsync(user, cancellationToken);

        // Clean up old password history entries
        if (_passwordConfig.EnablePasswordHistory)
        {
            await CleanupPasswordHistoryAsync(userId, cancellationToken);
        }

        _logger.LogInformation("Password changed with history tracking for user {UserId}", userId);
    }

    public async Task CleanupPasswordHistoryAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_passwordConfig.EnablePasswordHistory)
            {
                return;
            }

            await _passwordHistoryRepository.RemoveOldEntriesAsync(
                userId,
                _passwordConfig.PasswordHistoryRetentionDays,
                _passwordConfig.PasswordHistoryCount,
                cancellationToken);

            _logger.LogDebug("Cleaned up password history for user {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up password history for user {UserId}", userId);
            // Don't throw - this is a maintenance operation
        }
    }

    public async Task CleanupAllPasswordHistoryAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_passwordConfig.EnablePasswordHistory)
            {
                return;
            }

            // Get all users and clean up their password history
            var users = await _userRepository.GetAllAsync(cancellationToken);
            var cleanupTasks = users.Select(user => CleanupPasswordHistoryAsync(user.Id, cancellationToken));
            
            await Task.WhenAll(cleanupTasks);

            _logger.LogInformation("Completed password history cleanup for all users");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during global password history cleanup");
            // Don't throw - this is a maintenance operation
        }
    }
}
