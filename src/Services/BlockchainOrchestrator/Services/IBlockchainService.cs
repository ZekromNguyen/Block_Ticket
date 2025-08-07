using System;
using System.Numerics;
using System.Threading.Tasks;

namespace BlockchainOrchestrator.Worker;

public interface IBlockchainService
{
    Task<string> MintTicketAsync(Guid ticketId, string userWalletAddress, string metadata);
    Task<string> BurnTicketAsync(string tokenId);
    Task<bool> VerifyTicketAsync(string tokenId);

    Task<string> GetTicketOwnerAsync(string tokenId);
    Task<string> TransferTicketAsync(string from, string to, string tokenId);

    Task<string> GetTicketMetadataAsync(string tokenId);

    Task<bool> IsTicketUsedAsync(string tokenId);

    Task<string> MarkTicketAsUsedAsync(string tokenId);
}
