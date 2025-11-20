using Identity.Domain.Entities;
using Shared.Common.Models;


namespace Identity.Domain.Repositories;

public interface IPermissionRepository : IRepository<Permission>
{
    Task<Permission?> GetPermissionByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<List<Permission>> GetPermissionsByNamesAsync(IEnumerable<string> names, CancellationToken cancellationToken = default);
}

