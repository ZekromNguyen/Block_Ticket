using Shared.Common.Models;

namespace Event.Domain.Entities;

public class Promoter : BaseAuditableEntity
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    public ICollection<EventAggregate> Events { get; set; } = new List<EventAggregate>();
}

