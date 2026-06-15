using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Common.Models;
using Ticketing.Application.DTOs;
using Ticketing.Application.Features.Reservations.Commands;
using Ticketing.Application.Features.Tickets.Commands;
using Ticketing.Application.Features.Tickets.Queries;

namespace Ticketing.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TicketsController : ControllerBase
{
    private readonly IMediator _mediator;

    public TicketsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("purchase")]
    public async Task<ActionResult<ApiResponse<PurchaseResponse>>> PurchaseTicket(PurchaseTicketRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new PurchaseTicketCommand(request), cancellationToken);
        if (!result.Succeeded || result.Value is null)
        {
            return BadRequest(ApiResponse<PurchaseResponse>.ErrorResult(result.Error ?? "Purchase failed"));
        }

        return Ok(ApiResponse<PurchaseResponse>.SuccessResult(result.Value, "Ticket purchased successfully"));
    }

    [HttpGet("user/{userId:guid}")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<TicketDto>>>> GetUserTickets(Guid userId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetUserTicketsQuery(userId), cancellationToken);
        return Ok(ApiResponse<IReadOnlyCollection<TicketDto>>.SuccessResult(result.Value ?? Array.Empty<TicketDto>()));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<TicketDto>>> GetTicket(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetTicketQuery(id), cancellationToken);
        if (!result.Succeeded || result.Value is null)
        {
            return NotFound(ApiResponse<TicketDto>.ErrorResult(result.Error ?? "Ticket not found"));
        }

        return Ok(ApiResponse<TicketDto>.SuccessResult(result.Value));
    }

    [HttpPost("internal/verify")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<VerifyTicketResponse>>> VerifyTicket(VerifyTicketRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new VerifyTicketCommand(request), cancellationToken);
        if (!result.Succeeded || result.Value is null)
        {
            return BadRequest(ApiResponse<VerifyTicketResponse>.ErrorResult(result.Error ?? "Verification failed"));
        }

        return Ok(ApiResponse<VerifyTicketResponse>.SuccessResult(result.Value));
    }
}
