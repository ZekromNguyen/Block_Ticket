using MediatR;
using Ticketing.Application.Common;
using Ticketing.Application.DTOs;
using Ticketing.Application.Interfaces;
using Ticketing.Domain.Entities;

namespace Ticketing.Application.Features.WaitingList.Commands;

public sealed record JoinWaitingListCommand(WaitingListJoinRequest Request) : IRequest<Result<WaitingListEntryDto>>;

public sealed record LeaveWaitingListCommand(Guid UserId, Guid EventId, Guid TicketTypeId) : IRequest<Result<WaitingListEntryDto>>;

public sealed record CreateWaitingListOfferCommand(WaitingListOfferRequest Request) : IRequest<Result<WaitingListEntryDto>>;

public sealed class JoinWaitingListCommandHandler : IRequestHandler<JoinWaitingListCommand, Result<WaitingListEntryDto>>
{
    private readonly ITicketingRepository _repository;

    public JoinWaitingListCommandHandler(ITicketingRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<WaitingListEntryDto>> Handle(JoinWaitingListCommand command, CancellationToken cancellationToken)
    {
        var request = command.Request;
        var existing = await _repository.GetWaitingListEntryAsync(request.UserId, request.EventId, request.TicketTypeId, cancellationToken);
        if (existing is not null && existing.Status is WaitingListStatus.Waiting or WaitingListStatus.Offered)
        {
            return Result<WaitingListEntryDto>.Success(existing.ToDto());
        }

        var entry = new WaitingListEntry(request.UserId, request.EventId, request.TicketTypeId);
        await _repository.AddWaitingListEntryAsync(entry, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result<WaitingListEntryDto>.Success(entry.ToDto());
    }
}

public sealed class LeaveWaitingListCommandHandler : IRequestHandler<LeaveWaitingListCommand, Result<WaitingListEntryDto>>
{
    private readonly ITicketingRepository _repository;

    public LeaveWaitingListCommandHandler(ITicketingRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<WaitingListEntryDto>> Handle(LeaveWaitingListCommand command, CancellationToken cancellationToken)
    {
        var entry = await _repository.GetWaitingListEntryAsync(command.UserId, command.EventId, command.TicketTypeId, cancellationToken);
        if (entry is null)
        {
            return Result<WaitingListEntryDto>.Failure("Waiting list entry not found");
        }

        entry.Leave();
        await _repository.SaveChangesAsync(cancellationToken);
        return Result<WaitingListEntryDto>.Success(entry.ToDto());
    }
}

public sealed class CreateWaitingListOfferCommandHandler : IRequestHandler<CreateWaitingListOfferCommand, Result<WaitingListEntryDto>>
{
    private readonly ITicketingRepository _repository;
    private readonly ITicketEventPublisher _publisher;

    public CreateWaitingListOfferCommandHandler(ITicketingRepository repository, ITicketEventPublisher publisher)
    {
        _repository = repository;
        _publisher = publisher;
    }

    public async Task<Result<WaitingListEntryDto>> Handle(CreateWaitingListOfferCommand command, CancellationToken cancellationToken)
    {
        var request = command.Request;
        var entries = await _repository.GetWaitingListEntriesAsync(request.EventId, request.TicketTypeId, cancellationToken);
        var next = entries.FirstOrDefault(entry => entry.Status == WaitingListStatus.Waiting);
        if (next is null)
        {
            return Result<WaitingListEntryDto>.Failure("No waiting users for this ticket type");
        }

        next.CreateOffer(DateTime.UtcNow.Add(request.OfferTtl));
        await _repository.SaveChangesAsync(cancellationToken);
        await _publisher.PublishWaitingListOfferAsync(next, cancellationToken);

        return Result<WaitingListEntryDto>.Success(next.ToDto());
    }
}
