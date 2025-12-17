using MathComicGenerator.Api.Services;
using MathComicGenerator.Shared.Interfaces;
using MathComicGenerator.Shared.Models;
using Microsoft.Extensions.Logging;
using Moq;

namespace MathComicGenerator.Tests.Services;

public class ComicGenerationServiceTests
{
    private readonly Mock<IGeminiAPIService> _mockGeminiService;
    private readonly Mock<ILogger<ComicGenerationService>> _mockLogger;
    private readonly ComicGenerationService _service;

    public ComicGenerationServiceTests()
    {
        _mockGeminiService = new Mock<IGeminiAPIService>();
        _mockLogger = new Mock<ILogger<ComicGenerationService>>();
        
        _service = new ComicGenerationService(_mockGeminiService.Object, _mockLogger.Object);
    }

    [Fact]
    public void ValidateConcept_EmptyInput_ReturnsInvalid()
    {
        // Act
        var result = _service.ValidateConcept("");

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("请输入学习内容", result.ErrorMessage);
    }

    [Fact]
    public void ValidateConcept_ValidMathConcept_ReturnsValid()
    {
        // Act
        var result = _service.ValidateConcept("加法运算");

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task GenerateComicAsync_InvalidPanelCount_ThrowsException()
    {
        // Arrange
        var concept = new MathConcept { Topic = "加法运算" };
        var options = new GenerationOptions { PanelCount = 2 }; // Invalid: less than 3

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _service.GenerateComicAsync(concept, options));
    }

    [Fact]
    public async Task GenerateComicAsync_ValidInput_ReturnsComic()
    {
        // Arrange
        var concept = new MathConcept 
        { 
            Topic = "加法运算",
            Keywords = new List<string> { "加法", "运算" }
        };
        var options = new GenerationOptions { PanelCount = 4 };

        var mockComicContent = new ComicContent
        {
            Title = "加法学习",
            Panels = new List<PanelContent>
            {
                new PanelContent 
                { 
                    ImageDescription = "小明看到苹果",
                    Dialogue = new List<string> { "我有2个苹果" }
                },
                new PanelContent 
                { 
                    ImageDescription = "小红给小明苹果",
                    Dialogue = new List<string> { "我再给你3个苹果" }
                },
                new PanelContent 
                { 
                    ImageDescription = "小明数苹果",
                    Dialogue = new List<string> { "让我数一数" }
                },
                new PanelContent 
                { 
                    ImageDescription = "小明高兴地说",
                    Dialogue = new List<string> { "总共有5个苹果！" }
                }
            }
        };

        _mockGeminiService.Setup(x => x.GenerateComicContentAsync(It.IsAny<string>()))
            .ReturnsAsync(mockComicContent);

        // Act
        var result = await _service.GenerateComicAsync(concept, options);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("加法学习", result.Title);
        Assert.Equal(4, result.Panels.Count);
        Assert.Equal("加法运算", result.Metadata.MathConcept);
        Assert.All(result.Panels, panel => Assert.True(panel.Order >= 1 && panel.Order <= 4));
    }

    [Fact]
    public async Task GenerateComicAsync_InvalidConcept_ThrowsException()
    {
        // Arrange
        var concept = new MathConcept { Topic = "" }; // Invalid: empty topic
        var options = new GenerationOptions { PanelCount = 4 };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _service.GenerateComicAsync(concept, options));
    }

    [Fact]
    public void ContentSafetyFilter_FilterText_ReplacesUnsafeContent()
    {
        // Arrange
        var filter = new ContentSafetyFilter();
        var unsafeText = "小朋友们不要打架，要好好学习";

        // Act
        var result = filter.FilterText(unsafeText);

        // Assert
        Assert.Equal("小朋友们不要讨论，要好好学习", result);
    }

    [Fact]
    public void ContentSafetyFilter_FilterText_HandlesEmptyInput()
    {
        // Arrange
        var filter = new ContentSafetyFilter();

        // Act
        var result = filter.FilterText("");

        // Assert
        Assert.Equal("", result);
    }

    [Fact]
    public void ContentSafetyFilter_FilterText_HandlesNullInput()
    {
        // Arrange
        var filter = new ContentSafetyFilter();

        // Act
        var result = filter.FilterText(null!);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GenerateComicAsync_TooManyPanels_TrimsToCorrectCount()
    {
        // Arrange
        var concept = new MathConcept 
        { 
            Topic = "加法运算",
            Keywords = new List<string> { "加法" }
        };
        var options = new GenerationOptions { PanelCount = 3 };

        var mockComicContent = new ComicContent
        {
            Title = "加法学习",
            Panels = new List<PanelContent>
            {
                new PanelContent { ImageDescription = "Panel 1", Dialogue = new List<string> { "对话1" } },
                new PanelContent { ImageDescription = "Panel 2", Dialogue = new List<string> { "对话2" } },
                new PanelContent { ImageDescription = "Panel 3", Dialogue = new List<string> { "对话3" } },
                new PanelContent { ImageDescription = "Panel 4", Dialogue = new List<string> { "对话4" } },
                new PanelContent { ImageDescription = "Panel 5", Dialogue = new List<string> { "对话5" } }
            }
        };

        _mockGeminiService.Setup(x => x.GenerateComicContentAsync(It.IsAny<string>()))
            .ReturnsAsync(mockComicContent);

        // Act
        var result = await _service.GenerateComicAsync(concept, options);

        // Assert
        Assert.Equal(3, result.Panels.Count);
    }

    [Fact]
    public async Task GenerateComicAsync_TooFewPanels_AddsSupplementaryPanels()
    {
        // Arrange
        var concept = new MathConcept 
        { 
            Topic = "加法运算",
            Keywords = new List<string> { "加法" }
        };
        var options = new GenerationOptions { PanelCount = 4 };

        var mockComicContent = new ComicContent
        {
            Title = "加法学习",
            Panels = new List<PanelContent>
            {
                new PanelContent { ImageDescription = "Panel 1", Dialogue = new List<string> { "对话1" } },
                new PanelContent { ImageDescription = "Panel 2", Dialogue = new List<string> { "对话2" } }
            }
        };

        _mockGeminiService.Setup(x => x.GenerateComicContentAsync(It.IsAny<string>()))
            .ReturnsAsync(mockComicContent);

        // Act
        var result = await _service.GenerateComicAsync(concept, options);

        // Assert
        Assert.Equal(4, result.Panels.Count);
    }
}