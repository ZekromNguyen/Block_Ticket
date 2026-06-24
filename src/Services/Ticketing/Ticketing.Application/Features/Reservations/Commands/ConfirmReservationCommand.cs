using MediatR;
using Shared.Contracts.Dtos;
using Ticketing.Application.Common;
using Ticketing.Application.DTOs;
using Ticketing.Application.Interfaces;
using Ticketing.Domain.Entities;

namespace Ticketing.Application.Features.Reservations.Commands;

public sealed record ConfirmReservationCommand(ConfirmReservationRequest Request) : IRequest<Result<ConfirmReservationResponse>>;

public sealed class ConfirmReservationCommandHandler : IRequestHandler<ConfirmReservationCommand, Result<ConfirmReservationResponse>>
{
    private readonly ITicketingRepository _repository;
    private readonly IPaymentProvider _paymentProvider;
    private readonly ITicketEventPublisher _eventPublisher;
    private readonly IInventoryLockService _inventoryLockService;
    private readonly IRiskAssessmentService _riskAssessment;

    public ConfirmReservationCommandHandler(
        ITicketingRepository repository,
        IPaymentProvider paymentProvider,
        ITicketEventPublisher eventPublisher,
        IInventoryLockService inventoryLockService,
        IRiskAssessmentService riskAssessment)
    {
        _repository = repository;
        _paymentProvider = paymentProvider;
        _eventPublisher = eventPublisher;
        _inventoryLockService = inventoryLockService;
        _riskAssessment = riskAssessment;
    }

    public async Task<Result<ConfirmReservationResponse>> Handle(ConfirmReservationCommand command, CancellationToken cancellationToken)
    {
        var request = command.Request;
        var reservation = await _repository.GetReservationByIdAsync(request.ReservationId, cancellationToken);
        if (reservation is null)
        {
            return Result<ConfirmReservationResponse>.Failure("Reservation not found");
        }

        if (reservation.IsExpired(DateTime.UtcNow))
        {
            reservation.MarkExpired();
            await ReleaseInventoryAsync(reservation, cancellationToken);
            await _repository.SaveChangesAsync(cancellationToken);
            return Result<ConfirmReservationResponse>.Failure("Reservation has expired");
        }

        if (reservation.Status != ReservationStatus.Pending)
        {
            return Result<ConfirmReservationResponse>.Success(new ConfirmReservationResponse(reservation.ToDto(), reservation.Tickets.Select(TicketingMappings.ToDto).ToList()));
        }

        // Risk assessment before payment
        var riskRequest = new RiskAssessmentRequest(
            reservation.UserId,
            reservation.EventId,
            reservation.TotalAmount,
            reservation.Currency,
            request.PaymentMethod,
            null,
            reservation.Items.Sum(i => i.Quantity),
            null);
        var riskResult = await _riskAssessment.AssessAsync(riskRequest, cancellationToken);
        if (riskResult is { Approved: false })
        {
            reservation.Cancel($"Risk check failed: {riskResult.ReviewReason ?? "declined"}");
            await ReleaseInventoryAsync(reservation, cancellationToken);
            await _repository.SaveChangesAsync(cancellationToken);
            return Result<ConfirmReservationResponse>.Failure($"Transaction declined: {riskResult.ReviewReason}");
        }

        var payment = reservation.AddPayment(
            reservation.PaymentIntentId ?? $"pi_{reservation.Id:N}",
            request.PaymentMethod,
            reservation.TotalAmount,
            reservation.Currency);

        var confirmation = await _paymentProvider.ConfirmPaymentAsync(
            payment.PaymentIntentId,
            reservation.TotalAmount,
            reservation.Currency,
            request.PaymentMethod,
            cancellationToken);

        if (!confirmation.Succeeded)
        {
            payment.MarkFailed(confirmation.Error ?? "Payment failed", confirmation.ProcessorData);
            reservation.Cancel(confirmation.Error ?? "Payment failed");
            await ReleaseInventoryAsync(reservation, cancellationToken);
            await _repository.SaveChangesAsync(cancellationToken);
            return Result<ConfirmReservationResponse>.Failure(confirmation.Error ?? "Payment failed");
        }

        var tickets = reservation.Confirm(confirmation.TransactionId, confirmation.ProcessorData);
        await _repository.SaveChangesAsync(cancellationToken);

        foreach (var ticket in tickets)
        {
            await _eventPublisher.PublishTicketPurchasedAsync(ticket, cancellationToken);
            await _eventPublisher.PublishMintTicketAsync(ticket, request.UserWalletAddress ?? "local-dev-wallet", cancellationToken);
        }

        var response = new ConfirmReservationResponse(reservation.ToDto(), tickets.Select(TicketingMappings.ToDto).ToList());
        return Result<ConfirmReservationResponse>.Success(response);
    }

    private Task ReleaseInventoryAsync(Reservation reservation, CancellationToken cancellationToken)
    {
        var items = reservation.Items
            .Select(item => new ReservationItemRequest(item.TicketTypeId, item.TicketTypeName, item.UnitPrice, item.Quantity))
            .ToList();

        return _inventoryLockService.ReleaseAsync(reservation.EventId, items, reservation.InventoryLockOwner, cancellationToken);
    }
}
