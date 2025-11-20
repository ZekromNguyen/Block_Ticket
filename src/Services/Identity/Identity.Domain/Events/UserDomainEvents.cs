using MediatR;
using Shared.Common.Models;

namespace Identity.Domain.Events;

public record UserRegisteredDomainEvent(Guid UserId, string Email, string UserType) : IDomainEvent;

public record UserEmailConfirmedDomainEvent(Guid UserId, string Email) : IDomainEvent;

public record UserProfileUpdatedDomainEvent(Guid UserId, string FirstName, string LastName) : IDomainEvent;

public record UserPasswordChangedDomainEvent(Guid UserId) : IDomainEvent, INotification;

public record UserLoggedInDomainEvent(Guid UserId, string Email, DateTime LoginAt) : IDomainEvent, INotification;

public record UserLoginFailedDomainEvent(Guid UserId, string Email, int FailedAttempts) : IDomainEvent, INotification;

public record UserAccountLockedDomainEvent(Guid UserId, string Email, DateTime LockedUntil) : IDomainEvent, INotification;

public record UserAccountUnlockedDomainEvent(Guid UserId, string Email) : IDomainEvent, INotification;

public record UserMfaEnabledDomainEvent(Guid UserId, string Email) : IDomainEvent, INotification;

public record UserMfaDisabledDomainEvent(Guid UserId, string Email) : IDomainEvent, INotification;

public record UserMfaDeviceAddedDomainEvent(Guid UserId, string DeviceType) : IDomainEvent;

public record UserMfaDeviceRemovedDomainEvent(Guid UserId, string DeviceType) : IDomainEvent;

public record UserAllSessionsEndedDomainEvent(Guid UserId) : IDomainEvent;

public record UserRoleAssignedDomainEvent(Guid UserId, Guid RoleId) : IDomainEvent;

public record UserRoleRemovedDomainEvent(Guid UserId, Guid RoleId) : IDomainEvent;
