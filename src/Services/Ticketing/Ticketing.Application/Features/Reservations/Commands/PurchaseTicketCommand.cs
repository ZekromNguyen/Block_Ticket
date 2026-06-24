using MediatR;
using Ticketing.Application.Common;
using Ticketing.Application.DTOs;
using Ticketing.Application.Interfaces;

namespace Ticketing.Application.Features.Reservations.Commands;

public sealed record PurchaseTicketCommand(PurchaseTicketRequest Request) : IRequest<Result<PurchaseResponse>>;

public sealed class PurchaseTicketCommandHandler : IRequestHandler<PurchaseTicketCommand, Result<PurchaseResponse>>
{
    private readonly IMediator _mediator;
    private readonly ICurrencyPolicyService _currencyPolicy;

    public PurchaseTicketCommandHandler(IMediator mediator, ICurrencyPolicyService currencyPolicy)
    {
        _mediator = mediator;
        _currencyPolicy = currencyPolicy;
    }

    public async Task<Result<PurchaseResponse>> Handle(PurchaseTicketCommand command, CancellationToken cancellationToken)
    {
        var request = command.Request;
        var ticketTypeId = request.TicketTypeId.GetValueOrDefault(Guid.Parse("11111111-1111-1111-1111-111111111111"));

        var currency = request.Currency ?? "USD";
        var currencyCheck = await _currencyPolicy.ValidateAsync(request.EventId, currency, cancellationToken);
        if (!currencyCheck.Allowed)
        {
            return Result<PurchaseResponse>.Failure(currencyCheck.Reason ?? "Currency not allowed for this event");
        }
        currency = currencyCheck.Policy?.DefaultCurrency ?? currency;

        var item = new ReservationItemRequest(
            ticketTypeId,
            request.TicketTypeName ?? "General Admission",
            request.Price,
            1);

        var createResult = await _mediator.Send(
            new CreateReservationCommand(new CreateReservationRequest(
                request.UserId,
                request.EventId,
                currency,
                new[] { item },
                request.IdempotencyKey)),
            cancellationToken);

        if (!createResult.Succeeded || createResult.Value is null)
        {
            return Result<PurchaseResponse>.Failure(createResult.Error ?? "Unable to create reservation");
        }

        var confirmResult = await _mediator.Send(
            new ConfirmReservationCommand(new ConfirmReservationRequest(
                createResult.Value.Id,
                request.PaymentMethod,
                request.UserWalletAddress)),
            cancellationToken);

        if (!confirmResult.Succeeded || confirmResult.Value is null)
        {
            return Result<PurchaseResponse>.Failure(confirmResult.Error ?? "Unable to confirm reservation");
        }

        var ticket = confirmResult.Value.Tickets.First();
        return Result<PurchaseResponse>.Success(new PurchaseResponse
        {
            TicketId = ticket.Id,
            TicketNumber = ticket.TicketNumber,
            Status = ticket.Status.ToString(),
            TransactionId = confirmResult.Value.Reservation.PaymentIntentId,
            ReservationId = confirmResult.Value.Reservation.Id
        });
    }
}
