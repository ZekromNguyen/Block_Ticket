using Shared.Common.Models;

namespace Identity.Domain.Events;

public record UserRegisteredDomainEvent(Guid UserId, string Email, string UserType) : IDomainEvent;

public record UserEmailConfirmedDomainEvent(Guid UserId, string Email) : IDomainEvent;

public record UserProfileUpdatedDomainEvent(Guid UserId, string FirstName, string LastName) : IDomainEvent;

public record UserPasswordChangedDomainEvent(Guid UserId) : IDomainEvent;

public record UserLoggedInDomainEvent(Guid UserId, string Email, DateTime LoginAt) : IDomainEvent;

public record UserLoginFailedDomainEvent(Guid UserId, string Email, int FailedAttempts) : IDomainEvent;

public record UserAccountLockedDomainEvent(Guid UserId, string Email, DateTime LockedUntil) : IDomainEvent;

public record UserAccountUnlockedDomainEvent(Guid UserId, string Email) : IDomainEvent;

public record UserMfaEnabledDomainEvent(Guid UserId, string Email) : IDomainEvent;

public record UserMfaDisabledDomainEvent(Guid UserId, string Email) : IDomainEvent;

public record UserMfaDeviceAddedDomainEvent(Guid UserId, string DeviceType) : IDomainEvent;

public record UserSessionCreatedDomainEvent(Guid UserId, Guid SessionId, string DeviceInfo, string IpAddress) : IDomainEvent;

public record UserSessionEndedDomainEvent(Guid UserId, Guid SessionId, string Reason) : IDomainEvent;

public record UserAllSessionsEndedDomainEvent(Guid UserId) : IDomainEvent;

public record UserSessionLimitExceededDomainEvent(Guid UserId, int MaxAllowed, int CurrentActive) : IDomainEvent;

public record UserRoleAssignedDomainEvent(Guid UserId, Guid RoleId) : IDomainEvent;

public record UserMfaDeviceRemovedDomainEvent(Guid UserId, string DeviceType) : IDomainEvent;

public record UserRoleRemovedDomainEvent(Guid UserId, Guid RoleId) : IDomainEvent;
