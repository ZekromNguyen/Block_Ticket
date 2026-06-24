using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Common.Models;
using Ticketing.Application.DTOs;
using Ticketing.Application.Features.Refunds.Commands;

namespace Ticketing.Api.Controllers;

[ApiController]
[Route("api/webhooks/payments")]
[AllowAnonymous]
public sealed class PaymentWebhookController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<PaymentWebhookController> _logger;

    public PaymentWebhookController(IMediator mediator, ILogger<PaymentWebhookController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    [HttpPost("refund-updated")]
    public async Task<ActionResult<ApiResponse<object>>> HandleRefundUpdated(
        PaymentWebhookRefundRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Received refund webhook for ticket {TicketId}, status: {Status}",
            request.TicketId, request.Status);

        if (request.Status == "succeeded")
        {
            return Ok(ApiResponse<object>.SuccessResult(new { request.TicketId, request.Status }, "Refund webhook processed"));
        }

        if (request.Status == "failed")
        {
            _logger.LogWarning(
                "Refund failed for ticket {TicketId}: {Reason}",
                request.TicketId, request.FailureReason);
        }

        return Ok(ApiResponse<object>.SuccessResult(new { request.TicketId, request.Status }, "Refund webhook acknowledged"));
    }

    [HttpPost("payment-updated")]
    public async Task<ActionResult<ApiResponse<object>>> HandlePaymentUpdated(
        PaymentWebhookPaymentRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Received payment webhook for reservation {ReservationId}, status: {Status}",
            request.ReservationId, request.Status);

        return Ok(ApiResponse<object>.SuccessResult(new { request.ReservationId, request.Status }, "Payment webhook acknowledged"));
    }
}

public sealed record PaymentWebhookRefundRequest(
    Guid TicketId,
    string Status,
    string? FailureReason,
    string? TransactionId);

public sealed record PaymentWebhookPaymentRequest(
    Guid ReservationId,
    string Status,
    string? TransactionId,
    string? FailureReason);
