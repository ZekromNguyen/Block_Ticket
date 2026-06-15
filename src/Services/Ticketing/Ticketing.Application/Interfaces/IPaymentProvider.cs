namespace Ticketing.Application.Interfaces;

public interface IPaymentProvider
{
    Task<PaymentIntentResult> CreatePaymentIntentAsync(Guid reservationId, decimal amount, string currency, string paymentMethod, CancellationToken cancellationToken);

    Task<PaymentConfirmationResult> ConfirmPaymentAsync(string paymentIntentId, decimal amount, string currency, string paymentMethod, CancellationToken cancellationToken);
}

public sealed record PaymentIntentResult(string PaymentIntentId, bool Succeeded, string? Error);

public sealed record PaymentConfirmationResult(string TransactionId, bool Succeeded, string? Error, string ProcessorData);
