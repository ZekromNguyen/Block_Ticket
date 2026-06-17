using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Common.Models;
using Ticketing.Application.DTOs;
using Ticketing.Application.Features.Admin.Commands;
using Ticketing.Application.Features.Admin.Queries;

namespace Ticketing.Api.Controllers;

[ApiController]
[Route("api/admin/ticketing")]
[Authorize(Policy = "RequireAdminRole")]
public sealed class AdminController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdminController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("reservations/{reservationId:guid}/force-expire")]
    public async Task<ActionResult<ApiResponse<ReservationDto>>> ForceExpireReservation(
        Guid reservationId,
        AdminForceExpireReservationRequest request,
        CancellationToken cancellationToken)
    {
        var commandRequest = request with { ReservationId = reservationId };
        var result = await _mediator.Send(new ForceExpireReservationCommand(commandRequest), cancellationToken);
        if (!result.Succeeded || result.Value is null)
        {
            return BadRequest(ApiResponse<ReservationDto>.ErrorResult(result.Error ?? "Unable to expire reservation"));
        }

        return Ok(ApiResponse<ReservationDto>.SuccessResult(result.Value, "Reservation expired"));
    }

    [HttpGet("reservations/{reservationId:guid}/payment")]
    public async Task<ActionResult<ApiResponse<PaymentDto>>> GetPayment(Guid reservationId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetReservationPaymentQuery(reservationId), cancellationToken);
        if (!result.Succeeded || result.Value is null)
        {
            return NotFound(ApiResponse<PaymentDto>.ErrorResult(result.Error ?? "Payment not found"));
        }

        return Ok(ApiResponse<PaymentDto>.SuccessResult(result.Value));
    }

    [HttpPost("tickets/{ticketId:guid}/retry-mint")]
    public async Task<ActionResult<ApiResponse<TicketDto>>> RetryMint(Guid ticketId, AdminRetryMintRequest request, CancellationToken cancellationToken)
    {
        var commandRequest = request with { TicketId = ticketId };
        var result = await _mediator.Send(new RetryTicketMintCommand(commandRequest), cancellationToken);
        if (!result.Succeeded || result.Value is null)
        {
            return BadRequest(ApiResponse<TicketDto>.ErrorResult(result.Error ?? "Unable to retry mint"));
        }

        return Ok(ApiResponse<TicketDto>.SuccessResult(result.Value, "Mint retry requested"));
    }

    [HttpPost("tickets/{ticketId:guid}/verification-override")]
    public async Task<ActionResult<ApiResponse<TicketDto>>> OverrideVerification(
        Guid ticketId,
        AdminVerificationOverrideRequest request,
        CancellationToken cancellationToken)
    {
        var commandRequest = request with { TicketId = ticketId };
        var result = await _mediator.Send(new OverrideTicketVerificationCommand(commandRequest), cancellationToken);
        if (!result.Succeeded || result.Value is null)
        {
            return BadRequest(ApiResponse<TicketDto>.ErrorResult(result.Error ?? "Unable to override verification"));
        }

        return Ok(ApiResponse<TicketDto>.SuccessResult(result.Value, "Verification override enabled"));
    }

    [HttpPost("tickets/{ticketId:guid}/refund")]
    public async Task<ActionResult<ApiResponse<TicketDto>>> RefundTicket(Guid ticketId, AdminRefundTicketRequest request, CancellationToken cancellationToken)
    {
        var commandRequest = request with { TicketId = ticketId };
        var result = await _mediator.Send(new AdminRefundTicketCommand(commandRequest), cancellationToken);
        if (!result.Succeeded || result.Value is null)
        {
            return BadRequest(ApiResponse<TicketDto>.ErrorResult(result.Error ?? "Unable to refund ticket"));
        }

        return Ok(ApiResponse<TicketDto>.SuccessResult(result.Value, "Ticket refunded"));
    }

    [HttpGet("audit-notes")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<AdminAuditNoteDto>>>> GetAuditNotes(
        [FromQuery] Guid? ticketId,
        [FromQuery] Guid? reservationId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetAdminAuditNotesQuery(ticketId, reservationId), cancellationToken);
        return Ok(ApiResponse<IReadOnlyCollection<AdminAuditNoteDto>>.SuccessResult(result.Value ?? Array.Empty<AdminAuditNoteDto>()));
    }
}
