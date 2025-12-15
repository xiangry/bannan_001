using MathComicGenerator.Api.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace MathComicGenerator.Tests.Services;

public class ResourceManagementServiceTests : IDisposable
{
    private readonly Mock<ILogger<ResourceManagementService>> _mockLogger;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<IConfigurationSection> _mockConfigSection;
    private readonly ResourceManagementService _service;

    public ResourceManagementServiceTests()
    {
        _mockLogger = new Mock<ILogger<ResourceManagementService>>();
        _mockConfiguration = new Mock<IConfiguration>();
        _mockConfigSection = new Mock<IConfigurationSection>();
        
        SetupConfiguration();
        _service = new ResourceManagementService(_mockLogger.Object, _mockConfiguration.Object);
    }

    private void SetupConfiguration()
    {
        _mockConfigSection.Setup(x => x["MaxConcurrentRequests"]).Returns("5");
        _mockConfigSection.Setup(x => x["RequestTimeoutMs"]).Returns("5000");
        _mockConfigSection.Setup(x => x["MaxMemoryUsagePercent"]).Returns("80");
        _mockConfigSection.Setup(x => x["MaxCpuUsagePercent"]).Returns("80");
        _mockConfigSection.Setup(x => x["MaxDiskUsagePercent"]).Returns("90");
        _mockConfigSection.Setup(x => x["MonitoringIntervalSeconds"]).Returns("30");
        _mockConfigSection.Setup(x => x["EnableGracefulDegradation"]).Returns("true");
        _mockConfigSection.Setup(x => x["EnableAutoRecovery"]).Returns("true");
        
        _mockConfiguration.Setup(x => x.GetSection("ResourceManagement")).Returns(_mockConfigSection.Object);
    }

    [Fact]
    public async Task TryAcquireResourceAsync_WhenResourcesAvailable_ReturnsTrue()
    {
        // Act
        var result = await _service.TryAcquireResourceAsync();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task TryAcquireResourceAsync_WhenMaxConcurrentReached_ThrowsException()
    {
        // Arrange - 获取所有可用资源
        var tasks = new List<Task<bool>>();
        for (int i = 0; i < 5; i++) // MaxConcurrentRequests = 5
        {
            tasks.Add(_service.TryAcquireResourceAsync());
        }
        await Task.WhenAll(tasks);

        // Act & Assert - 尝试获取超出限制的资源
        await Assert.ThrowsAsync<ResourceLimitException>(() => 
            _service.TryAcquireResourceAsync(new CancellationTokenSource(100).Token));
    }

    [Fact]
    public void ReleaseResource_AfterAcquire_IncreasesAvailableSlots()
    {
        // Arrange
        var initialHealth = _service.GetSystemHealth();
        var initialSlots = initialHealth.AvailableRequestSlots;

        // Act
        _service.TryAcquireResourceAsync().Wait();
        var afterAcquireHealth = _service.GetSystemHealth();
        
        _service.ReleaseResource();
        var afterReleaseHealth = _service.GetSystemHealth();

        // Assert
        Assert.Equal(initialSlots - 1, afterAcquireHealth.AvailableRequestSlots);
        Assert.Equal(initialSlots, afterReleaseHealth.AvailableRequestSlots);
    }

    [Fact]
    public void GetSystemHealth_ReturnsValidHealthStatus()
    {
        // Act
        var health = _service.GetSystemHealth();

        // Assert
        Assert.NotNull(health);
        Assert.True(health.MemoryUsagePercent >= 0);
        Assert.True(health.CpuUsagePercent >= 0);
        Assert.True(health.DiskUsagePercent >= 0);
        Assert.True(health.AvailableRequestSlots >= 0);
        Assert.Equal(5, health.MaxConcurrentRequests); // From configuration
        Assert.True(health.Timestamp <= DateTime.UtcNow);
    }

    [Fact]
    public async Task CheckResourceAvailabilityAsync_WithNormalUsage_ReturnsTrue()
    {
        // Act
        var result = await _service.CheckResourceAvailabilityAsync();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task PerformGracefulDegradationAsync_CompletesSuccessfully()
    {
        // Act & Assert - Should not throw
        await _service.PerformGracefulDegradationAsync();
    }

    [Fact]
    public async Task AttemptSystemRecoveryAsync_ReturnsResult()
    {
        // Act
        var result = await _service.AttemptSystemRecoveryAsync();

        // Assert
        Assert.IsType<bool>(result);
    }

    [Fact]
    public void ResourceConfiguration_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var config = new ResourceConfiguration();

        // Assert
        Assert.Equal(10, config.MaxConcurrentRequests);
        Assert.Equal(30000, config.RequestTimeoutMs);
        Assert.Equal(80.0, config.MaxMemoryUsagePercent);
        Assert.Equal(80.0, config.MaxCpuUsagePercent);
        Assert.Equal(90.0, config.MaxDiskUsagePercent);
        Assert.Equal(30, config.MonitoringIntervalSeconds);
        Assert.True(config.EnableGracefulDegradation);
        Assert.True(config.EnableAutoRecovery);
    }

    [Fact]
    public void ResourceLimitException_WithMessage_SetsMessageCorrectly()
    {
        // Arrange & Act
        var exception = new ResourceLimitException("Test message");

        // Assert
        Assert.Equal("Test message", exception.Message);
    }

    [Fact]
    public void ResourceLimitException_WithMessageAndInnerException_SetsPropertiesCorrectly()
    {
        // Arrange
        var innerException = new InvalidOperationException("Inner exception");

        // Act
        var exception = new ResourceLimitException("Test message", innerException);

        // Assert
        Assert.Equal("Test message", exception.Message);
        Assert.Equal(innerException, exception.InnerException);
    }

    public void Dispose()
    {
        _service?.Dispose();
    }
}