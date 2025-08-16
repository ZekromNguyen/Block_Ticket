using Event.Domain.Entities;

namespace Event.Domain.Interfaces;

/// <summary>
/// Repository interface for PricingRule aggregate
/// </summary>
public interface IPricingRuleRepository : IRepository<PricingRule>
{
    Task<IEnumerable<PricingRule>> GetByEventIdAsync(Guid eventId, CancellationToken cancellationToken = default);
    Task<PricingRule?> GetByDiscountCodeAsync(Guid eventId, string discountCode, CancellationToken cancellationToken = default);
    Task<bool> DiscountCodeExistsAsync(Guid eventId, string discountCode, CancellationToken cancellationToken = default);
    Task<IEnumerable<PricingRule>> GetActiveRulesForEventAsync(Guid eventId, DateTime effectiveDate, CancellationToken cancellationToken = default);
    Task<IEnumerable<PricingRule>> GetByTicketTypeAsync(Guid ticketTypeId, CancellationToken cancellationToken = default);
    Task<(IEnumerable<PricingRule> Rules, int TotalCount)> GetPagedByEventAsync(
        Guid eventId,
        int pageNumber,
        int pageSize,
        string? type = null,
        bool? isActive = null,
        CancellationToken cancellationToken = default);
}
