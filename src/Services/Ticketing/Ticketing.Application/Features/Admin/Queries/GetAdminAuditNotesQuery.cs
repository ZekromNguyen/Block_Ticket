using MediatR;
using Ticketing.Application.Common;
using Ticketing.Application.DTOs;
using Ticketing.Application.Interfaces;

namespace Ticketing.Application.Features.Admin.Queries;

public sealed record GetAdminAuditNotesQuery(Guid? TicketId, Guid? ReservationId) : IRequest<Result<IReadOnlyCollection<AdminAuditNoteDto>>>;

public sealed record GetReservationPaymentQuery(Guid ReservationId) : IRequest<Result<PaymentDto>>;

public sealed class GetAdminAuditNotesQueryHandler : IRequestHandler<GetAdminAuditNotesQuery, Result<IReadOnlyCollection<AdminAuditNoteDto>>>
{
    private readonly ITicketingRepository _repository;

    public GetAdminAuditNotesQueryHandler(ITicketingRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<IReadOnlyCollection<AdminAuditNoteDto>>> Handle(GetAdminAuditNotesQuery request, CancellationToken cancellationToken)
    {
        var notes = await _repository.GetAdminAuditNotesAsync(request.TicketId, request.ReservationId, cancellationToken);
        return Result<IReadOnlyCollection<AdminAuditNoteDto>>.Success(notes.Select(TicketingMappings.ToDto).ToList());
    }
}

public sealed class GetReservationPaymentQueryHandler : IRequestHandler<GetReservationPaymentQuery, Result<PaymentDto>>
{
    private readonly ITicketingRepository _repository;

    public GetReservationPaymentQueryHandler(ITicketingRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<PaymentDto>> Handle(GetReservationPaymentQuery request, CancellationToken cancellationToken)
    {
        var payment = await _repository.GetPaymentByReservationIdAsync(request.ReservationId, cancellationToken);
        return payment is null
            ? Result<PaymentDto>.Failure("Payment not found")
            : Result<PaymentDto>.Success(payment.ToDto());
    }
}
