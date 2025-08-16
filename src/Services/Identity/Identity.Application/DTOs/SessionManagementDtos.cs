namespace Identity.Application.DTOs;

public class UserSessionDto
{
    public Guid Id { get; set; }
    public string DeviceInfo { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public bool IsActive { get; set; }
    public bool IsCurrentSession { get; set; }
}

public class SessionLimitInfoDto
{
    public int MaxAllowedSessions { get; set; }
    public int CurrentActiveSessions { get; set; }
    public bool CanCreateNewSession { get; set; }
    public string LimitBehavior { get; set; } = string.Empty;
    public IEnumerable<UserSessionDto> ActiveSessions { get; set; } = new List<UserSessionDto>();
}
