using Identity.Domain.Entities;

namespace Identity.Domain.Repositories;

public interface IAuditLogRepository
{
    Task<AuditLog?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<AuditLog>> GetByUserIdAsync(Guid userId, int pageSize = 50, int pageNumber = 1, CancellationToken cancellationToken = default);
    Task<IEnumerable<AuditLog>> GetByActionAsync(string action, int pageSize = 50, int pageNumber = 1, CancellationToken cancellationToken = default);
    Task<IEnumerable<AuditLog>> GetByDateRangeAsync(DateTime from, DateTime to, int pageSize = 50, int pageNumber = 1, CancellationToken cancellationToken = default);
    Task<IEnumerable<AuditLog>> GetFailedAttemptsAsync(TimeSpan timeWindow, int pageSize = 50, int pageNumber = 1, CancellationToken cancellationToken = default);
    Task<IEnumerable<AuditLog>> GetSuspiciousActivitiesAsync(int pageSize = 50, int pageNumber = 1, CancellationToken cancellationToken = default);
    Task AddAsync(AuditLog auditLog, CancellationToken cancellationToken = default);
    Task AddRangeAsync(IEnumerable<AuditLog> auditLogs, CancellationToken cancellationToken = default);
    Task<long> GetTotalCountAsync(CancellationToken cancellationToken = default);
    Task<long> GetCountByUserAsync(Guid userId, CancellationToken cancellationToken = default);
}
