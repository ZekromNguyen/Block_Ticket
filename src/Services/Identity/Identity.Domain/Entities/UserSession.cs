using Shared.Common.Models;

namespace Identity.Domain.Entities;

public class UserSession : BaseEntity
{
    public Guid UserId { get; private set; }
    public string DeviceInfo { get; private set; } = string.Empty;
    public string IpAddress { get; private set; } = string.Empty;
    public DateTime ExpiresAt { get; private set; }
    public DateTime? EndedAt { get; private set; }
    public bool IsActive => EndedAt == null && ExpiresAt > DateTime.UtcNow;
    public string? RefreshToken { get; private set; }
    public DateTime? RefreshTokenExpiresAt { get; private set; }

    private UserSession() { } // For EF Core

    public UserSession(Guid userId, string deviceInfo, string ipAddress, TimeSpan? sessionDuration = null)
    {
        UserId = userId;
        DeviceInfo = deviceInfo;
        IpAddress = ipAddress;
        ExpiresAt = DateTime.UtcNow.Add(sessionDuration ?? TimeSpan.FromHours(24));
    }

    public void SetRefreshToken(string refreshToken, DateTime expiresAt)
    {
        RefreshToken = refreshToken;
        RefreshTokenExpiresAt = expiresAt;
        UpdatedAt = DateTime.UtcNow;
    }

    public void RevokeRefreshToken()
    {
        RefreshToken = null;
        RefreshTokenExpiresAt = null;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ExtendSession(TimeSpan extension)
    {
        if (!IsActive)
            throw new InvalidOperationException("Cannot extend inactive session");

        ExpiresAt = ExpiresAt.Add(extension);
        UpdatedAt = DateTime.UtcNow;
    }

    public void End()
    {
        EndedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        RevokeRefreshToken();
    }

    public bool IsRefreshTokenValid()
    {
        return !string.IsNullOrEmpty(RefreshToken) && 
               RefreshTokenExpiresAt.HasValue && 
               RefreshTokenExpiresAt.Value > DateTime.UtcNow;
    }
}
