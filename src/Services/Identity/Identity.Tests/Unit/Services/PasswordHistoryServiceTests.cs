using Identity.Domain.Configuration;
using Identity.Domain.Entities;
using Identity.Domain.Repositories;
using Identity.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Identity.Tests.Unit.Services;

public class PasswordHistoryServiceTests
{
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<IPasswordHistoryRepository> _mockPasswordHistoryRepository;
    private readonly Mock<ILogger<PasswordHistoryService>> _mockLogger;
    private readonly IOptions<PasswordConfiguration> _passwordConfig;
    private readonly PasswordHistoryService _service;

    public PasswordHistoryServiceTests()
    {
        _mockUserRepository = new Mock<IUserRepository>();
        _mockPasswordHistoryRepository = new Mock<IPasswordHistoryRepository>();
        _mockLogger = new Mock<ILogger<PasswordHistoryService>>();

        _passwordConfig = Options.Create(new PasswordConfiguration
        {
            EnablePasswordHistory = true,
            PasswordHistoryCount = 5,
            PasswordHistoryRetentionDays = 365
        });

        _service = new PasswordHistoryService(
            _mockUserRepository.Object,
            _mockPasswordHistoryRepository.Object,
            _passwordConfig,
            _mockLogger.Object);
    }

    [Fact]
    public async Task ValidatePasswordNotInHistoryAsync_WhenPasswordHistoryDisabled_ShouldReturnSuccess()
    {
        // Arrange
        var config = Options.Create(new PasswordConfiguration { EnablePasswordHistory = false });
        var service = new PasswordHistoryService(
            _mockUserRepository.Object,
            _mockPasswordHistoryRepository.Object,
            config,
            _mockLogger.Object);

        var userId = Guid.NewGuid();
        var passwordHash = "newPasswordHash";

        // Act
        var result = await service.ValidatePasswordNotInHistoryAsync(userId, passwordHash);

        // Assert
        Assert.True(result.IsSuccess);
        _mockPasswordHistoryRepository.Verify(
            x => x.IsPasswordInHistoryAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ValidatePasswordNotInHistoryAsync_WhenPasswordNotInHistory_ShouldReturnSuccess()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var passwordHash = "newPasswordHash";

        _mockPasswordHistoryRepository
            .Setup(x => x.IsPasswordInHistoryAsync(userId, passwordHash, 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await service.ValidatePasswordNotInHistoryAsync(userId, passwordHash);

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task ValidatePasswordNotInHistoryAsync_WhenPasswordInHistory_ShouldReturnFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var passwordHash = "existingPasswordHash";

        _mockPasswordHistoryRepository
            .Setup(x => x.IsPasswordInHistoryAsync(userId, passwordHash, 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await service.ValidatePasswordNotInHistoryAsync(userId, passwordHash);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("cannot be reused", result.ErrorMessage);
    }

    [Fact]
    public async Task ChangePasswordWithHistoryAsync_WhenUserNotFound_ShouldReturnFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var newPasswordHash = "newPasswordHash";

        _mockUserRepository
            .Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        var result = await service.ChangePasswordWithHistoryAsync(userId, newPasswordHash);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("User not found", result.ErrorMessage);
    }

    [Fact]
    public async Task ChangePasswordWithHistoryAsync_WhenUserExists_ShouldStoreCurrentPasswordInHistory()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var currentPasswordHash = "currentPasswordHash";
        var newPasswordHash = "newPasswordHash";

        var user = new User(
            new Email("test@example.com"),
            "John",
            "Doe",
            currentPasswordHash,
            UserType.Regular);

        _mockUserRepository
            .Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await service.ChangePasswordWithHistoryAsync(userId, newPasswordHash);

        // Assert
        Assert.True(result.IsSuccess);
        _mockPasswordHistoryRepository.Verify(
            x => x.AddAsync(It.Is<PasswordHistory>(ph => ph.UserId == userId && ph.PasswordHash == currentPasswordHash), It.IsAny<CancellationToken>()),
            Times.Once);
        _mockUserRepository.Verify(x => x.UpdateAsync(user, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ChangePasswordWithHistoryAsync_WhenPasswordHistoryDisabled_ShouldNotStoreInHistory()
    {
        // Arrange
        var config = Options.Create(new PasswordConfiguration { EnablePasswordHistory = false });
        var service = new PasswordHistoryService(
            _mockUserRepository.Object,
            _mockPasswordHistoryRepository.Object,
            config,
            _mockLogger.Object);

        var userId = Guid.NewGuid();
        var currentPasswordHash = "currentPasswordHash";
        var newPasswordHash = "newPasswordHash";

        var user = new User(
            new Email("test@example.com"),
            "John",
            "Doe",
            currentPasswordHash,
            UserType.Regular);

        _mockUserRepository
            .Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await service.ChangePasswordWithHistoryAsync(userId, newPasswordHash);

        // Assert
        Assert.True(result.IsSuccess);
        _mockPasswordHistoryRepository.Verify(
            x => x.AddAsync(It.IsAny<PasswordHistory>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CleanupPasswordHistoryAsync_WhenPasswordHistoryDisabled_ShouldNotPerformCleanup()
    {
        // Arrange
        var config = Options.Create(new PasswordConfiguration { EnablePasswordHistory = false });
        var service = new PasswordHistoryService(
            _mockUserRepository.Object,
            _mockPasswordHistoryRepository.Object,
            config,
            _mockLogger.Object);

        var userId = Guid.NewGuid();

        // Act
        await service.CleanupPasswordHistoryAsync(userId);

        // Assert
        _mockPasswordHistoryRepository.Verify(
            x => x.RemoveOldEntriesAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CleanupPasswordHistoryAsync_WhenPasswordHistoryEnabled_ShouldPerformCleanup()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        await service.CleanupPasswordHistoryAsync(userId);

        // Assert
        _mockPasswordHistoryRepository.Verify(
            x => x.RemoveOldEntriesAsync(userId, 365, 5, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CleanupAllPasswordHistoryAsync_ShouldCleanupAllUsers()
    {
        // Arrange
        var users = new[]
        {
            new User(new Email("user1@example.com"), "User", "One", "hash1", UserType.Regular),
            new User(new Email("user2@example.com"), "User", "Two", "hash2", UserType.Regular)
        };

        _mockUserRepository
            .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(users);

        // Act
        await service.CleanupAllPasswordHistoryAsync();

        // Assert
        _mockPasswordHistoryRepository.Verify(
            x => x.RemoveOldEntriesAsync(It.IsAny<Guid>(), 365, 5, It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }
}

public class PasswordHistoryEntityTests
{
    [Fact]
    public void PasswordHistory_Constructor_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var passwordHash = "testPasswordHash";

        // Act
        var passwordHistory = new PasswordHistory(userId, passwordHash);

        // Assert
        Assert.Equal(userId, passwordHistory.UserId);
        Assert.Equal(passwordHash, passwordHistory.PasswordHash);
        Assert.True(passwordHistory.CreatedAt <= DateTime.UtcNow);
        Assert.True(passwordHistory.CreatedAt > DateTime.UtcNow.AddMinutes(-1));
    }

    [Fact]
    public void IsWithinRetentionPeriod_WhenWithinPeriod_ShouldReturnTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var passwordHash = "testPasswordHash";
        var passwordHistory = new PasswordHistory(userId, passwordHash);
        
        // Simulate a password created 30 days ago by setting CreatedAt via reflection
        var createdAt = DateTime.UtcNow.AddDays(-30);
        typeof(PasswordHistory).GetProperty("CreatedAt")!
            .SetValue(passwordHistory, createdAt);

        // Act
        var result = passwordHistory.IsWithinRetentionPeriod(365);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsWithinRetentionPeriod_WhenOutsidePeriod_ShouldReturnFalse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var passwordHash = "testPasswordHash";
        var passwordHistory = new PasswordHistory(userId, passwordHash);
        
        // Simulate a password created 400 days ago
        var createdAt = DateTime.UtcNow.AddDays(-400);
        typeof(PasswordHistory).GetProperty("CreatedAt")!
            .SetValue(passwordHistory, createdAt);

        // Act
        var result = passwordHistory.IsWithinRetentionPeriod(365);

        // Assert
        Assert.False(result);
    }
}

public class UserPasswordHistoryTests
{
    [Fact]
    public void IsPasswordInHistory_WhenPasswordExists_ShouldReturnTrue()
    {
        // Arrange
        var user = new User(
            new Email("test@example.com"),
            "John",
            "Doe",
            "currentPasswordHash",
            UserType.Regular);

        // Add password to history using reflection to access private field
        var passwordHistoryField = typeof(User).GetField("_passwordHistory", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var passwordHistory = (List<PasswordHistory>)passwordHistoryField!.GetValue(user)!;
        
        passwordHistory.Add(new PasswordHistory(user.Id, "oldPasswordHash1"));
        passwordHistory.Add(new PasswordHistory(user.Id, "oldPasswordHash2"));

        // Act
        var result = user.IsPasswordInHistory("oldPasswordHash1");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsPasswordInHistory_WhenPasswordDoesNotExist_ShouldReturnFalse()
    {
        // Arrange
        var user = new User(
            new Email("test@example.com"),
            "John",
            "Doe",
            "currentPasswordHash",
            UserType.Regular);

        // Add password to history using reflection
        var passwordHistoryField = typeof(User).GetField("_passwordHistory", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var passwordHistory = (List<PasswordHistory>)passwordHistoryField!.GetValue(user)!;
        
        passwordHistory.Add(new PasswordHistory(user.Id, "oldPasswordHash1"));

        // Act
        var result = user.IsPasswordInHistory("nonExistentPasswordHash");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ChangePasswordWithHistory_ShouldStoreCurrentPasswordInHistory()
    {
        // Arrange
        var currentPasswordHash = "currentPasswordHash";
        var newPasswordHash = "newPasswordHash";
        
        var user = new User(
            new Email("test@example.com"),
            "John",
            "Doe",
            currentPasswordHash,
            UserType.Regular);

        // Act
        user.ChangePasswordWithHistory(newPasswordHash, storeCurrentPasswordInHistory: true);

        // Assert
        Assert.Equal(newPasswordHash, user.PasswordHash);
        Assert.Single(user.PasswordHistory);
        Assert.Equal(currentPasswordHash, user.PasswordHistory.First().PasswordHash);
    }

    [Fact]
    public void ChangePasswordWithHistory_WhenNotStoringInHistory_ShouldNotStoreCurrentPassword()
    {
        // Arrange
        var currentPasswordHash = "currentPasswordHash";
        var newPasswordHash = "newPasswordHash";
        
        var user = new User(
            new Email("test@example.com"),
            "John",
            "Doe",
            currentPasswordHash,
            UserType.Regular);

        // Act
        user.ChangePasswordWithHistory(newPasswordHash, storeCurrentPasswordInHistory: false);

        // Assert
        Assert.Equal(newPasswordHash, user.PasswordHash);
        Assert.Empty(user.PasswordHistory);
    }
}
