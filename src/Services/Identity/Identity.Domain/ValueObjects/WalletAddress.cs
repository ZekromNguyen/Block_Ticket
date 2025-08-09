using System.Text.RegularExpressions;

namespace Identity.Domain.ValueObjects;

public record WalletAddress
{
    private static readonly Regex EthereumAddressRegex = new(
        @"^0x[a-fA-F0-9]{40}$",
        RegexOptions.Compiled);

    public string Value { get; }

    public WalletAddress(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Wallet address cannot be null or empty", nameof(value));

        value = value.Trim();

        if (!EthereumAddressRegex.IsMatch(value))
            throw new ArgumentException("Invalid Ethereum wallet address format", nameof(value));

        Value = value.ToLowerInvariant();
    }

    public static implicit operator string(WalletAddress address) => address.Value;
    public static implicit operator WalletAddress(string address) => new(address);

    public override string ToString() => Value;
}
