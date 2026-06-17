using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Common.Models;
using Ticketing.Application.DTOs;
using Ticketing.Application.Features.WaitingList.Commands;

namespace Ticketing.Api.Controllers;

[ApiController]
[Route("api/waiting-list")]
[Authorize]
public sealed class WaitingListController : ControllerBase
{
    private readonly IMediator _mediator;

    public WaitingListController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("join")]
    public async Task<ActionResult<ApiResponse<WaitingListEntryDto>>> Join(WaitingListJoinRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new JoinWaitingListCommand(request), cancellationToken);
        if (!result.Succeeded || result.Value is null)
        {
            return BadRequest(ApiResponse<WaitingListEntryDto>.ErrorResult(result.Error ?? "Unable to join waiting list"));
        }

        return Ok(ApiResponse<WaitingListEntryDto>.SuccessResult(result.Value, "Joined waiting list"));
    }

    [HttpDelete("events/{eventId:guid}/ticket-types/{ticketTypeId:guid}/users/{userId:guid}")]
    public async Task<ActionResult<ApiResponse<WaitingListEntryDto>>> Leave(Guid eventId, Guid ticketTypeId, Guid userId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new LeaveWaitingListCommand(userId, eventId, ticketTypeId), cancellationToken);
        if (!result.Succeeded || result.Value is null)
        {
            return NotFound(ApiResponse<WaitingListEntryDto>.ErrorResult(result.Error ?? "Waiting list entry not found"));
        }

        return Ok(ApiResponse<WaitingListEntryDto>.SuccessResult(result.Value, "Left waiting list"));
    }

    [HttpPost("offers")]
    public async Task<ActionResult<ApiResponse<WaitingListEntryDto>>> CreateOffer(WaitingListOfferRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new CreateWaitingListOfferCommand(request), cancellationToken);
        if (!result.Succeeded || result.Value is null)
        {
            return BadRequest(ApiResponse<WaitingListEntryDto>.ErrorResult(result.Error ?? "Unable to create waiting-list offer"));
        }

        return Ok(ApiResponse<WaitingListEntryDto>.SuccessResult(result.Value, "Waiting-list offer created"));
    }
}
