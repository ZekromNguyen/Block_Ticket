using Identity.Domain.Entities;
using Identity.Domain.Repositories;
using Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure.Repositories;

public class PermissionRepository : Repository<Permission>, IPermissionRepository
{
    public PermissionRepository(IdentityDbContext context) : base(context)
    {
    }

    public async Task<Permission?> GetPermissionByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return await _context.Permissions.FirstOrDefaultAsync(p => p.Name == name, cancellationToken);
    }

    public async Task<List<Permission>> GetPermissionsByNamesAsync(IEnumerable<string> names, CancellationToken cancellationToken = default)
    {
        return await _context.Permissions.Where(p => names.Contains(p.Name)).ToListAsync(cancellationToken);
    }
}

