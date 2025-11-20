using Event.Application.Common.Models;
using Event.Domain.Enums;
using MediatR;

namespace Event.Application.Features.TicketTypes.Commands.CreateTicketType;

public record CreateTicketTypeCommand : IRequest<TicketTypeDto>
{
    public Guid EventId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
    public string? Description { get; init; }
    public InventoryType InventoryType { get; init; }
    public MoneyDto BasePrice { get; init; } = new();
    public int MinPurchaseQuantity { get; init; } = 1;
    public int MaxPurchaseQuantity { get; init; } = 10;
    public int? MaxPerCustomer { get; init; }
    public bool IsVisible { get; init; } = true;

        public static CreateTicketTypeCommand FromRequest(Guid eventId, CreateTicketTypeCommandRequest request)
    {
        return new CreateTicketTypeCommand
        {
            EventId = eventId,
            Name = request.Name,
            Code = request.Code,
            Description = request.Description,
            InventoryType = request.InventoryType,
            BasePrice = request.BasePrice,
            MinPurchaseQuantity = request.MinPurchaseQuantity,
            MaxPurchaseQuantity = request.MaxPurchaseQuantity,
            MaxPerCustomer = request.MaxPerCustomer,
            IsVisible = request.IsVisible
        };
    }
}

