using MediatR;
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

    public CreateReservationCommandHandler(
        ITicketingRepository repository,
        IInventoryLockService inventoryLockService,
        IPaymentProvider paymentProvider)
    {
        _repository = repository;
        _inventoryLockService = inventoryLockService;
        _paymentProvider = paymentProvider;
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
                request.Currency,
                request.IdempotencyKey,
                lockOwner,
                DateTime.UtcNow.Add(ReservationTtl));

            foreach (var item in request.Items)
            {
                reservation.AddItem(item.TicketTypeId, item.TicketTypeName, item.UnitPrice, item.Quantity);
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
