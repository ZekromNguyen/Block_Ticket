using Event.Application.Common.Models;
using Event.Domain.Enums;

namespace Event.Application.Features.TicketTypes.Commands.CreateTicketType;

public record CreateTicketTypeCommandRequest
{
    public string Name { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
    public string? Description { get; init; }
    public InventoryType InventoryType { get; init; }
    public MoneyDto BasePrice { get; init; } = new();
    public int MinPurchaseQuantity { get; init; } = 1;
    public int MaxPurchaseQuantity { get; init; } = 10;
    public int? MaxPerCustomer { get; init; }
    public bool IsVisible { get; init; } = true;
}

