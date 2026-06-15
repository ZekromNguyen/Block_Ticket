using System.Text.Json;
using Shared.Contracts.Commands;
using Shared.Contracts.Events;
using Xunit;

namespace Shared.Contracts.Tests;

public sealed class ContractSerializationTests
{
    [Fact]
    public void TicketPurchased_RoundTrips_WithRequiredFields()
    {
        var message = new TicketPurchased(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 125m, DateTime.UtcNow);

        var copy = RoundTrip(message);

        Assert.Equal(message.TicketId, copy.TicketId);
        Assert.Equal(message.EventId, copy.EventId);
        Assert.Equal(message.UserId, copy.UserId);
        Assert.Equal(message.Price, copy.Price);
    }

    [Fact]
    public void MintTicketCommand_RoundTrips_WithMetadata()
    {
        var message = new MintTicketCommand(Guid.NewGuid(), Guid.NewGuid(), "wallet", 99m, "{\"ticketNumber\":\"TKT\"}");

        var copy = RoundTrip(message);

        Assert.Equal(message.TicketId, copy.TicketId);
        Assert.Equal(message.UserWalletAddress, copy.UserWalletAddress);
        Assert.Equal(message.TicketMetadata, copy.TicketMetadata);
    }

    [Fact]
    public void TicketMinted_RoundTrips_WithOptionalCompatibilityFields()
    {
        var message = new TicketMinted(Guid.NewGuid(), "0xtx", "123", DateTime.UtcNow, "0xcontract", true, null);

        var copy = RoundTrip(message);

        Assert.Equal(message.TicketId, copy.TicketId);
        Assert.True(copy.Success);
        Assert.Equal("0xcontract", copy.ContractAddress);
    }

    [Fact]
    public void TicketMintFailed_RoundTrips_WithReason()
    {
        var message = new TicketMintFailed(Guid.NewGuid(), "RPC failed", DateTime.UtcNow);

        var copy = RoundTrip(message);

        Assert.Equal(message.TicketId, copy.TicketId);
        Assert.Equal(message.Reason, copy.Reason);
    }

    private static T RoundTrip<T>(T message)
    {
        var json = JsonSerializer.Serialize(message);
        var copy = JsonSerializer.Deserialize<T>(json);
        Assert.NotNull(copy);
        return copy!;
    }
}
