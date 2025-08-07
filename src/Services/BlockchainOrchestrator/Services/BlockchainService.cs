using System;
using System.Numerics;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using Nethereum.Contracts;
using Nethereum.Hex.HexTypes;
using Nethereum.RPC.Eth.DTOs;

namespace BlockchainOrchestrator.Worker;


public class BlockchainService : IBlockchainService
{
    private readonly ILogger<BlockchainService> _logger;
    private readonly Web3 _web3;
    private readonly Account _account;
    private readonly string _contractAddress;
    private readonly Contract _ticketContract;

    // Smart contract ABI for ticket NFT operations
    private const string CONTRACT_ABI = @"[
        {
            ""constant"": false,
            ""inputs"": [
                {""name"": ""to"", ""type"": ""address""},
                {""name"": ""tokenId"", ""type"": ""uint256""},
                {""name"": ""uri"", ""type"": ""string""}
            ],
            ""name"": ""mintTicket"",
            ""outputs"": [],
            ""stateMutability"": ""nonpayable"",
            ""type"": ""function""
        },
        {
            ""constant"": false,
            ""inputs"": [
                {""name"": ""tokenId"", ""type"": ""uint256""}
            ],
            ""name"": ""burn"",
            ""outputs"": [],
            ""stateMutability"": ""nonpayable"",
            ""type"": ""function""
        },
        {
            ""constant"": true,
            ""inputs"": [
                {""name"": ""tokenId"", ""type"": ""uint256""}
            ],
            ""name"": ""ownerOf"",
            ""outputs"": [
                {""name"": """", ""type"": ""address""}
            ],
            ""stateMutability"": ""view"",
            ""type"": ""function""
        },
        {
            ""constant"": true,
            ""inputs"": [
                {""name"": ""tokenId"", ""type"": ""uint256""}
            ],
            ""name"": ""tokenURI"",
            ""outputs"": [
                {""name"": """", ""type"": ""string""}
            ],
            ""stateMutability"": ""view"",
            ""type"": ""function""
        },
        {
            ""constant"": false,
            ""inputs"": [
                {""name"": ""from"", ""type"": ""address""},
                {""name"": ""to"", ""type"": ""address""},
                {""name"": ""tokenId"", ""type"": ""uint256""}
            ],
            ""name"": ""transferFrom"",
            ""outputs"": [],
            ""stateMutability"": ""nonpayable"",
            ""type"": ""function""
        },
        {
            ""constant"": true,
            ""inputs"": [
                {""name"": ""tokenId"", ""type"": ""uint256""}
            ],
            ""name"": ""exists"",
            ""outputs"": [
                {""name"": """", ""type"": ""bool""}
            ],
            ""stateMutability"": ""view"",
            ""type"": ""function""
        },
        {
            ""constant"": true,
            ""inputs"": [
                {""name"": ""tokenId"", ""type"": ""uint256""}
            ],
            ""name"": ""isTicketUsed"",
            ""outputs"": [
                {""name"": """", ""type"": ""bool""}
            ],
            ""stateMutability"": ""view"",
            ""type"": ""function""
        },
        {
            ""constant"": false,
            ""inputs"": [
                {""name"": ""tokenId"", ""type"": ""uint256""}
            ],
            ""name"": ""markAsUsed"",
            ""outputs"": [],
            ""stateMutability"": ""nonpayable"",
            ""type"": ""function""
        }
    ]";

    public BlockchainService(IConfiguration configuration, ILogger<BlockchainService> logger)
    {
        _logger = logger;

        // Load blockchain configuration
        var rpcUrl = configuration["Blockchain:RpcUrl"] ?? 
            throw new ArgumentException("Blockchain:RpcUrl configuration is required");
        
        var privateKey = configuration["Blockchain:PrivateKey"] ?? 
            throw new ArgumentException("Blockchain:PrivateKey configuration is required");
        
        _contractAddress = configuration["Blockchain:ContractAddress"] ?? 
            throw new ArgumentException("Blockchain:ContractAddress configuration is required");

        // Initialize Web3 with account for signing transactions
        _account = new Account(privateKey);
        _web3 = new Web3(_account, rpcUrl);

        // Initialize smart contract
        _ticketContract = _web3.Eth.GetContract(CONTRACT_ABI, _contractAddress);

        _logger.LogInformation("BlockchainService initialized with RPC: {RpcUrl}, Contract: {ContractAddress}", 
            rpcUrl, _contractAddress);
    }

    public async Task<string> MintTicketAsync(Guid ticketId, string userWalletAddress, string metadata)
    {
        try
        {
            _logger.LogInformation("Minting ticket {TicketId} for user {UserWallet}", ticketId, userWalletAddress);

            var mintFunction = _ticketContract.GetFunction("mintTicket");
            
            // Convert GUID to BigInteger for blockchain token ID
            var tokenId = ConvertGuidToBigInteger(ticketId);

            // Estimate gas for the transaction
            var gasEstimate = await mintFunction.EstimateGasAsync(
                _account.Address, 
                null, 
                null, 
                userWalletAddress, 
                tokenId, 
                metadata);

            // Send transaction and wait for receipt
            var receipt = await mintFunction.SendTransactionAndWaitForReceiptAsync(
                _account.Address,
                gasEstimate,
                null,
                null,
                userWalletAddress,
                tokenId,
                metadata);

            if (receipt.Status.Value == 1)
            {
                _logger.LogInformation("Successfully minted ticket {TicketId} with transaction hash: {TxHash}", 
                    ticketId, receipt.TransactionHash);
                return receipt.TransactionHash;
            }
            else
            {
                _logger.LogError("Failed to mint ticket {TicketId}. Transaction failed with status: {Status}", 
                    ticketId, receipt.Status.Value);
                throw new Exception($"Minting transaction failed with status: {receipt.Status.Value}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error minting ticket {TicketId} for user {UserWallet}", ticketId, userWalletAddress);
            throw;
        }
    }

    public async Task<string> BurnTicketAsync(string tokenId)
    {
        try
        {
            _logger.LogInformation("Burning ticket with token ID {TokenId}", tokenId);

            var burnFunction = _ticketContract.GetFunction("burn");
            var numericTokenId = BigInteger.Parse(tokenId);

            var gasEstimate = await burnFunction.EstimateGasAsync(_account.Address, null, null, numericTokenId);
            var receipt = await burnFunction.SendTransactionAndWaitForReceiptAsync(
                _account.Address,
                gasEstimate,
                null,
                null,
                numericTokenId);

            if (receipt.Status.Value == 1)
            {
                _logger.LogInformation("Successfully burned ticket {TokenId} with transaction hash: {TxHash}", 
                    tokenId, receipt.TransactionHash);
                return receipt.TransactionHash;
            }
            else
            {
                _logger.LogError("Failed to burn ticket {TokenId}. Transaction failed with status: {Status}", 
                    tokenId, receipt.Status.Value);
                throw new Exception($"Burn transaction failed with status: {receipt.Status.Value}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error burning ticket {TokenId}", tokenId);
            throw;
        }
    }

    public async Task<bool> VerifyTicketAsync(string tokenId)
    {
        try
        {
            _logger.LogDebug("Verifying ticket {TokenId}", tokenId);

            var existsFunction = _ticketContract.GetFunction("exists");
            var numericTokenId = BigInteger.Parse(tokenId);
            
            var exists = await existsFunction.CallAsync<bool>(numericTokenId);
            
            _logger.LogDebug("Ticket {TokenId} verification result: {Exists}", tokenId, exists);
            return exists;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error verifying ticket {TokenId}. Treating as invalid.", tokenId);
            return false;
        }
    }

    public async Task<string> GetTicketOwnerAsync(string tokenId)
    {
        try
        {
            _logger.LogDebug("Getting owner of ticket {TokenId}", tokenId);

            var ownerFunction = _ticketContract.GetFunction("ownerOf");
            var numericTokenId = BigInteger.Parse(tokenId);
            
            var owner = await ownerFunction.CallAsync<string>(numericTokenId);
            
            _logger.LogDebug("Ticket {TokenId} owner: {Owner}", tokenId, owner);
            return owner;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting owner of ticket {TokenId}", tokenId);
            throw;
        }
    }

    public async Task<string> TransferTicketAsync(string from, string to, string tokenId)
    {
        try
        {
            _logger.LogInformation("Transferring ticket {TokenId} from {From} to {To}", tokenId, from, to);

            var transferFunction = _ticketContract.GetFunction("transferFrom");
            var numericTokenId = BigInteger.Parse(tokenId);

            var gasEstimate = await transferFunction.EstimateGasAsync(
                _account.Address, null, null, from, to, numericTokenId);
            
            var receipt = await transferFunction.SendTransactionAndWaitForReceiptAsync(
                _account.Address,
                gasEstimate,
                null,
                null,
                from,
                to,
                numericTokenId);

            if (receipt.Status.Value == 1)
            {
                _logger.LogInformation("Successfully transferred ticket {TokenId} with transaction hash: {TxHash}", 
                    tokenId, receipt.TransactionHash);
                return receipt.TransactionHash;
            }
            else
            {
                _logger.LogError("Failed to transfer ticket {TokenId}. Transaction failed with status: {Status}", 
                    tokenId, receipt.Status.Value);
                throw new Exception($"Transfer transaction failed with status: {receipt.Status.Value}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error transferring ticket {TokenId} from {From} to {To}", tokenId, from, to);
            throw;
        }
    }

    public async Task<string> GetTicketMetadataAsync(string tokenId)
    {
        try
        {
            _logger.LogDebug("Getting metadata for ticket {TokenId}", tokenId);

            var tokenUriFunction = _ticketContract.GetFunction("tokenURI");
            var numericTokenId = BigInteger.Parse(tokenId);
            
            var metadataUri = await tokenUriFunction.CallAsync<string>(numericTokenId);
            
            _logger.LogDebug("Ticket {TokenId} metadata URI: {MetadataUri}", tokenId, metadataUri);
            return metadataUri;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting metadata for ticket {TokenId}", tokenId);
            throw;
        }
    }

    public async Task<bool> IsTicketUsedAsync(string tokenId)
    {
        try
        {
            _logger.LogDebug("Checking if ticket {TokenId} is used", tokenId);

            var isUsedFunction = _ticketContract.GetFunction("isTicketUsed");
            var numericTokenId = BigInteger.Parse(tokenId);
            
            var isUsed = await isUsedFunction.CallAsync<bool>(numericTokenId);
            
            _logger.LogDebug("Ticket {TokenId} used status: {IsUsed}", tokenId, isUsed);
            return isUsed;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error checking if ticket {TokenId} is used. Treating as not used.", tokenId);
            return false;
        }
    }

    public async Task<string> MarkTicketAsUsedAsync(string tokenId)
    {
        try
        {
            _logger.LogInformation("Marking ticket {TokenId} as used", tokenId);

            var markAsUsedFunction = _ticketContract.GetFunction("markAsUsed");
            var numericTokenId = BigInteger.Parse(tokenId);

            var gasEstimate = await markAsUsedFunction.EstimateGasAsync(_account.Address, null, null, numericTokenId);
            var receipt = await markAsUsedFunction.SendTransactionAndWaitForReceiptAsync(
                _account.Address,
                gasEstimate,
                null,
                null,
                numericTokenId);

            if (receipt.Status.Value == 1)
            {
                _logger.LogInformation("Successfully marked ticket {TokenId} as used with transaction hash: {TxHash}", 
                    tokenId, receipt.TransactionHash);
                return receipt.TransactionHash;
            }
            else
            {
                _logger.LogError("Failed to mark ticket {TokenId} as used. Transaction failed with status: {Status}", 
                    tokenId, receipt.Status.Value);
                throw new Exception($"Mark as used transaction failed with status: {receipt.Status.Value}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking ticket {TokenId} as used", tokenId);
            throw;
        }
    }

    /// <summary>
    /// Converts a GUID to a BigInteger for use as blockchain token ID
    /// </summary>
    private static BigInteger ConvertGuidToBigInteger(Guid guid)
    {
        var bytes = guid.ToByteArray();
        
        // Ensure positive number by padding with zero byte if necessary
        if (bytes[bytes.Length - 1] >= 128)
        {
            var paddedBytes = new byte[bytes.Length + 1];
            Array.Copy(bytes, paddedBytes, bytes.Length);
            bytes = paddedBytes;
        }
        
        return new BigInteger(bytes);
    }
}
