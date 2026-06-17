using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Common.Models;
using Ticketing.Application.DTOs;
using Ticketing.Application.Features.Resale.Commands;

namespace Ticketing.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class ResaleController : ControllerBase
{
    private readonly IMediator _mediator;

    public ResaleController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("listings")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<TicketDto>>>> GetListings([FromQuery] Guid? eventId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetResaleListingsQuery(eventId), cancellationToken);
        return Ok(ApiResponse<IReadOnlyCollection<TicketDto>>.SuccessResult(result.Value ?? Array.Empty<TicketDto>()));
    }

    [HttpPost("list")]
    public async Task<ActionResult<ApiResponse<TicketDto>>> ListTicket(ResaleListTicketRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new ListTicketForResaleCommand(request), cancellationToken);
        if (!result.Succeeded || result.Value is null)
        {
            return BadRequest(ApiResponse<TicketDto>.ErrorResult(result.Error ?? "Unable to list ticket"));
        }

        return Ok(ApiResponse<TicketDto>.SuccessResult(result.Value, "Ticket listed for resale"));
    }

    [HttpPost("{ticketId:guid}/purchase")]
    public async Task<ActionResult<ApiResponse<TicketDto>>> Purchase(Guid ticketId, ResalePurchaseTicketRequest request, CancellationToken cancellationToken)
    {
        var commandRequest = request with { TicketId = ticketId };
        var result = await _mediator.Send(new PurchaseResaleTicketCommand(commandRequest), cancellationToken);
        if (!result.Succeeded || result.Value is null)
        {
            return BadRequest(ApiResponse<TicketDto>.ErrorResult(result.Error ?? "Unable to purchase resale ticket"));
        }

        return Ok(ApiResponse<TicketDto>.SuccessResult(result.Value, "Resale ticket purchased"));
    }

    [HttpPost("{ticketId:guid}/cancel")]
    public async Task<ActionResult<ApiResponse<TicketDto>>> Cancel(Guid ticketId, CancelResaleRequest request, CancellationToken cancellationToken)
    {
        var commandRequest = request with { TicketId = ticketId };
        var result = await _mediator.Send(new CancelResaleListingCommand(commandRequest), cancellationToken);
        if (!result.Succeeded || result.Value is null)
        {
            return BadRequest(ApiResponse<TicketDto>.ErrorResult(result.Error ?? "Unable to cancel resale listing"));
        }

        return Ok(ApiResponse<TicketDto>.SuccessResult(result.Value, "Resale listing cancelled"));
    }
}
