using Event.Domain.Interfaces;
using Event.Domain.Models;
using Event.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Event.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for approval workflow templates
/// </summary>
public class ApprovalWorkflowTemplateRepository : IApprovalWorkflowTemplateRepository
{
    private readonly EventDbContext _context;

    public ApprovalWorkflowTemplateRepository(EventDbContext context)
    {
        _context = context;
    }

    public async Task<ApprovalWorkflowTemplate> UpsertAsync(
        ApprovalWorkflowTemplate template,
        CancellationToken cancellationToken = default)
    {
        var existing = await _context.ApprovalWorkflowTemplates
            .FirstOrDefaultAsync(t => t.Id == template.Id, cancellationToken);

        if (existing != null)
        {
            // Update existing
            existing.Name = template.Name;
            existing.Description = template.Description;
            existing.RequiredApprovals = template.RequiredApprovals;
            existing.RequiredRoles = template.RequiredRoles;
            existing.DefaultRiskLevel = template.DefaultRiskLevel;
            existing.DefaultExpirationTime = template.DefaultExpirationTime;
            existing.IsActive = template.IsActive;
            existing.UpdatedAt = DateTime.UtcNow;
            existing.AutoApprovalConditions = template.AutoApprovalConditions;
            existing.EscalationRules = template.EscalationRules;

            _context.ApprovalWorkflowTemplates.Update(existing);
            await _context.SaveChangesAsync(cancellationToken);
            return existing;
        }
        else
        {
            // Create new
            template.CreatedAt = DateTime.UtcNow;
            template.UpdatedAt = DateTime.UtcNow;
            
            _context.ApprovalWorkflowTemplates.Add(template);
            await _context.SaveChangesAsync(cancellationToken);
            return template;
        }
    }

    public async Task<ApprovalWorkflowTemplate?> GetByOperationTypeAsync(
        ApprovalOperationType operationType,
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        return await _context.ApprovalWorkflowTemplates
            .FirstOrDefaultAsync(t => t.OperationType == operationType && 
                                    t.OrganizationId == organizationId && 
                                    t.IsActive, 
                               cancellationToken);
    }

    public async Task<List<ApprovalWorkflowTemplate>> GetByOrganizationAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        return await _context.ApprovalWorkflowTemplates
            .Where(t => t.OrganizationId == organizationId)
            .OrderBy(t => t.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<ApprovalWorkflowTemplate?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _context.ApprovalWorkflowTemplates
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var template = await GetByIdAsync(id, cancellationToken);
        if (template != null)
        {
            _context.ApprovalWorkflowTemplates.Remove(template);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<List<ApprovalWorkflowTemplate>> GetActiveTemplatesAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        return await _context.ApprovalWorkflowTemplates
            .Where(t => t.OrganizationId == organizationId && t.IsActive)
            .OrderBy(t => t.Name)
            .ToListAsync(cancellationToken);
    }
}
