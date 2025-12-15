using MathComicGenerator.Api.Services;
using MathComicGenerator.Shared.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using System.Net;
using System.Text.Json;

namespace MathComicGenerator.Tests.Services;

public class GeminiAPIServiceTests
{
    private readonly Mock<ILogger<GeminiAPIService>> _mockLogger;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<IConfigurationSection> _mockConfigSection;
    private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
    private readonly HttpClient _httpClient;

    public GeminiAPIServiceTests()
    {
        _mockLogger = new Mock<ILogger<GeminiAPIService>>();
        _mockConfiguration = new Mock<IConfiguration>();
        _mockConfigSection = new Mock<IConfigurationSection>();
        _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        
        _httpClient = new HttpClient(_mockHttpMessageHandler.Object);
        
        SetupConfiguration();
    }

    private void SetupConfiguration()
    {
        _mockConfigSection.Setup(x => x["BaseUrl"]).Returns("https://test-api.com");
        _mockConfigSection.Setup(x => x["ApiKey"]).Returns("test-key");
        _mockConfigSection.Setup(x => x["TimeoutSeconds"]).Returns("30");
        _mockConfigSection.Setup(x => x["MaxRetries"]).Returns("3");
        
        _mockConfiguration.Setup(x => x.GetSection("GeminiAPI")).Returns(_mockConfigSection.Object);
    }

    [Fact]
    public async Task HandleAPIErrorAsync_TimeoutError_ReturnsCorrectResponse()
    {
        // Arrange
        var service = new GeminiAPIService(_httpClient, _mockLogger.Object, _mockConfiguration.Object);
        var apiError = new APIError
        {
            ErrorCode = "TIMEOUT",
            Message = "Request timeout"
        };

        // Act
        var result = await service.HandleAPIErrorAsync(apiError);

        // Assert
        Assert.Equal("请求超时，请稍后重试", result.UserMessage);
        Assert.True(result.ShouldRetry);
        Assert.Equal(TimeSpan.FromSeconds(30), result.RetryAfter);
    }

    [Fact]
    public async Task HandleAPIErrorAsync_RateLimitError_ReturnsCorrectResponse()
    {
        // Arrange
        var service = new GeminiAPIService(_httpClient, _mockLogger.Object, _mockConfiguration.Object);
        var apiError = new APIError
        {
            ErrorCode = "RATE_LIMIT",
            Message = "Rate limit exceeded"
        };

        // Act
        var result = await service.HandleAPIErrorAsync(apiError);

        // Assert
        Assert.Equal("请求过于频繁，请稍后重试", result.UserMessage);
        Assert.True(result.ShouldRetry);
        Assert.Equal(TimeSpan.FromMinutes(1), result.RetryAfter);
    }

    [Fact]
    public async Task HandleAPIErrorAsync_QuotaExceededError_ReturnsNoRetry()
    {
        // Arrange
        var service = new GeminiAPIService(_httpClient, _mockLogger.Object, _mockConfiguration.Object);
        var apiError = new APIError
        {
            ErrorCode = "QUOTA_EXCEEDED",
            Message = "Quota exceeded"
        };

        // Act
        var result = await service.HandleAPIErrorAsync(apiError);

        // Assert
        Assert.Equal("API配额已用完，请联系管理员", result.UserMessage);
        Assert.False(result.ShouldRetry);
    }

    [Fact]
    public async Task HandleAPIErrorAsync_InvalidRequestError_ReturnsNoRetry()
    {
        // Arrange
        var service = new GeminiAPIService(_httpClient, _mockLogger.Object, _mockConfiguration.Object);
        var apiError = new APIError
        {
            ErrorCode = "INVALID_REQUEST",
            Message = "Invalid request format"
        };

        // Act
        var result = await service.HandleAPIErrorAsync(apiError);

        // Assert
        Assert.Equal("请求格式错误，请检查输入内容", result.UserMessage);
        Assert.False(result.ShouldRetry);
    }

    [Fact]
    public async Task HandleAPIErrorAsync_NetworkError_ReturnsCorrectResponse()
    {
        // Arrange
        var service = new GeminiAPIService(_httpClient, _mockLogger.Object, _mockConfiguration.Object);
        var apiError = new APIError
        {
            ErrorCode = "NETWORK_ERROR",
            Message = "Network connection failed"
        };

        // Act
        var result = await service.HandleAPIErrorAsync(apiError);

        // Assert
        Assert.Equal("网络连接错误，请检查网络连接", result.UserMessage);
        Assert.True(result.ShouldRetry);
        Assert.Equal(TimeSpan.FromSeconds(10), result.RetryAfter);
    }

    [Fact]
    public async Task HandleAPIErrorAsync_UnknownError_ReturnsDefaultResponse()
    {
        // Arrange
        var service = new GeminiAPIService(_httpClient, _mockLogger.Object, _mockConfiguration.Object);
        var apiError = new APIError
        {
            ErrorCode = "UNKNOWN_ERROR",
            Message = "Unknown error occurred"
        };

        // Act
        var result = await service.HandleAPIErrorAsync(apiError);

        // Assert
        Assert.Equal("系统暂时不可用，请稍后重试", result.UserMessage);
        Assert.True(result.ShouldRetry);
        Assert.Equal(TimeSpan.FromSeconds(60), result.RetryAfter);
    }

    [Fact]
    public void GeminiAPIException_SetsErrorCodeCorrectly()
    {
        // Arrange & Act
        var exception = new GeminiAPIException("Test message", "TEST_ERROR");

        // Assert
        Assert.Equal("Test message", exception.Message);
        Assert.Equal("TEST_ERROR", exception.ErrorCode);
    }

    [Fact]
    public void GeminiAPIConfiguration_HasDefaultValues()
    {
        // Arrange & Act
        var config = new GeminiAPIConfiguration();

        // Assert
        Assert.Equal("https://generativelanguage.googleapis.com", config.BaseUrl);
        Assert.Equal("", config.ApiKey);
        Assert.Equal(30, config.TimeoutSeconds);
        Assert.Equal(3, config.MaxRetries);
    }
}