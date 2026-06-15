using Ticketing.Application.Interfaces;

namespace Ticketing.Infrastructure.Payments;

public sealed class FakePaymentProvider : IPaymentProvider
{
    public Task<PaymentIntentResult> CreatePaymentIntentAsync(Guid reservationId, decimal amount, string currency, string paymentMethod, CancellationToken cancellationToken)
    {
        return Task.FromResult(new PaymentIntentResult($"pi_{reservationId:N}", true, null));
    }

    public Task<PaymentConfirmationResult> ConfirmPaymentAsync(string paymentIntentId, decimal amount, string currency, string paymentMethod, CancellationToken cancellationToken)
    {
        if (paymentMethod.Equals("fail", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(new PaymentConfirmationResult(string.Empty, false, "Payment declined by fake provider", "{\"provider\":\"fake\",\"status\":\"declined\"}"));
        }

        var transactionId = $"txn_{Guid.NewGuid():N}";
        return Task.FromResult(new PaymentConfirmationResult(transactionId, true, null, $"{{\"provider\":\"fake\",\"status\":\"succeeded\",\"paymentIntentId\":\"{paymentIntentId}\"}}"));
    }
}
