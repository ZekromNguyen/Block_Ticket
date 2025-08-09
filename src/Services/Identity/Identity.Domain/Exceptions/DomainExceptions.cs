namespace Identity.Domain.Exceptions;

public abstract class DomainException : Exception
{
    protected DomainException(string message) : base(message) { }
    protected DomainException(string message, Exception innerException) : base(message, innerException) { }
}

public class UserNotFoundException : DomainException
{
    public UserNotFoundException(Guid userId) : base($"User with ID {userId} was not found") { }
    public UserNotFoundException(string email) : base($"User with email {email} was not found") { }
}

public class UserAlreadyExistsException : DomainException
{
    public UserAlreadyExistsException(string email) : base($"User with email {email} already exists") { }
}

public class InvalidCredentialsException : DomainException
{
    public InvalidCredentialsException() : base("Invalid email or password") { }
}

public class AccountLockedException : DomainException
{
    public DateTime LockedUntil { get; }
    
    public AccountLockedException(DateTime lockedUntil) : base($"Account is locked until {lockedUntil}")
    {
        LockedUntil = lockedUntil;
    }
}

public class EmailNotConfirmedException : DomainException
{
    public EmailNotConfirmedException() : base("Email address has not been confirmed") { }
}

public class MfaRequiredException : DomainException
{
    public MfaRequiredException() : base("Multi-factor authentication is required") { }
}

public class InvalidMfaCodeException : DomainException
{
    public InvalidMfaCodeException() : base("Invalid MFA code provided") { }
}

public class MfaDeviceNotFoundException : DomainException
{
    public MfaDeviceNotFoundException(Guid deviceId) : base($"MFA device with ID {deviceId} was not found") { }
}

public class SessionNotFoundException : DomainException
{
    public SessionNotFoundException(Guid sessionId) : base($"Session with ID {sessionId} was not found") { }
}

public class SessionExpiredException : DomainException
{
    public SessionExpiredException() : base("Session has expired") { }
}

public class InvalidTokenException : DomainException
{
    public InvalidTokenException(string tokenType) : base($"Invalid {tokenType} token") { }
}

public class TokenExpiredException : DomainException
{
    public TokenExpiredException(string tokenType) : base($"{tokenType} token has expired") { }
}

public class WeakPasswordException : DomainException
{
    public WeakPasswordException(string requirements) : base($"Password does not meet requirements: {requirements}") { }
}
