using Identity.Application.Common.Configuration;
using Identity.Application.Common.Interfaces;
using Identity.Application.Common.Models;
using Identity.Domain.Repositories;
using Identity.Domain.Services;
using Identity.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Identity.Application.Features.Authentication.Commands;

public record ResendEmailConfirmationCommand(string Email) : ICommand<Result>;

public class ResendEmailConfirmationCommandHandler : ICommandHandler<ResendEmailConfirmationCommand, Result>
{
    private readonly IUserRepository _userRepository;
    private readonly IEmailService _emailService;
    private readonly ITokenService _tokenService;
    private readonly ILogger<ResendEmailConfirmationCommandHandler> _logger;
    private readonly ApplicationSettings _applicationSettings;

    public ResendEmailConfirmationCommandHandler(
        IUserRepository userRepository,
        IEmailService emailService,
        ITokenService tokenService,
        ILogger<ResendEmailConfirmationCommandHandler> logger,
        IOptions<ApplicationSettings> applicationSettings)
    {
        _userRepository = userRepository;
        _emailService = emailService;
        _tokenService = tokenService;
        _logger = logger;
        _applicationSettings = applicationSettings.Value;
    }

    public async Task<Result> Handle(ResendEmailConfirmationCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Find user by email
            var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
            if (user == null)
            {
                _logger.LogWarning("Resend email confirmation requested for non-existent email: {Email}", request.Email);
                // Don't reveal that the email doesn't exist for security reasons
                return Result.Success();
            }

            // Check if email is already confirmed
            if (user.EmailConfirmed)
            {
                _logger.LogInformation("Email confirmation resend requested for already confirmed email: {Email}", request.Email);
                return Result.Success();
            }

            // Generate new confirmation token and send email
            var confirmationToken = _tokenService.GenerateEmailConfirmationToken(user.Id);
            var confirmationUrl = _applicationSettings.EmailConfirmationUrl;

            _logger.LogInformation("🔗 Resend email confirmation URL: {ConfirmationUrl}?token={Token}",
                confirmationUrl, confirmationToken);

            await _emailService.SendEmailConfirmationAsync(user.Email.Value, confirmationToken, confirmationUrl);
            
            _logger.LogInformation("Email confirmation resent to {Email}", request.Email);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to resend email confirmation to {Email}", request.Email);
            return Result.Failure("Failed to send email confirmation. Please try again later.");
        }
    }
}
