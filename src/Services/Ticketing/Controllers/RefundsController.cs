using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Common.Models;
using Ticketing.Application.DTOs;
using Ticketing.Application.Features.Refunds.Commands;

namespace Ticketing.Api.Controllers;

[ApiController]
[Route("api/refunds")]
[Authorize]
public sealed class RefundsController : ControllerBase
{
    private readonly IMediator _mediator;

    public RefundsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("tickets/{ticketId:guid}")]
    public async Task<ActionResult<ApiResponse<TicketDto>>> RefundTicket(Guid ticketId, RefundTicketRequest request, CancellationToken cancellationToken)
    {
        var commandRequest = request with { TicketId = ticketId };
        var result = await _mediator.Send(new RefundTicketCommand(commandRequest), cancellationToken);
        if (!result.Succeeded || result.Value is null)
        {
            return BadRequest(ApiResponse<TicketDto>.ErrorResult(result.Error ?? "Unable to refund ticket"));
        }

        return Ok(ApiResponse<TicketDto>.SuccessResult(result.Value, "Ticket refunded"));
    }
}
