namespace Identity.Application.Common.Configuration;

public class ApplicationSettings
{
    public const string SectionName = "Application";
    
    public string BaseUrl { get; set; } = string.Empty;
    public string EmailConfirmationPath { get; set; } = "/confirm-email";
    public string PasswordResetPath { get; set; } = "/reset-password";
    
    /// <summary>
    /// Gets the full URL for email confirmation
    /// </summary>
    public string EmailConfirmationUrl => $"{BaseUrl.TrimEnd('/')}{EmailConfirmationPath}";
    
    /// <summary>
    /// Gets the full URL for password reset
    /// </summary>
    public string PasswordResetUrl => $"{BaseUrl.TrimEnd('/')}{PasswordResetPath}";
}
