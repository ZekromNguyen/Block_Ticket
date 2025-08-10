using Identity.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace Identity.Infrastructure.Persistence;

/// <summary>
/// Unit of Work implementation for managing database transactions
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly IdentityDbContext _context;
    private readonly ILogger<UnitOfWork> _logger;

    public UnitOfWork(IdentityDbContext context, ILogger<UnitOfWork> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Saves all changes made in the current unit of work to the database
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The number of state entries written to the database</returns>
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _context.SaveChangesAsync(cancellationToken);
            _logger.LogDebug("Successfully saved {Count} changes to database", result);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving changes to database");
            throw;
        }
    }
}
