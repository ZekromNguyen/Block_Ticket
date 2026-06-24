using MediatR;
using Shared.Contracts.Dtos;
using Ticketing.Application.Common;
using Ticketing.Application.DTOs;
using Ticketing.Application.Interfaces;
using Ticketing.Domain.Entities;

namespace Ticketing.Application.Features.Reservations.Commands;

public sealed record CreateReservationCommand(CreateReservationRequest Request) : IRequest<Result<ReservationDto>>;

public sealed class CreateReservationCommandHandler : IRequestHandler<CreateReservationCommand, Result<ReservationDto>>
{
    private static readonly TimeSpan ReservationTtl = TimeSpan.FromMinutes(15);

    private readonly ITicketingRepository _repository;
    private readonly IInventoryLockService _inventoryLockService;
    private readonly IPaymentProvider _paymentProvider;
    private readonly ISeatMapAvailabilityService _seatMapAvailability;
    private readonly IPricingEvaluationService _pricingEvaluation;
    private readonly ICurrencyPolicyService _currencyPolicy;

    public CreateReservationCommandHandler(
        ITicketingRepository repository,
        IInventoryLockService inventoryLockService,
        IPaymentProvider paymentProvider,
        ISeatMapAvailabilityService seatMapAvailability,
        IPricingEvaluationService pricingEvaluation,
        ICurrencyPolicyService currencyPolicy)
    {
        _repository = repository;
        _inventoryLockService = inventoryLockService;
        _paymentProvider = paymentProvider;
        _seatMapAvailability = seatMapAvailability;
        _pricingEvaluation = pricingEvaluation;
        _currencyPolicy = currencyPolicy;
    }

    public async Task<Result<ReservationDto>> Handle(CreateReservationCommand command, CancellationToken cancellationToken)
    {
        var request = command.Request;

        if (request.UserId == Guid.Empty || request.EventId == Guid.Empty)
        {
            return Result<ReservationDto>.Failure("UserId and EventId are required");
        }

        if (request.Items.Count == 0)
        {
            return Result<ReservationDto>.Failure("At least one reservation item is required");
        }

        if (request.Items.Any(item => item.TicketTypeId == Guid.Empty || item.Quantity <= 0 || item.UnitPrice < 0))
        {
            return Result<ReservationDto>.Failure("Reservation items must include ticket type, positive quantity, and non-negative price");
        }

        // Validate currency against event policy
        var currency = request.Currency ?? "USD";
        var currencyCheck = await _currencyPolicy.ValidateAsync(request.EventId, currency, cancellationToken);
        if (!currencyCheck.Allowed)
        {
            return Result<ReservationDto>.Failure(currencyCheck.Reason ?? "Currency not allowed");
        }

        // Evaluate pricing rules to snapshot final prices
        var pricingRequest = new PricingEvaluationRequest(
            request.EventId,
            request.Items.Select(i => new PricingLineItem(i.TicketTypeId, i.TicketTypeName, i.UnitPrice, i.Quantity)).ToArray(),
            null, request.UserId, currency);
        var pricingResult = await _pricingEvaluation.EvaluateAsync(pricingRequest, cancellationToken);

        if (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
        {
            var existing = await _repository.GetReservationByIdempotencyKeyAsync(request.IdempotencyKey, cancellationToken);
            if (existing is not null)
            {
                return Result<ReservationDto>.Success(existing.ToDto());
            }
        }

        var lockOwner = request.IdempotencyKey ?? Guid.NewGuid().ToString("N");
        var locked = await _inventoryLockService.TryReserveAsync(request.EventId, request.Items, lockOwner, ReservationTtl, cancellationToken);
        if (!locked)
        {
            return Result<ReservationDto>.Failure("Requested ticket inventory is not available");
        }

        try
        {
            var reservation = new Reservation(
                request.UserId,
                request.EventId,
                currency,
                request.IdempotencyKey,
                lockOwner,
                DateTime.UtcNow.Add(ReservationTtl));

            if (pricingResult is not null)
            {
                foreach (var pricedItem in pricingResult.Items)
                {
                    reservation.AddItem(pricedItem.TicketTypeId, pricedItem.TicketTypeName, pricedItem.FinalUnitPrice, pricedItem.Quantity);
                }
            }
            else
            {
                foreach (var item in request.Items)
                {
                    reservation.AddItem(item.TicketTypeId, item.TicketTypeName, item.UnitPrice, item.Quantity);
                }
            }

            if (pricingResult is not null)
            {
                reservation.SetPricingSnapshot(
                    pricingResult.Subtotal,
                    pricingResult.DiscountTotal,
                    pricingResult.ServiceFee,
                    pricingResult.ProcessingFee,
                    pricingResult.TotalAmount);
            }

            var intent = await _paymentProvider.CreatePaymentIntentAsync(
                reservation.Id,
                reservation.TotalAmount,
                reservation.Currency,
                "pending",
                cancellationToken);

            if (!intent.Succeeded)
            {
                await _inventoryLockService.ReleaseAsync(request.EventId, request.Items, lockOwner, cancellationToken);
                return Result<ReservationDto>.Failure(intent.Error ?? "Unable to create payment intent");
            }

            reservation.SetPaymentIntent(intent.PaymentIntentId);
            await _repository.AddReservationAsync(reservation, cancellationToken);
            await _repository.SaveChangesAsync(cancellationToken);

            return Result<ReservationDto>.Success(reservation.ToDto());
        }
        catch
        {
            await _inventoryLockService.ReleaseAsync(request.EventId, request.Items, lockOwner, cancellationToken);
            throw;
        }
    }
}

public sealed record CreateSeatReservationCommand(
    Guid UserId,
    Guid EventId,
    Guid TicketTypeId,
    IReadOnlyCollection<Guid> SeatIds,
    string Currency,
    string? IdempotencyKey) : IRequest<Result<ReservationDto>>;

public sealed class CreateSeatReservationCommandHandler : IRequestHandler<CreateSeatReservationCommand, Result<ReservationDto>>
{
    private static readonly TimeSpan ReservationTtl = TimeSpan.FromMinutes(15);

    private readonly ITicketingRepository _repository;
    private readonly IInventoryLockService _inventoryLockService;
    private readonly IPaymentProvider _paymentProvider;
    private readonly ISeatMapAvailabilityService _seatMapAvailability;

    public CreateSeatReservationCommandHandler(
        ITicketingRepository repository,
        IInventoryLockService inventoryLockService,
        IPaymentProvider paymentProvider,
        ISeatMapAvailabilityService seatMapAvailability)
    {
        _repository = repository;
        _inventoryLockService = inventoryLockService;
        _paymentProvider = paymentProvider;
        _seatMapAvailability = seatMapAvailability;
    }

    public async Task<Result<ReservationDto>> Handle(CreateSeatReservationCommand command, CancellationToken cancellationToken)
    {
        if (command.UserId == Guid.Empty || command.EventId == Guid.Empty)
        {
            return Result<ReservationDto>.Failure("UserId and EventId are required");
        }

        if (command.SeatIds.Count == 0)
        {
            return Result<ReservationDto>.Failure("At least one seat must be selected");
        }

        if (!string.IsNullOrWhiteSpace(command.IdempotencyKey))
        {
            var existing = await _repository.GetReservationByIdempotencyKeyAsync(command.IdempotencyKey, cancellationToken);
            if (existing is not null)
            {
                return Result<ReservationDto>.Success(existing.ToDto());
            }
        }

        var snapshot = await _seatMapAvailability.GetSnapshotAsync(command.EventId, command.TicketTypeId, cancellationToken);
        if (snapshot is null)
        {
            return Result<ReservationDto>.Failure("Unable to retrieve seat availability");
        }

        var requestedSeats = snapshot.Seats.Where(s => command.SeatIds.Contains(s.SeatId)).ToList();
        var unavailableSeats = requestedSeats.Where(s => s.Availability != SeatAvailability.Available).ToList();
        if (unavailableSeats.Count > 0)
        {
            return Result<ReservationDto>.Failure($"Seats not available: {string.Join(", ", unavailableSeats.Select(s => $"{s.Section}-{s.Row}-{s.Number}"))}");
        }

        var holdOwner = command.IdempotencyKey ?? Guid.NewGuid().ToString("N");
        var holdResult = await _seatMapAvailability.HoldSeatsAsync(
            new SeatHoldRequestDto(command.EventId, command.TicketTypeId, command.SeatIds, holdOwner, DateTime.UtcNow.Add(ReservationTtl)),
            cancellationToken);

        if (holdResult is null || holdResult.RejectedSeatIds.Count > 0)
        {
            return Result<ReservationDto>.Failure("Unable to hold selected seats");
        }

        var unitPrice = requestedSeats.FirstOrDefault()?.Price ?? 0m;
        var ticketTypeName = $"Seat {requestedSeats.FirstOrDefault()?.Section}";
        var reservation = new Reservation(
            command.UserId,
            command.EventId,
            command.Currency,
            command.IdempotencyKey,
            holdOwner,
            DateTime.UtcNow.Add(ReservationTtl));

        reservation.AddItem(command.TicketTypeId, ticketTypeName, unitPrice, command.SeatIds.Count);

        var intent = await _paymentProvider.CreatePaymentIntentAsync(
            reservation.Id,
            reservation.TotalAmount,
            reservation.Currency,
            "pending",
            cancellationToken);

        if (!intent.Succeeded)
        {
            await _seatMapAvailability.ReleaseSeatsAsync(command.EventId, command.TicketTypeId, holdOwner, cancellationToken);
            return Result<ReservationDto>.Failure(intent.Error ?? "Unable to create payment intent");
        }

        reservation.SetPaymentIntent(intent.PaymentIntentId);
        await _repository.AddReservationAsync(reservation, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result<ReservationDto>.Success(reservation.ToDto());
    }
}
