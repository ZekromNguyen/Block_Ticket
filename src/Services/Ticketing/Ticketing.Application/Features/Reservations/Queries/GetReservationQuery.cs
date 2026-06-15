using MediatR;
using Ticketing.Application.Common;
using Ticketing.Application.DTOs;
using Ticketing.Application.Interfaces;

namespace Ticketing.Application.Features.Reservations.Queries;

public sealed record GetReservationQuery(Guid ReservationId) : IRequest<Result<ReservationDto>>;

public sealed class GetReservationQueryHandler : IRequestHandler<GetReservationQuery, Result<ReservationDto>>
{
    private readonly ITicketingRepository _repository;

    public GetReservationQueryHandler(ITicketingRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<ReservationDto>> Handle(GetReservationQuery request, CancellationToken cancellationToken)
    {
        var reservation = await _repository.GetReservationByIdAsync(request.ReservationId, cancellationToken);
        return reservation is null
            ? Result<ReservationDto>.Failure("Reservation not found")
            : Result<ReservationDto>.Success(reservation.ToDto());
    }
}
