using Event.Domain.Entities;
using Event.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Event.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for PricingRule aggregate
/// </summary>
public class PricingRuleRepository : BaseRepository<PricingRule>, IPricingRuleRepository
{
    private readonly ILogger<PricingRuleRepository> _logger;

    public PricingRuleRepository(EventDbContext context, ILogger<PricingRuleRepository> logger)
        : base(context)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IEnumerable<PricingRule>> GetByEventIdAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting pricing rules for event {EventId}", eventId);

        return await Context.PricingRules
            .Where(pr => pr.EventId == eventId && !pr.IsDeleted)
            .OrderBy(pr => pr.Priority)
            .ThenBy(pr => pr.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<PricingRule?> GetByDiscountCodeAsync(Guid eventId, string discountCode, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(discountCode))
            return null;

        _logger.LogDebug("Getting pricing rule by discount code {DiscountCode} for event {EventId}", discountCode, eventId);

        var normalizedCode = discountCode.Trim().ToUpperInvariant();

        return await Context.PricingRules
            .Where(pr => pr.EventId == eventId &&
                        pr.DiscountCode == normalizedCode &&
                        !pr.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> DiscountCodeExistsAsync(Guid eventId, string discountCode, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(discountCode))
            return false;

        _logger.LogDebug("Checking if discount code {DiscountCode} exists for event {EventId}", discountCode, eventId);

        var normalizedCode = discountCode.Trim().ToUpperInvariant();

        return await Context.PricingRules
            .AnyAsync(pr => pr.EventId == eventId &&
                           pr.DiscountCode == normalizedCode &&
                           !pr.IsDeleted,
                     cancellationToken);
    }

    public async Task<IEnumerable<PricingRule>> GetActiveRulesForEventAsync(Guid eventId, DateTime effectiveDate, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting active pricing rules for event {EventId} effective at {EffectiveDate}", eventId, effectiveDate);

        return await Context.PricingRules
            .Where(pr => pr.EventId == eventId &&
                        pr.IsActive &&
                        !pr.IsDeleted &&
                        pr.EffectiveFrom <= effectiveDate &&
                        (pr.EffectiveTo == null || pr.EffectiveTo >= effectiveDate))
            .OrderBy(pr => pr.Priority)
            .ThenBy(pr => pr.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<PricingRule>> GetByTicketTypeAsync(Guid ticketTypeId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting pricing rules targeting ticket type {TicketTypeId}", ticketTypeId);

        return await Context.PricingRules
            .Where(pr => pr.IsActive &&
                        !pr.IsDeleted &&
                        (pr.TargetTicketTypeIds == null ||
                         pr.TargetTicketTypeIds.Contains(ticketTypeId)))
            .OrderBy(pr => pr.Priority)
            .ThenBy(pr => pr.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<PricingRule>> GetByTypeAsync(Guid eventId, Domain.Enums.PricingRuleType type, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting pricing rules of type {Type} for event {EventId}", type, eventId);

        return await Context.PricingRules
            .Where(pr => pr.EventId == eventId &&
                        pr.Type == type &&
                        !pr.IsDeleted)
            .OrderBy(pr => pr.Priority)
            .ThenBy(pr => pr.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<PricingRule>> GetExpiredRulesAsync(DateTime cutoffDate, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting expired pricing rules before {CutoffDate}", cutoffDate);

        return await Context.PricingRules
            .Where(pr => pr.EffectiveTo.HasValue &&
                        pr.EffectiveTo < cutoffDate &&
                        pr.IsActive &&
                        !pr.IsDeleted)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<PricingRule>> GetRulesNearingExpiryAsync(DateTime warningDate, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting pricing rules expiring before {WarningDate}", warningDate);

        return await Context.PricingRules
            .Where(pr => pr.EffectiveTo.HasValue &&
                        pr.EffectiveTo <= warningDate &&
                        pr.EffectiveTo >= DateTime.UtcNow &&
                        pr.IsActive &&
                        !pr.IsDeleted)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetUsageCountAsync(Guid pricingRuleId, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting usage count for pricing rule {PricingRuleId}", pricingRuleId);

        // For now, return the current uses from the pricing rule entity
        // In a full implementation, this would query a separate usage tracking table
        var pricingRule = await GetByIdAsync(pricingRuleId, cancellationToken);
        return pricingRule?.CurrentUses ?? 0;
    }

    public async Task<bool> HasConflictingRulesAsync(Guid eventId, Domain.Enums.PricingRuleType type, DateTime effectiveFrom, DateTime? effectiveTo, Guid? excludeRuleId = null, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Checking for conflicting pricing rules for event {EventId}", eventId);

        var query = Context.PricingRules
            .Where(pr => pr.EventId == eventId && 
                        pr.Type == type && 
                        pr.IsActive && 
                        !pr.IsDeleted);

        if (excludeRuleId.HasValue)
        {
            query = query.Where(pr => pr.Id != excludeRuleId.Value);
        }

        // Check for overlapping date ranges
        if (effectiveTo.HasValue)
        {
            query = query.Where(pr => 
                (pr.EffectiveFrom <= effectiveTo && (pr.EffectiveTo == null || pr.EffectiveTo >= effectiveFrom)));
        }
        else
        {
            query = query.Where(pr => 
                (pr.EffectiveTo == null || pr.EffectiveTo >= effectiveFrom));
        }

        return await query.AnyAsync(cancellationToken);
    }

    public async Task BulkUpdateUsageAsync(Dictionary<Guid, int> usageUpdates, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Bulk updating usage for {Count} pricing rules", usageUpdates.Count);

        var ruleIds = usageUpdates.Keys.ToList();
        var rules = await Context.PricingRules
            .Where(pr => ruleIds.Contains(pr.Id))
            .ToListAsync(cancellationToken);

        foreach (var rule in rules)
        {
            if (usageUpdates.TryGetValue(rule.Id, out var newUsageCount))
            {
                // This would typically be handled through domain methods
                // For now, we'll update directly (not recommended in production)
                rule.GetType().GetProperty("CurrentUses")?.SetValue(rule, newUsageCount);
            }
        }

        await Context.SaveChangesAsync(cancellationToken);
    }

    public async Task<(IEnumerable<PricingRule> Rules, int TotalCount)> GetPagedByEventAsync(
        Guid eventId,
        int pageNumber,
        int pageSize,
        string? type = null,
        bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting paged pricing rules for event {EventId}, page {PageNumber}, size {PageSize}",
            eventId, pageNumber, pageSize);

        var query = Context.PricingRules
            .Where(pr => pr.EventId == eventId && !pr.IsDeleted);

        // Apply filters
        if (!string.IsNullOrEmpty(type) && Enum.TryParse<Domain.Enums.PricingRuleType>(type, true, out var ruleType))
        {
            query = query.Where(pr => pr.Type == ruleType);
        }

        if (isActive.HasValue)
        {
            query = query.Where(pr => pr.IsActive == isActive.Value);
        }

        // Get total count
        var totalCount = await query.CountAsync(cancellationToken);

        // Apply paging and ordering
        var rules = await query
            .OrderBy(pr => pr.Priority)
            .ThenBy(pr => pr.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (rules, totalCount);
    }
}
