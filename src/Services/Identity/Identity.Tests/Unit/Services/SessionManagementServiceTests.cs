using Identity.Application.DTOs;
using Identity.Domain.Services;
using Identity.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Identity.Tests.Unit.Services;

public class SessionManagementServiceTests
{
    private readonly Mock<IUserSessionRepository> _mockSessionRepository;
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<ITokenService> _mockTokenService;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly ILogger<SessionManagementService> _logger;
    private readonly SessionManagementService _sessionManagementService;

    public SessionManagementServiceTests()
    {
        _mockSessionRepository = new Mock<IUserSessionRepository>();
        _mockUserRepository = new Mock<IUserRepository>();
        _mockTokenService = new Mock<ITokenService>();
        _mockConfiguration = new Mock<IConfiguration>();
        _logger = NullLogger<SessionManagementService>.Instance;

        // Setup configuration
        var mockSection = new Mock<IConfigurationSection>();
        mockSection.Setup(x => x["MaxConcurrentSessions"]).Returns("5");
        mockSection.Setup(x => x["SessionLimitBehavior"]).Returns("RevokeOldest");
        mockSection.Setup(x => x["EnableSessionLimits"]).Returns("true");
        _mockConfiguration.Setup(x => x.GetSection("Security")).Returns(mockSection.Object);

        _sessionManagementService = new SessionManagementService(
            _mockSessionRepository.Object,
            _mockUserRepository.Object,
            _mockTokenService.Object,
            _mockConfiguration.Object,
            _logger);
    }

    [Fact]
    public async Task CanCreateSessionAsync_WithinLimit_ReturnsTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _mockSessionRepository.Setup(x => x.GetActiveSessionCountAsync(userId, default))
            .ReturnsAsync(3);

        // Act
        var result = await _sessionManagementService.CanCreateSessionAsync(userId);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task GetActiveSessionCountAsync_ReturnsCorrectCount()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var sessions = new List<UserSession>
        {
            new UserSession(userId, "Device 1", "192.168.1.1"),
            new UserSession(userId, "Device 2", "192.168.1.2")
        };

        _mockSessionRepository.Setup(x => x.GetActiveSessionsByUserIdAsync(userId, default))
            .ReturnsAsync(sessions);

        // Act
        var result = await _sessionManagementService.GetActiveSessionCountAsync(userId);

        // Assert
        Assert.Equal(2, result);
    }

    [Fact]
    public async Task GetMaxAllowedSessionsAsync_ForRegularUser_ReturnsBaseLimit()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User(
            new Email("test@example.com"),
            "Test",
            "User",
            "hashedPassword",
            UserType.Fan);

        _mockUserRepository.Setup(x => x.GetByIdAsync(userId, default))
            .ReturnsAsync(user);

        // Act
        var result = await _sessionManagementService.GetMaxAllowedSessionsAsync(userId);

        // Assert
        Assert.Equal(5, result); // Base limit from configuration
    }
}
