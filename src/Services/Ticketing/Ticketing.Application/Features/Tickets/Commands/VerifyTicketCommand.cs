using MediatR;
using Ticketing.Application.Common;
using Ticketing.Application.DTOs;
using Ticketing.Application.Interfaces;
using Ticketing.Domain.Entities;

namespace Ticketing.Application.Features.Tickets.Commands;

public sealed record VerifyTicketCommand(VerifyTicketRequest Request) : IRequest<Result<VerifyTicketResponse>>;

public sealed class VerifyTicketCommandHandler : IRequestHandler<VerifyTicketCommand, Result<VerifyTicketResponse>>
{
    private readonly ITicketingRepository _repository;

    public VerifyTicketCommandHandler(ITicketingRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<VerifyTicketResponse>> Handle(VerifyTicketCommand command, CancellationToken cancellationToken)
    {
        var request = command.Request;
        var ticket = await _repository.GetTicketByIdAsync(request.TicketId, cancellationToken);
        if (ticket is null)
        {
            return Result<VerifyTicketResponse>.Success(new VerifyTicketResponse(request.TicketId, false, "Ticket not found", null));
        }

        if (!ticket.VerificationCode.Equals(request.VerificationCode, StringComparison.OrdinalIgnoreCase))
        {
            return Result<VerifyTicketResponse>.Success(new VerifyTicketResponse(ticket.Id, false, "Invalid verification code", ticket.ToDto()));
        }

        if (ticket.Status == TicketStatus.Used)
        {
            return Result<VerifyTicketResponse>.Success(new VerifyTicketResponse(ticket.Id, false, "Ticket already used", ticket.ToDto()));
        }

        try
        {
            ticket.MarkUsed(request.CheckedBy, request.Location);
            await _repository.SaveChangesAsync(cancellationToken);

            return Result<VerifyTicketResponse>.Success(new VerifyTicketResponse(ticket.Id, true, "Ticket accepted", ticket.ToDto()));
        }
        catch (InvalidOperationException)
        {
            return Result<VerifyTicketResponse>.Success(new VerifyTicketResponse(ticket.Id, false, $"Ticket is {ticket.Status}", ticket.ToDto()));
        }
    }
}
