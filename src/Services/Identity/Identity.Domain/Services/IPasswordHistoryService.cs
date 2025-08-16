namespace Identity.Domain.Services;

public interface IPasswordHistoryService
{
    /// <summary>
    /// Validates that a new password is not in the user's password history
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="newPasswordHash">New password hash to validate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if password is valid (not in history), false otherwise</returns>
    Task<bool> IsPasswordValidAsync(
        Guid userId, 
        string newPasswordHash, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Stores current password in history and updates to new password
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="newPasswordHash">New password hash</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task</returns>
    Task ChangePasswordWithHistoryAsync(
        Guid userId, 
        string newPasswordHash, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cleans up old password history entries for a user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task</returns>
    Task CleanupPasswordHistoryAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cleans up old password history entries for all users (maintenance task)
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task</returns>
    Task CleanupAllPasswordHistoryAsync(CancellationToken cancellationToken = default);
}
