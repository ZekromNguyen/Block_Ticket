using Identity.Domain.ValueObjects;
using Identity.Domain.Events;
using Shared.Common.Models;

namespace Identity.Domain.Entities;

public class User : BaseAuditableEntity
{
    private readonly List<UserSession> _sessions = new();
    private readonly List<MfaDevice> _mfaDevices = new();
    private readonly List<UserRole> _userRoles = new();
    private readonly List<PasswordHistory> _passwordHistory = new();

    public Email Email { get; private set; } = null!;
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public UserType UserType { get; private set; }
    public WalletAddress? WalletAddress { get; private set; }
    public UserStatus Status { get; private set; }
    public bool EmailConfirmed { get; private set; }
    public DateTime? EmailConfirmedAt { get; private set; }
    public bool MfaEnabled { get; private set; }
    public DateTime? LastLoginAt { get; private set; }
    public int FailedLoginAttempts { get; private set; }
    public DateTime? LockedOutUntil { get; private set; }

    // Navigation properties
    public IReadOnlyCollection<UserSession> Sessions => _sessions.AsReadOnly();
    public IReadOnlyCollection<MfaDevice> MfaDevices => _mfaDevices.AsReadOnly();
    public IReadOnlyCollection<UserRole> UserRoles => _userRoles.AsReadOnly();
    public IReadOnlyCollection<PasswordHistory> PasswordHistory => _passwordHistory.AsReadOnly();

    private User() { } // For EF Core

    public User(Email email, string firstName, string lastName, string passwordHash, UserType userType, WalletAddress? walletAddress = null)
    {
        Email = email;
        FirstName = firstName;
        LastName = lastName;
        PasswordHash = passwordHash;
        UserType = userType;
        WalletAddress = walletAddress;
        Status = UserStatus.Active;
        EmailConfirmed = false;
        MfaEnabled = false;
        FailedLoginAttempts = 0;

        AddDomainEvent(new UserRegisteredDomainEvent(Id, email.Value, userType.ToString()));
    }

    public void ConfirmEmail()
    {
        if (EmailConfirmed)
            throw new InvalidOperationException("Email is already confirmed");

        EmailConfirmed = true;
        EmailConfirmedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new UserEmailConfirmedDomainEvent(Id, Email.Value));
    }

    public void UpdateProfile(string firstName, string lastName, WalletAddress? walletAddress = null)
    {
        FirstName = firstName;
        LastName = lastName;
        WalletAddress = walletAddress;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new UserProfileUpdatedDomainEvent(Id, firstName, lastName));
    }

    public void ChangePassword(string newPasswordHash)
    {
        PasswordHash = newPasswordHash;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new UserPasswordChangedDomainEvent(Id));
    }

    /// <summary>
    /// Changes password with history tracking support
    /// </summary>
    /// <param name="newPasswordHash">The new password hash</param>
    /// <param name="storeCurrentPasswordInHistory">Whether to store current password in history</param>
    public void ChangePasswordWithHistory(string newPasswordHash, bool storeCurrentPasswordInHistory = true)
    {
        // Store current password in history before changing it
        if (storeCurrentPasswordInHistory && !string.IsNullOrEmpty(PasswordHash))
        {
            var historyEntry = new PasswordHistory(Id, PasswordHash);
            _passwordHistory.Add(historyEntry);
        }

        // Change to new password
        PasswordHash = newPasswordHash;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new UserPasswordChangedDomainEvent(Id));
    }

    /// <summary>
    /// Checks if a password hash exists in the user's password history
    /// </summary>
    /// <param name="passwordHash">The password hash to check</param>
    /// <param name="historyCount">Number of recent passwords to check (0 means check all)</param>
    /// <returns>True if password exists in history, false otherwise</returns>
    public bool IsPasswordInHistory(string passwordHash, int historyCount = 0)
    {
        if (string.IsNullOrEmpty(passwordHash))
            return false;

        var historyToCheck = historyCount > 0 
            ? _passwordHistory.OrderByDescending(h => h.CreatedAt).Take(historyCount)
            : _passwordHistory;

        return historyToCheck.Any(h => h.PasswordHash == passwordHash);
    }

    /// <summary>
    /// Cleans up old password history entries based on retention policy
    /// </summary>
    /// <param name="retentionDays">Number of days to retain password history</param>
    /// <param name="maxHistoryCount">Maximum number of entries to keep regardless of age</param>
    public void CleanupPasswordHistory(int retentionDays, int maxHistoryCount)
    {
        if (!_passwordHistory.Any()) return;

        var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);
        var orderedHistory = _passwordHistory.OrderByDescending(h => h.CreatedAt).ToList();

        // Keep the most recent entries up to maxHistoryCount
        var entriesToKeep = orderedHistory.Take(maxHistoryCount).ToList();

        // Also keep any entries within retention period that aren't already kept
        var entriesWithinRetention = orderedHistory
            .Skip(maxHistoryCount)
            .Where(h => h.CreatedAt > cutoffDate)
            .ToList();

        var finalEntriesToKeep = entriesToKeep.Union(entriesWithinRetention).ToList();
        var entriesToRemove = _passwordHistory.Except(finalEntriesToKeep).ToList();

        foreach (var entry in entriesToRemove)
        {
            _passwordHistory.Remove(entry);
        }
    }

    public void RecordSuccessfulLogin()
    {
        LastLoginAt = DateTime.UtcNow;
        FailedLoginAttempts = 0;
        LockedOutUntil = null;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new UserLoggedInDomainEvent(Id, Email.Value, LastLoginAt.Value));
    }

    public void RecordFailedLogin()
    {
        FailedLoginAttempts++;
        UpdatedAt = DateTime.UtcNow;

        if (FailedLoginAttempts >= 5) // Configurable threshold
        {
            LockAccount(TimeSpan.FromMinutes(30)); // Configurable duration
        }

        AddDomainEvent(new UserLoginFailedDomainEvent(Id, Email.Value, FailedLoginAttempts));
    }

    public void LockAccount(TimeSpan duration)
    {
        Status = UserStatus.LockedOut;
        LockedOutUntil = DateTime.UtcNow.Add(duration);
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new UserAccountLockedDomainEvent(Id, Email.Value, LockedOutUntil.Value));
    }

    public void UnlockAccount()
    {
        if (Status != UserStatus.LockedOut)
            throw new InvalidOperationException("Account is not locked");

        Status = UserStatus.Active;
        LockedOutUntil = null;
        FailedLoginAttempts = 0;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new UserAccountUnlockedDomainEvent(Id, Email.Value));
    }

    public bool IsLockedOut()
    {
        return Status == UserStatus.LockedOut && 
               LockedOutUntil.HasValue && 
               LockedOutUntil.Value > DateTime.UtcNow;
    }

    public void EnableMfa()
    {
        MfaEnabled = true;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new UserMfaEnabledDomainEvent(Id, Email.Value));
    }

    public void DisableMfa()
    {
        MfaEnabled = false;
        _mfaDevices.Clear();
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new UserMfaDisabledDomainEvent(Id, Email.Value));
    }

    public void AddMfaDevice(MfaDevice device)
    {
        if (_mfaDevices.Any(d => d.Type == device.Type && d.IsActive))
            throw new InvalidOperationException($"Active MFA device of type {device.Type} already exists");

        _mfaDevices.Add(device);
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new UserMfaDeviceAddedDomainEvent(Id, device.Type.ToString()));
    }

    public void RemoveMfaDevice(Guid deviceId)
    {
        var device = _mfaDevices.FirstOrDefault(d => d.Id == deviceId);
        if (device == null)
            throw new InvalidOperationException("MFA device not found");

        _mfaDevices.Remove(device);
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new UserMfaDeviceRemovedDomainEvent(Id, device.Type.ToString()));
    }

    public UserSession CreateSession(string deviceInfo, string ipAddress)
    {
        var session = new UserSession(Id, deviceInfo, ipAddress);
        _sessions.Add(session);
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new UserSessionCreatedDomainEvent(Id, session.Id, deviceInfo, ipAddress));

        return session;
    }

    public void EndSession(Guid sessionId)
    {
        var session = _sessions.FirstOrDefault(s => s.Id == sessionId);
        if (session != null)
        {
            session.End();
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public void EndAllSessions()
    {
        foreach (var session in _sessions.Where(s => s.IsActive))
        {
            session.End();
        }
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new UserAllSessionsEndedDomainEvent(Id));
    }

    public void AssignRole(Guid roleId, string? assignedBy = null, DateTime? expiresAt = null, bool updateTimestamp = true)
    {
        if (_userRoles.Any(ur => ur.RoleId == roleId && ur.IsValid()))
            return; // Role already assigned and active

        var userRole = new UserRole(Id, roleId, assignedBy, expiresAt);
        _userRoles.Add(userRole);

        // Only update timestamp if explicitly requested (avoid concurrency issues during registration)
        if (updateTimestamp)
        {
            UpdatedAt = DateTime.UtcNow;
        }

        AddDomainEvent(new UserRoleAssignedDomainEvent(Id, roleId));
    }

    public void RemoveRole(Guid roleId)
    {
        var userRole = _userRoles.FirstOrDefault(ur => ur.RoleId == roleId && ur.IsActive);
        if (userRole != null)
        {
            userRole.Deactivate();
            UpdatedAt = DateTime.UtcNow;

            AddDomainEvent(new UserRoleRemovedDomainEvent(Id, roleId));
        }
    }

    public bool HasRole(Guid roleId)
    {
        return _userRoles.Any(ur => ur.RoleId == roleId && ur.IsValid());
    }

    public bool HasAnyRole(params Guid[] roleIds)
    {
        return _userRoles.Any(ur => roleIds.Contains(ur.RoleId) && ur.IsValid());
    }

    public IEnumerable<Role> GetActiveRoles()
    {
        return _userRoles
            .Where(ur => ur.IsValid())
            .Select(ur => ur.Role)
            .OrderByDescending(r => r.Priority);
    }
}

public enum UserType
{
    Fan = 0,
    Promoter = 1,
    Admin = 2
}

public enum UserStatus
{
    Active = 0,
    Inactive = 1,
    LockedOut = 2,
    Suspended = 3
}
