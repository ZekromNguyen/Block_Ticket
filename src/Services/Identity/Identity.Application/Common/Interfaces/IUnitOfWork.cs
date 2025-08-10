namespace Identity.Application.Common.Interfaces;

/// <summary>
/// Unit of Work pattern interface for managing database transactions
/// </summary>
public interface IUnitOfWork
{
    /// <summary>
    /// Saves all changes made in the current unit of work to the database
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The number of state entries written to the database</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
