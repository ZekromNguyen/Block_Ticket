using MediatR;
using Ticketing.Application.Common;
using Ticketing.Application.DTOs;
using Ticketing.Application.Interfaces;
using Ticketing.Domain.Entities;

namespace Ticketing.Application.Features.Resale.Commands;

public sealed record ListTicketForResaleCommand(ResaleListTicketRequest Request) : IRequest<Result<TicketDto>>;

public sealed record PurchaseResaleTicketCommand(ResalePurchaseTicketRequest Request) : IRequest<Result<TicketDto>>;

public sealed record CancelResaleListingCommand(CancelResaleRequest Request) : IRequest<Result<TicketDto>>;

public sealed record GetResaleListingsQuery(Guid? EventId) : IRequest<Result<IReadOnlyCollection<TicketDto>>>;

public sealed class ListTicketForResaleCommandHandler : IRequestHandler<ListTicketForResaleCommand, Result<TicketDto>>
{
    private readonly ITicketingRepository _repository;
    private readonly ITicketEventPublisher _publisher;
    private readonly ITicketResalePolicy _resalePolicy;

    public ListTicketForResaleCommandHandler(ITicketingRepository repository, ITicketEventPublisher publisher, ITicketResalePolicy resalePolicy)
    {
        _repository = repository;
        _publisher = publisher;
        _resalePolicy = resalePolicy;
    }

    public async Task<Result<TicketDto>> Handle(ListTicketForResaleCommand command, CancellationToken cancellationToken)
    {
        var request = command.Request;
        var ticket = await _repository.GetTicketByIdAsync(request.TicketId, cancellationToken);
        if (ticket is null)
        {
            return Result<TicketDto>.Failure("Ticket not found");
        }

        if (ticket.UserId != request.SellerUserId)
        {
            return Result<TicketDto>.Failure("Only the current owner can list the ticket");
        }

        var policyCheck = await _resalePolicy.CheckAsync(ticket.EventId, ticket.PricePaid, request.Price, cancellationToken);
        if (!policyCheck.Allowed)
        {
            return Result<TicketDto>.Failure(policyCheck.Reason ?? "Resale not allowed by event policy");
        }

        ticket.ListForResale(request.Price);
        await _repository.SaveChangesAsync(cancellationToken);
        await _publisher.PublishTicketListedForResaleAsync(ticket, request.Price, cancellationToken);

        return Result<TicketDto>.Success(ticket.ToDto());
    }
}

public sealed class PurchaseResaleTicketCommandHandler : IRequestHandler<PurchaseResaleTicketCommand, Result<TicketDto>>
{
    private readonly ITicketingRepository _repository;
    private readonly IPaymentProvider _paymentProvider;
    private readonly ITicketEventPublisher _publisher;

    public PurchaseResaleTicketCommandHandler(ITicketingRepository repository, IPaymentProvider paymentProvider, ITicketEventPublisher publisher)
    {
        _repository = repository;
        _paymentProvider = paymentProvider;
        _publisher = publisher;
    }

    public async Task<Result<TicketDto>> Handle(PurchaseResaleTicketCommand command, CancellationToken cancellationToken)
    {
        var request = command.Request;
        var ticket = await _repository.GetTicketByIdAsync(request.TicketId, cancellationToken);
        if (ticket is null)
        {
            return Result<TicketDto>.Failure("Ticket not found");
        }

        if (ticket.Status != TicketStatus.OnResale || ticket.ResalePrice is null || ticket.ResaleSellerUserId is null)
        {
            return Result<TicketDto>.Failure("Ticket is not listed for resale");
        }

        var sellerUserId = ticket.UserId;
        var resalePrice = ticket.ResalePrice.Value;
        var payment = await _paymentProvider.ConfirmPaymentAsync($"resale_{ticket.Id:N}", resalePrice, "USD", request.PaymentMethod, cancellationToken);
        if (!payment.Succeeded)
        {
            return Result<TicketDto>.Failure(payment.Error ?? "Resale payment failed");
        }

        ticket.TransferTo(request.BuyerUserId);
        await _repository.SaveChangesAsync(cancellationToken);
        await _publisher.PublishTicketTransferredAsync(ticket, sellerUserId, request.BuyerUserId, resalePrice, cancellationToken);

        return Result<TicketDto>.Success(ticket.ToDto());
    }
}

public sealed class CancelResaleListingCommandHandler : IRequestHandler<CancelResaleListingCommand, Result<TicketDto>>
{
    private readonly ITicketingRepository _repository;
    private readonly ITicketEventPublisher _publisher;

    public CancelResaleListingCommandHandler(ITicketingRepository repository, ITicketEventPublisher publisher)
    {
        _repository = repository;
        _publisher = publisher;
    }

    public async Task<Result<TicketDto>> Handle(CancelResaleListingCommand command, CancellationToken cancellationToken)
    {
        var request = command.Request;
        var ticket = await _repository.GetTicketByIdAsync(request.TicketId, cancellationToken);
        if (ticket is null)
        {
            return Result<TicketDto>.Failure("Ticket not found");
        }

        if (ticket.UserId != request.SellerUserId)
        {
            return Result<TicketDto>.Failure("Only the current owner can cancel the listing");
        }

        ticket.CancelResale();
        await _repository.SaveChangesAsync(cancellationToken);
        await _publisher.PublishResaleListingCancelledAsync(ticket, request.Reason, cancellationToken);

        return Result<TicketDto>.Success(ticket.ToDto());
    }
}

public sealed class GetResaleListingsQueryHandler : IRequestHandler<GetResaleListingsQuery, Result<IReadOnlyCollection<TicketDto>>>
{
    private readonly ITicketingRepository _repository;

    public GetResaleListingsQueryHandler(ITicketingRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<IReadOnlyCollection<TicketDto>>> Handle(GetResaleListingsQuery request, CancellationToken cancellationToken)
    {
        var tickets = await _repository.GetResaleTicketsAsync(request.EventId, cancellationToken);
        return Result<IReadOnlyCollection<TicketDto>>.Success(tickets.Select(TicketingMappings.ToDto).ToList());
    }
}
