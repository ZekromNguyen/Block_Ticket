using System;
using System.Collections.Generic;

namespace Event.Domain.Models;

public class PricingInfo
{
    public Guid EventId { get; set; }
    public string ETag { get; set; } = string.Empty;
    public List<TicketTypePricing> TicketTypes { get; set; } = new();
    public List<PricingRuleInfo> PricingRules { get; set; } = new();
}

public class TicketTypePricing
{
    public Guid TicketTypeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal BasePrice { get; set; }
}

public class PricingRuleInfo
{
    public Guid RuleId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Adjustment { get; set; }
    // Add other relevant rule properties as needed
}

