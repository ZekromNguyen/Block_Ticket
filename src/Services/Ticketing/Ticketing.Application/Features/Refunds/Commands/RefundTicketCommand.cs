using MediatR;
using Ticketing.Application.Common;
using Ticketing.Application.DTOs;
using Ticketing.Application.Interfaces;

namespace Ticketing.Application.Features.Refunds.Commands;

public sealed record RefundTicketCommand(RefundTicketRequest Request) : IRequest<Result<TicketDto>>;

public sealed class RefundTicketCommandHandler : IRequestHandler<RefundTicketCommand, Result<TicketDto>>
{
    private readonly ITicketingRepository _repository;
    private readonly IPaymentProvider _paymentProvider;
    private readonly ITicketEventPublisher _publisher;

    public RefundTicketCommandHandler(ITicketingRepository repository, IPaymentProvider paymentProvider, ITicketEventPublisher publisher)
    {
        _repository = repository;
        _paymentProvider = paymentProvider;
        _publisher = publisher;
    }

    public async Task<Result<TicketDto>> Handle(RefundTicketCommand command, CancellationToken cancellationToken)
    {
        var request = command.Request;
        var ticket = await _repository.GetTicketByIdAsync(request.TicketId, cancellationToken);
        if (ticket is null)
        {
            return Result<TicketDto>.Failure("Ticket not found");
        }

        var refund = await _paymentProvider.RefundPaymentAsync(ticket.Id, ticket.PricePaid, request.Reason, cancellationToken);
        if (!refund.Succeeded)
        {
            return Result<TicketDto>.Failure(refund.Error ?? "Refund failed");
        }

        ticket.Refund(ticket.PricePaid, request.Reason);
        await _repository.SaveChangesAsync(cancellationToken);
        await _publisher.PublishTicketRefundedAsync(ticket, ticket.PricePaid, request.Reason, cancellationToken);
        await _publisher.PublishTicketsRestockedAsync(ticket, request.Reason, cancellationToken);

        if (!string.IsNullOrWhiteSpace(request.UserWalletAddress))
        {
            await _publisher.PublishBurnTicketAsync(ticket, request.UserWalletAddress, request.Reason, cancellationToken);
        }

        return Result<TicketDto>.Success(ticket.ToDto());
    }
}
