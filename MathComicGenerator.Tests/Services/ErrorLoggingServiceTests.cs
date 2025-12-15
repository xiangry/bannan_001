using MathComicGenerator.Api.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace MathComicGenerator.Tests.Services;

public class ErrorLoggingServiceTests : IDisposable
{
    private readonly Mock<ILogger<ErrorLoggingService>> _mockLogger;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly ErrorLoggingService _service;
    private readonly string _testLogPath;

    public ErrorLoggingServiceTests()
    {
        _mockLogger = new Mock<ILogger<ErrorLoggingService>>();
        _mockConfiguration = new Mock<IConfiguration>();
        
        // 创建临时测试目录
        _testLogPath = Path.Combine(Path.GetTempPath(), "ErrorLoggingTests", Guid.NewGuid().ToString());
        
        SetupConfiguration();
        _service = new ErrorLoggingService(_mockLogger.Object, _mockConfiguration.Object);
    }

    private void SetupConfiguration()
    {
        _mockConfiguration.Setup(x => x["Logging:ErrorLogPath"])
                         .Returns(_testLogPath);
    }

    [Fact]
    public async Task LogErrorAsync_WithException_CreatesLogEntry()
    {
        // Arrange
        var exception = new ArgumentException("Test exception");
        var context = "Test context";
        var additionalData = new Dictionary<string, object> { { "key", "value" } };

        // Act
        await _service.LogErrorAsync(exception, context, additionalData);

        // Assert
        var errorFiles = Directory.GetFiles(Path.Combine(_testLogPath, "errors"), "*.json");
        Assert.Single(errorFiles);
    }

    [Fact]
    public async Task LogErrorAsync_WithNullContext_DoesNotThrow()
    {
        // Arrange
        var exception = new InvalidOperationException("Test exception");

        // Act & Assert - Should not throw
        await _service.LogErrorAsync(exception);
    }

    [Fact]
    public async Task GetRecentErrorsAsync_WithLoggedErrors_ReturnsErrors()
    {
        // Arrange
        var exception1 = new ArgumentException("First exception");
        var exception2 = new InvalidOperationException("Second exception");
        
        await _service.LogErrorAsync(exception1, "Context 1");
        await _service.LogErrorAsync(exception2, "Context 2");

        // Act
        var errors = await _service.GetRecentErrorsAsync();

        // Assert
        Assert.Equal(2, errors.Count);
        Assert.Contains(errors, e => e.Message == "First exception");
        Assert.Contains(errors, e => e.Message == "Second exception");
    }

    [Fact]
    public async Task GetRecentErrorsAsync_WithNoErrors_ReturnsEmptyList()
    {
        // Act
        var errors = await _service.GetRecentErrorsAsync();

        // Assert
        Assert.Empty(errors);
    }

    [Fact]
    public async Task GetErrorStatisticsAsync_WithLoggedErrors_ReturnsStatistics()
    {
        // Arrange
        var exception1 = new ArgumentException("Test exception 1");
        var exception2 = new ArgumentException("Test exception 2");
        var exception3 = new InvalidOperationException("Test exception 3");
        
        await _service.LogErrorAsync(exception1);
        await _service.LogErrorAsync(exception2);
        await _service.LogErrorAsync(exception3);

        // Act
        var stats = await _service.GetErrorStatisticsAsync(TimeSpan.FromHours(1));

        // Assert
        Assert.Equal(3, stats.TotalErrors);
        Assert.True(stats.ErrorsByType.ContainsKey("ArgumentException"));
        Assert.True(stats.ErrorsByType.ContainsKey("InvalidOperationException"));
        Assert.Equal(2, stats.ErrorsByType["ArgumentException"]);
        Assert.Equal(1, stats.ErrorsByType["InvalidOperationException"]);
    }

    [Fact]
    public async Task CleanupOldErrorsAsync_WithOldFiles_RemovesOldFiles()
    {
        // Arrange
        var exception = new Exception("Test exception");
        await _service.LogErrorAsync(exception);

        // 模拟旧文件 - 修改文件创建时间
        var errorDir = Path.Combine(_testLogPath, "errors");
        var files = Directory.GetFiles(errorDir, "*.json");
        foreach (var file in files)
        {
            File.SetCreationTime(file, DateTime.UtcNow.AddDays(-2));
        }

        // Act
        await _service.CleanupOldErrorsAsync(TimeSpan.FromDays(1));

        // Assert
        var remainingFiles = Directory.GetFiles(errorDir, "*.json");
        Assert.Empty(remainingFiles);
    }

    [Fact]
    public void ErrorLogEntry_Properties_SetCorrectly()
    {
        // Arrange & Act
        var entry = new ErrorLogEntry
        {
            Id = "test-id",
            Timestamp = DateTime.UtcNow,
            ExceptionType = "TestException",
            Message = "Test message",
            StackTrace = "Test stack trace",
            Context = "Test context",
            AdditionalData = new Dictionary<string, object> { { "key", "value" } },
            InnerException = "Inner exception message"
        };

        // Assert
        Assert.Equal("test-id", entry.Id);
        Assert.Equal("TestException", entry.ExceptionType);
        Assert.Equal("Test message", entry.Message);
        Assert.Equal("Test stack trace", entry.StackTrace);
        Assert.Equal("Test context", entry.Context);
        Assert.Equal("Inner exception message", entry.InnerException);
        Assert.Single(entry.AdditionalData);
    }

    [Fact]
    public void ErrorStatistics_Properties_SetCorrectly()
    {
        // Arrange & Act
        var stats = new ErrorStatistics
        {
            TotalErrors = 10,
            ErrorsByType = new Dictionary<string, int> { { "Exception", 5 } },
            ErrorsByHour = new Dictionary<int, int> { { 14, 3 } },
            MostCommonErrors = new Dictionary<string, int> { { "Common error", 2 } },
            Period = TimeSpan.FromHours(1),
            GeneratedAt = DateTime.UtcNow
        };

        // Assert
        Assert.Equal(10, stats.TotalErrors);
        Assert.Single(stats.ErrorsByType);
        Assert.Single(stats.ErrorsByHour);
        Assert.Single(stats.MostCommonErrors);
        Assert.Equal(TimeSpan.FromHours(1), stats.Period);
    }

    public void Dispose()
    {
        _service?.Dispose();
        
        // 清理测试数据
        if (Directory.Exists(_testLogPath))
        {
            try
            {
                Directory.Delete(_testLogPath, true);
            }
            catch
            {
                // 忽略清理错误
            }
        }
    }
}