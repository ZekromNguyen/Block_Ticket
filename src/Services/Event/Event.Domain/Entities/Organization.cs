using Shared.Common.Models;

namespace Event.Domain.Entities;

public class Organization : BaseAuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    public ICollection<EventAggregate> Events { get; set; } = new List<EventAggregate>();
}

