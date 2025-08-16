using Identity.Domain.Entities;
using Identity.Domain.Repositories;
using Identity.Domain.Services;
using Identity.Infrastructure.Services;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Identity.Tests.Infrastructure.Services;

public class SecurityNotificationServiceTests
{
    private readonly Mock<IDiscordNotificationService> _mockDiscordService;
    private readonly Mock<IEmailService> _mockEmailService;
    private readonly Mock<ISmsService> _mockSmsService;
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<ISecurityEventRepository> _mockSecurityEventRepository;
    private readonly Mock<ISuspiciousActivityRepository> _mockSuspiciousActivityRepository;
    private readonly Mock<IAccountLockoutRepository> _mockAccountLockoutRepository;
    private readonly Mock<IDistributedCache> _mockCache;
    private readonly Mock<ILogger<SecurityNotificationService>> _mockLogger;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly SecurityNotificationService _service;

    public SecurityNotificationServiceTests()
    {
        _mockDiscordService = new Mock<IDiscordNotificationService>();
        _mockEmailService = new Mock<IEmailService>();
        _mockSmsService = new Mock<ISmsService>();
        _mockUserRepository = new Mock<IUserRepository>();
        _mockSecurityEventRepository = new Mock<ISecurityEventRepository>();
        _mockSuspiciousActivityRepository = new Mock<ISuspiciousActivityRepository>();
        _mockAccountLockoutRepository = new Mock<IAccountLockoutRepository>();
        _mockCache = new Mock<IDistributedCache>();
        _mockLogger = new Mock<ILogger<SecurityNotificationService>>();
        _mockConfiguration = new Mock<IConfiguration>();

        // Setup default configuration
        SetupConfiguration();

        _service = new SecurityNotificationService(
            _mockDiscordService.Object,
            _mockEmailService.Object,
            _mockSmsService.Object,
            _mockUserRepository.Object,
            _mockSecurityEventRepository.Object,
            _mockSuspiciousActivityRepository.Object,
            _mockAccountLockoutRepository.Object,
            _mockCache.Object,
            _mockLogger.Object,
            _mockConfiguration.Object);
    }

    [Fact]
    public async Task SendSecurityEventNotificationAsync_WithCriticalEvent_SendsDiscordNotification()
    {
        // Arrange
        var securityEvent = SecurityEvent.CreateAccountLockout(
            Guid.NewGuid(),
            "192.168.1.100",
            "Too many failed login attempts");

        _mockCache.Setup(c => c.GetStringAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        // Act
        await _service.SendSecurityEventNotificationAsync(securityEvent);

        // Assert
        _mockDiscordService.Verify(s => s.SendSecurityAlertAsync(securityEvent, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendSecurityEventNotificationAsync_WithThrottledEvent_DoesNotSendNotification()
    {
        // Arrange
        var securityEvent = SecurityEvent.CreateLoginAttempt(
            Guid.NewGuid(),
            "192.168.1.100",
            "Mozilla/5.0",
            false,
            "Invalid password");

        _mockCache.Setup(c => c.GetStringAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("10"); // Already at limit

        // Act
        await _service.SendSecurityEventNotificationAsync(securityEvent);

        // Assert
        _mockDiscordService.Verify(s => s.SendSecurityAlertAsync(It.IsAny<SecurityEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SendSuspiciousActivityNotificationAsync_WithHighRiskActivity_SendsNotification()
    {
        // Arrange
        var suspiciousActivity = new SuspiciousActivity(
            Guid.NewGuid(),
            "BRUTE_FORCE_ATTACK",
            "Multiple failed login attempts detected",
            "192.168.1.100",
            85.0);

        _mockCache.Setup(c => c.GetStringAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        // Act
        await _service.SendSuspiciousActivityNotificationAsync(suspiciousActivity);

        // Assert
        _mockDiscordService.Verify(s => s.SendSuspiciousActivityAlertAsync(suspiciousActivity, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendSuspiciousActivityNotificationAsync_WithLowRiskActivity_DoesNotSendNotification()
    {
        // Arrange
        var suspiciousActivity = new SuspiciousActivity(
            Guid.NewGuid(),
            "UNUSUAL_USER_AGENT",
            "Uncommon browser detected",
            "192.168.1.100",
            30.0);

        // Act
        await _service.SendSuspiciousActivityNotificationAsync(suspiciousActivity);

        // Assert
        _mockDiscordService.Verify(s => s.SendSuspiciousActivityAlertAsync(It.IsAny<SuspiciousActivity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SendAccountLockoutNotificationAsync_WithLockout_SendsNotificationAndEmailsUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var accountLockout = new AccountLockout(
            userId,
            "Too many failed attempts",
            5,
            "192.168.1.100",
            DateTime.UtcNow,
            DateTime.UtcNow.AddMinutes(30));

        var user = CreateTestUser(userId, "test@example.com");
        _mockUserRepository.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        await _service.SendAccountLockoutNotificationAsync(accountLockout);

        // Assert
        _mockDiscordService.Verify(s => s.SendAccountLockoutAlertAsync(accountLockout, It.IsAny<CancellationToken>()), Times.Once);
        _mockEmailService.Verify(e => e.SendSecurityAlertAsync(
            "test@example.com", 
            It.Is<string>(msg => msg.Contains("temporarily locked")), 
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendCriticalSecurityAlertAsync_WithAlert_SendsToAllChannels()
    {
        // Arrange
        var message = "Critical security breach detected";
        var context = "Multiple unauthorized access attempts";

        // Act
        await _service.SendCriticalSecurityAlertAsync(message, context);

        // Assert
        _mockDiscordService.Verify(s => s.SendCriticalAlertAsync(
            "Security Alert", 
            message, 
            context, 
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendSecuritySummaryAsync_WithDateRange_GeneratesAndSendsSummary()
    {
        // Arrange
        var from = DateTime.UtcNow.Date.AddDays(-1);
        var to = DateTime.UtcNow.Date;

        var events = new List<SecurityEvent>
        {
            SecurityEvent.CreateLoginAttempt(Guid.NewGuid(), "192.168.1.100", "Mozilla/5.0", true),
            SecurityEvent.CreateLoginAttempt(Guid.NewGuid(), "192.168.1.101", "Mozilla/5.0", false, "Invalid password"),
            SecurityEvent.CreateAccountLockout(Guid.NewGuid(), "192.168.1.102", "Too many failed attempts")
        };

        var suspiciousActivities = new List<SuspiciousActivity>
        {
            new(Guid.NewGuid(), "BRUTE_FORCE", "Attack detected", "192.168.1.100", 80.0)
        };

        var accountLockouts = new List<AccountLockout>
        {
            new(Guid.NewGuid(), "Failed attempts", 5, "192.168.1.100", DateTime.UtcNow, DateTime.UtcNow.AddMinutes(30))
        };

        _mockSecurityEventRepository.Setup(r => r.GetEventsAsync(null, from, to, It.IsAny<CancellationToken>()))
            .ReturnsAsync(events);
        _mockSuspiciousActivityRepository.Setup(r => r.GetActivitiesAsync(null, from, to, It.IsAny<CancellationToken>()))
            .ReturnsAsync(suspiciousActivities);
        _mockAccountLockoutRepository.Setup(r => r.GetLockoutsAsync(null, from, to, It.IsAny<CancellationToken>()))
            .ReturnsAsync(accountLockouts);

        // Act
        await _service.SendSecuritySummaryAsync(from, to);

        // Assert
        _mockDiscordService.Verify(s => s.SendDailySummaryAsync(
            It.Is<SecuritySummary>(summary => 
                summary.TotalEvents == 3 &&
                summary.CriticalEvents == 1 &&
                summary.SuspiciousActivities == 1 &&
                summary.AccountLockouts == 1), 
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendSecurityEventNotificationAsync_WhenDisabled_DoesNotSendNotification()
    {
        // Arrange
        var securityEvent = SecurityEvent.CreateAccountLockout(
            Guid.NewGuid(),
            "192.168.1.100",
            "Too many failed login attempts");

        // Setup disabled configuration
        var mockConfigSection = new Mock<IConfigurationSection>();
        mockConfigSection.Setup(s => s.Value).Returns("false");
        _mockConfiguration.Setup(c => c.GetSection("Notifications:SecurityEvents:Enabled"))
            .Returns(mockConfigSection.Object);

        var disabledService = new SecurityNotificationService(
            _mockDiscordService.Object,
            _mockEmailService.Object,
            _mockSmsService.Object,
            _mockUserRepository.Object,
            _mockSecurityEventRepository.Object,
            _mockSuspiciousActivityRepository.Object,
            _mockAccountLockoutRepository.Object,
            _mockCache.Object,
            _mockLogger.Object,
            _mockConfiguration.Object);

        // Act
        await disabledService.SendSecurityEventNotificationAsync(securityEvent);

        // Assert
        _mockDiscordService.Verify(s => s.SendSecurityAlertAsync(It.IsAny<SecurityEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private void SetupConfiguration()
    {
        var mockConfigSection = new Mock<IConfigurationSection>();
        mockConfigSection.Setup(s => s.Value).Returns("true");
        _mockConfiguration.Setup(c => c.GetSection("Notifications:SecurityEvents:Enabled"))
            .Returns(mockConfigSection.Object);

        _mockConfiguration.Setup(c => c.GetSection("Notifications:SecurityEvents"))
            .Returns(new Mock<IConfigurationSection>().Object);
    }

    private static Domain.Entities.User CreateTestUser(Guid userId, string email)
    {
        return new Domain.Entities.User(
            new Domain.ValueObjects.Email(email),
            "Test",
            "User",
            Domain.Enums.UserType.Fan,
            null);
    }
}
