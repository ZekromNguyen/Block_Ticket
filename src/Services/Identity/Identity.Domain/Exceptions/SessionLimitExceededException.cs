namespace Identity.Domain.Exceptions;

public class SessionLimitExceededException : Exception
{
    public int MaxAllowedSessions { get; }
    public int CurrentActiveSessions { get; }

    public SessionLimitExceededException(int maxAllowedSessions, int currentActiveSessions)
        : base($"Session limit exceeded. Maximum allowed: {maxAllowedSessions}, Current active: {currentActiveSessions}")
    {
        MaxAllowedSessions = maxAllowedSessions;
        CurrentActiveSessions = currentActiveSessions;
    }

    public SessionLimitExceededException(int maxAllowedSessions, int currentActiveSessions, string message)
        : base(message)
    {
        MaxAllowedSessions = maxAllowedSessions;
        CurrentActiveSessions = currentActiveSessions;
    }
}
