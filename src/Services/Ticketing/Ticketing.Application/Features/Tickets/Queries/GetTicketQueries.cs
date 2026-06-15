using MediatR;
using Ticketing.Application.Common;
using Ticketing.Application.DTOs;
using Ticketing.Application.Interfaces;

namespace Ticketing.Application.Features.Tickets.Queries;

public sealed record GetTicketQuery(Guid TicketId) : IRequest<Result<TicketDto>>;

public sealed record GetUserTicketsQuery(Guid UserId) : IRequest<Result<IReadOnlyCollection<TicketDto>>>;

public sealed class GetTicketQueryHandler : IRequestHandler<GetTicketQuery, Result<TicketDto>>
{
    private readonly ITicketingRepository _repository;

    public GetTicketQueryHandler(ITicketingRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<TicketDto>> Handle(GetTicketQuery request, CancellationToken cancellationToken)
    {
        var ticket = await _repository.GetTicketByIdAsync(request.TicketId, cancellationToken);
        return ticket is null
            ? Result<TicketDto>.Failure("Ticket not found")
            : Result<TicketDto>.Success(ticket.ToDto());
    }
}

public sealed class GetUserTicketsQueryHandler : IRequestHandler<GetUserTicketsQuery, Result<IReadOnlyCollection<TicketDto>>>
{
    private readonly ITicketingRepository _repository;

    public GetUserTicketsQueryHandler(ITicketingRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<IReadOnlyCollection<TicketDto>>> Handle(GetUserTicketsQuery request, CancellationToken cancellationToken)
    {
        var tickets = await _repository.GetTicketsByUserAsync(request.UserId, cancellationToken);
        return Result<IReadOnlyCollection<TicketDto>>.Success(tickets.Select(TicketingMappings.ToDto).ToList());
    }
}
