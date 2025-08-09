using Identity.Domain.Services;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Identity.Infrastructure.Services;

public class PasswordService : IPasswordService
{
    private readonly ILogger<PasswordService> _logger;
    private static readonly Regex PasswordStrengthRegex = new(
        @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$",
        RegexOptions.Compiled);

    public PasswordService(ILogger<PasswordService> logger)
    {
        _logger = logger;
    }

    public string HashPassword(string password)
    {
        if (string.IsNullOrEmpty(password))
            throw new ArgumentException("Password cannot be null or empty", nameof(password));

        try
        {
            // Generate a random salt
            byte[] salt = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }

            // Hash the password with the salt using PBKDF2
            using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 100000, HashAlgorithmName.SHA256);
            byte[] hash = pbkdf2.GetBytes(32);

            // Combine salt and hash
            byte[] hashBytes = new byte[64];
            Array.Copy(salt, 0, hashBytes, 0, 32);
            Array.Copy(hash, 0, hashBytes, 32, 32);

            // Convert to base64 string
            return Convert.ToBase64String(hashBytes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error hashing password");
            throw;
        }
    }

    public bool VerifyPassword(string password, string hashedPassword)
    {
        if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(hashedPassword))
            return false;

        try
        {
            // Convert the stored hash back to bytes
            byte[] hashBytes = Convert.FromBase64String(hashedPassword);

            // Extract the salt (first 32 bytes)
            byte[] salt = new byte[32];
            Array.Copy(hashBytes, 0, salt, 0, 32);

            // Hash the provided password with the extracted salt
            using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 100000, HashAlgorithmName.SHA256);
            byte[] hash = pbkdf2.GetBytes(32);

            // Compare the computed hash with the stored hash (last 32 bytes)
            for (int i = 0; i < 32; i++)
            {
                if (hashBytes[i + 32] != hash[i])
                    return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying password");
            return false;
        }
    }

    public bool IsPasswordStrong(string password)
    {
        if (string.IsNullOrEmpty(password))
            return false;

        // Check minimum requirements:
        // - At least 8 characters
        // - At least one lowercase letter
        // - At least one uppercase letter
        // - At least one digit
        // - At least one special character
        return PasswordStrengthRegex.IsMatch(password);
    }

    public string GenerateRandomPassword(int length = 12)
    {
        if (length < 8)
            throw new ArgumentException("Password length must be at least 8 characters", nameof(length));

        const string lowercase = "abcdefghijklmnopqrstuvwxyz";
        const string uppercase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        const string digits = "0123456789";
        const string special = "@$!%*?&";
        const string allChars = lowercase + uppercase + digits + special;

        var password = new StringBuilder();
        using var rng = RandomNumberGenerator.Create();

        // Ensure at least one character from each required category
        password.Append(GetRandomChar(lowercase, rng));
        password.Append(GetRandomChar(uppercase, rng));
        password.Append(GetRandomChar(digits, rng));
        password.Append(GetRandomChar(special, rng));

        // Fill the rest with random characters
        for (int i = 4; i < length; i++)
        {
            password.Append(GetRandomChar(allChars, rng));
        }

        // Shuffle the password to avoid predictable patterns
        return ShuffleString(password.ToString(), rng);
    }

    private static char GetRandomChar(string chars, RandomNumberGenerator rng)
    {
        byte[] randomBytes = new byte[4];
        rng.GetBytes(randomBytes);
        int randomIndex = Math.Abs(BitConverter.ToInt32(randomBytes, 0)) % chars.Length;
        return chars[randomIndex];
    }

    private static string ShuffleString(string input, RandomNumberGenerator rng)
    {
        char[] array = input.ToCharArray();
        for (int i = array.Length - 1; i > 0; i--)
        {
            byte[] randomBytes = new byte[4];
            rng.GetBytes(randomBytes);
            int j = Math.Abs(BitConverter.ToInt32(randomBytes, 0)) % (i + 1);
            (array[i], array[j]) = (array[j], array[i]);
        }
        return new string(array);
    }
}
