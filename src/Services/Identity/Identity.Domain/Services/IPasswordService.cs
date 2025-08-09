namespace Identity.Domain.Services;

public interface IPasswordService
{
    string HashPassword(string password);
    bool VerifyPassword(string password, string hashedPassword);
    bool IsPasswordStrong(string password);
    string GenerateRandomPassword(int length = 12);
}
