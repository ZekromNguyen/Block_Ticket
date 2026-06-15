using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Common.Models;
using Ticketing.Application.DTOs;
using Ticketing.Application.Features.Reservations.Commands;
using Ticketing.Application.Features.Reservations.Queries;

namespace Ticketing.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReservationsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ReservationsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<ReservationDto>>> CreateReservation(CreateReservationRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new CreateReservationCommand(request), cancellationToken);
        if (!result.Succeeded || result.Value is null)
        {
            return BadRequest(ApiResponse<ReservationDto>.ErrorResult(result.Error ?? "Reservation failed"));
        }

        return Ok(ApiResponse<ReservationDto>.SuccessResult(result.Value, "Reservation created"));
    }

    [HttpPost("{reservationId:guid}/confirm")]
    public async Task<ActionResult<ApiResponse<ConfirmReservationResponse>>> ConfirmReservation(
        Guid reservationId,
        ConfirmReservationRequest request,
        CancellationToken cancellationToken)
    {
        var commandRequest = request with { ReservationId = reservationId };
        var result = await _mediator.Send(new ConfirmReservationCommand(commandRequest), cancellationToken);
        if (!result.Succeeded || result.Value is null)
        {
            return BadRequest(ApiResponse<ConfirmReservationResponse>.ErrorResult(result.Error ?? "Confirmation failed"));
        }

        return Ok(ApiResponse<ConfirmReservationResponse>.SuccessResult(result.Value, "Reservation confirmed"));
    }

    [HttpGet("{reservationId:guid}")]
    public async Task<ActionResult<ApiResponse<ReservationDto>>> GetReservation(Guid reservationId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetReservationQuery(reservationId), cancellationToken);
        if (!result.Succeeded || result.Value is null)
        {
            return NotFound(ApiResponse<ReservationDto>.ErrorResult(result.Error ?? "Reservation not found"));
        }

        return Ok(ApiResponse<ReservationDto>.SuccessResult(result.Value));
    }
}
