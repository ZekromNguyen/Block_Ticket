using Identity.Domain.Services;
using Microsoft.Extensions.Logging;

namespace Identity.Infrastructure.Services;

public class SmsService : ISmsService
{
    private readonly ILogger<SmsService> _logger;

    public SmsService(ILogger<SmsService> logger)
    {
        _logger = logger;
    }

    public async Task SendSmsAsync(string to, string message)
    {
        // TODO: Implement SMS sending (Twilio, AWS SNS, etc.)
        _logger.LogInformation("Sending SMS to {To} with message: {Message}", to, message);
        await Task.Delay(100); // Simulate async operation
    }

    public async Task SendMfaCodeAsync(string phoneNumber, string code)
    {
        var message = $"Your BlockTicket verification code is: {code}";
        await SendSmsAsync(phoneNumber, message);
    }

    public async Task SendSecurityAlertAsync(string phoneNumber, string alertMessage)
    {
        var message = $"BlockTicket Security Alert: {alertMessage}";
        await SendSmsAsync(phoneNumber, message);
    }
}
