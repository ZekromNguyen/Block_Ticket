using MediatR;
using Ticketing.Application.Common;
using Ticketing.Application.DTOs;
using Ticketing.Application.Interfaces;
using Ticketing.Domain.Entities;

namespace Ticketing.Application.Features.Admin.Commands;

public sealed record ForceExpireReservationCommand(AdminForceExpireReservationRequest Request) : IRequest<Result<ReservationDto>>;

public sealed record RetryTicketMintCommand(AdminRetryMintRequest Request) : IRequest<Result<TicketDto>>;

public sealed record OverrideTicketVerificationCommand(AdminVerificationOverrideRequest Request) : IRequest<Result<TicketDto>>;

public sealed record AdminRefundTicketCommand(AdminRefundTicketRequest Request) : IRequest<Result<TicketDto>>;

public sealed class ForceExpireReservationCommandHandler : IRequestHandler<ForceExpireReservationCommand, Result<ReservationDto>>
{
    private readonly ITicketingRepository _repository;
    private readonly ITicketEventPublisher _publisher;

    public ForceExpireReservationCommandHandler(ITicketingRepository repository, ITicketEventPublisher publisher)
    {
        _repository = repository;
        _publisher = publisher;
    }

    public async Task<Result<ReservationDto>> Handle(ForceExpireReservationCommand command, CancellationToken cancellationToken)
    {
        var request = command.Request;
        var reservation = await _repository.GetReservationByIdAsync(request.ReservationId, cancellationToken);
        if (reservation is null)
        {
            return Result<ReservationDto>.Failure("Reservation not found");
        }

        reservation.MarkExpired();
        await _repository.AddAdminAuditNoteAsync(new AdminAuditNote(null, reservation.Id, "ForceExpireReservation", request.AdminUserId, request.Note), cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);
        await _publisher.PublishReservationReleasedAsync(reservation, cancellationToken);
        return Result<ReservationDto>.Success(reservation.ToDto());
    }
}

public sealed class RetryTicketMintCommandHandler : IRequestHandler<RetryTicketMintCommand, Result<TicketDto>>
{
    private readonly ITicketingRepository _repository;
    private readonly ITicketEventPublisher _publisher;

    public RetryTicketMintCommandHandler(ITicketingRepository repository, ITicketEventPublisher publisher)
    {
        _repository = repository;
        _publisher = publisher;
    }

    public async Task<Result<TicketDto>> Handle(RetryTicketMintCommand command, CancellationToken cancellationToken)
    {
        var request = command.Request;
        var ticket = await _repository.GetTicketByIdAsync(request.TicketId, cancellationToken);
        if (ticket is null)
        {
            return Result<TicketDto>.Failure("Ticket not found");
        }

        await _repository.AddAdminAuditNoteAsync(new AdminAuditNote(ticket.Id, null, "RetryTicketMint", request.AdminUserId, request.Reason), cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);
        await _publisher.PublishRetryMintAsync(ticket, request.UserWalletAddress, request.AdminUserId, request.Reason, cancellationToken);
        return Result<TicketDto>.Success(ticket.ToDto());
    }
}

public sealed class OverrideTicketVerificationCommandHandler : IRequestHandler<OverrideTicketVerificationCommand, Result<TicketDto>>
{
    private readonly ITicketingRepository _repository;

    public OverrideTicketVerificationCommandHandler(ITicketingRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<TicketDto>> Handle(OverrideTicketVerificationCommand command, CancellationToken cancellationToken)
    {
        var request = command.Request;
        var ticket = await _repository.GetTicketByIdAsync(request.TicketId, cancellationToken);
        if (ticket is null)
        {
            return Result<TicketDto>.Failure("Ticket not found");
        }

        ticket.AllowVerificationOverride(request.Reason, request.ValidUntil);
        await _repository.AddAdminAuditNoteAsync(new AdminAuditNote(ticket.Id, null, "OverrideTicketVerification", request.AdminUserId, request.Reason), cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);
        return Result<TicketDto>.Success(ticket.ToDto());
    }
}

public sealed class AdminRefundTicketCommandHandler : IRequestHandler<AdminRefundTicketCommand, Result<TicketDto>>
{
    private readonly ITicketingRepository _repository;
    private readonly IPaymentProvider _paymentProvider;
    private readonly ITicketEventPublisher _publisher;

    public AdminRefundTicketCommandHandler(ITicketingRepository repository, IPaymentProvider paymentProvider, ITicketEventPublisher publisher)
    {
        _repository = repository;
        _paymentProvider = paymentProvider;
        _publisher = publisher;
    }

    public async Task<Result<TicketDto>> Handle(AdminRefundTicketCommand command, CancellationToken cancellationToken)
    {
        var request = command.Request;
        var ticket = await _repository.GetTicketByIdAsync(request.TicketId, cancellationToken);
        if (ticket is null)
        {
            return Result<TicketDto>.Failure("Ticket not found");
        }

        var refund = await _paymentProvider.RefundPaymentAsync(ticket.Id, ticket.PricePaid, request.Reason, cancellationToken);
        if (!refund.Succeeded)
        {
            return Result<TicketDto>.Failure(refund.Error ?? "Refund failed");
        }

        ticket.Refund(ticket.PricePaid, request.Reason);
        await _repository.AddAdminAuditNoteAsync(new AdminAuditNote(ticket.Id, null, "AdminRefundTicket", request.AdminUserId, request.Reason), cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);
        await _publisher.PublishTicketRefundedAsync(ticket, ticket.PricePaid, request.Reason, cancellationToken);
        await _publisher.PublishTicketsRestockedAsync(ticket, request.Reason, cancellationToken);

        if (!string.IsNullOrWhiteSpace(request.UserWalletAddress))
        {
            await _publisher.PublishBurnTicketAsync(ticket, request.UserWalletAddress, request.Reason, cancellationToken);
        }

        return Result<TicketDto>.Success(ticket.ToDto());
    }
}
