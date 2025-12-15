using MathComicGenerator.Api.Services;
using MathComicGenerator.Shared.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace MathComicGenerator.Tests.Services;

public class StorageServiceTests : IDisposable
{
    private readonly Mock<ILogger<StorageService>> _mockLogger;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<IConfigurationSection> _mockConfigSection;
    private readonly StorageService _storageService;
    private readonly string _testDataPath;

    public StorageServiceTests()
    {
        _mockLogger = new Mock<ILogger<StorageService>>();
        _mockConfiguration = new Mock<IConfiguration>();
        _mockConfigSection = new Mock<IConfigurationSection>();
        
        // 创建临时测试目录
        _testDataPath = Path.Combine(Path.GetTempPath(), "MathComicGeneratorTests", Guid.NewGuid().ToString());
        
        SetupConfiguration();
        _storageService = new StorageService(_mockLogger.Object, _mockConfiguration.Object);
    }

    private void SetupConfiguration()
    {
        _mockConfigSection.Setup(x => x["BasePath"]).Returns(_testDataPath);
        _mockConfigSection.Setup(x => x["MaxStorageSize"]).Returns("1073741824");
        _mockConfigSection.Setup(x => x["MaxComicsPerUser"]).Returns("100");
        _mockConfigSection.Setup(x => x["EnableAutoCleanup"]).Returns("true");
        _mockConfigSection.Setup(x => x["CleanupAfterDays"]).Returns("30");
        
        _mockConfiguration.Setup(x => x.GetSection("Storage")).Returns(_mockConfigSection.Object);
    }

    [Fact]
    public async Task SaveComicAsync_ValidComic_ReturnsComicId()
    {
        // Arrange
        var comic = CreateTestComic();

        // Act
        var result = await _storageService.SaveComicAsync(comic);

        // Assert
        Assert.Equal(comic.Id, result);
        
        // 验证文件是否创建
        var comicPath = Path.Combine(_testDataPath, "comics", comic.Id, "comic.json");
        Assert.True(File.Exists(comicPath));
        
        var metadataPath = Path.Combine(_testDataPath, "metadata", $"{comic.Id}.json");
        Assert.True(File.Exists(metadataPath));
    }

    [Fact]
    public async Task LoadComicAsync_ExistingComic_ReturnsComic()
    {
        // Arrange
        var comic = CreateTestComic();
        await _storageService.SaveComicAsync(comic);

        // Act
        var result = await _storageService.LoadComicAsync(comic.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(comic.Id, result.Id);
        Assert.Equal(comic.Title, result.Title);
        Assert.Equal(comic.Panels.Count, result.Panels.Count);
    }

    [Fact]
    public async Task LoadComicAsync_NonExistentComic_ReturnsNull()
    {
        // Act
        var result = await _storageService.LoadComicAsync("non-existent-id");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteComicAsync_ExistingComic_ReturnsTrue()
    {
        // Arrange
        var comic = CreateTestComic();
        await _storageService.SaveComicAsync(comic);

        // Act
        var result = await _storageService.DeleteComicAsync(comic.Id);

        // Assert
        Assert.True(result);
        
        // 验证文件是否删除
        var comicPath = Path.Combine(_testDataPath, "comics", comic.Id);
        Assert.False(Directory.Exists(comicPath));
        
        var metadataPath = Path.Combine(_testDataPath, "metadata", $"{comic.Id}.json");
        Assert.False(File.Exists(metadataPath));
    }

    [Fact]
    public async Task DeleteComicAsync_NonExistentComic_ReturnsFalse()
    {
        // Act
        var result = await _storageService.DeleteComicAsync("non-existent-id");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ListComicsAsync_MultipleComics_ReturnsAllComics()
    {
        // Arrange
        var comic1 = CreateTestComic();
        var comic2 = CreateTestComic();
        comic2.Id = Guid.NewGuid().ToString();
        comic2.Title = "Test Comic 2";

        await _storageService.SaveComicAsync(comic1);
        await _storageService.SaveComicAsync(comic2);

        // Act
        var result = await _storageService.ListComicsAsync();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, m => m.MathConcept == comic1.Metadata.MathConcept);
        Assert.Contains(result, m => m.MathConcept == comic2.Metadata.MathConcept);
    }

    [Fact]
    public async Task ListComicsAsync_NoComics_ReturnsEmptyList()
    {
        // Act
        var result = await _storageService.ListComicsAsync();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task ExportComicAsync_ValidComicAsJson_ReturnsJsonBytes()
    {
        // Arrange
        var comic = CreateTestComic();
        await _storageService.SaveComicAsync(comic);

        // Act
        var result = await _storageService.ExportComicAsync(comic.Id, ExportFormat.JSON);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Length > 0);
        
        var jsonString = System.Text.Encoding.UTF8.GetString(result);
        Assert.Contains(comic.Title, jsonString);
    }

    [Fact]
    public async Task ExportComicAsync_NonExistentComic_ThrowsException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<StorageException>(() => 
            _storageService.ExportComicAsync("non-existent-id", ExportFormat.JSON));
    }

    [Fact]
    public async Task GetStatisticsAsync_WithComics_ReturnsCorrectStatistics()
    {
        // Arrange
        var comic1 = CreateTestComic();
        var comic2 = CreateTestComic();
        comic2.Id = Guid.NewGuid().ToString();
        comic2.Metadata.GenerationOptions.AgeGroup = AgeGroup.MiddleSchool;

        await _storageService.SaveComicAsync(comic1);
        await _storageService.SaveComicAsync(comic2);

        // Act
        var result = await _storageService.GetStatisticsAsync();

        // Assert
        Assert.Equal(2, result.TotalComics);
        Assert.True(result.ComicsByAgeGroup.ContainsKey(AgeGroup.Elementary));
        Assert.True(result.ComicsByAgeGroup.ContainsKey(AgeGroup.MiddleSchool));
        Assert.NotEmpty(result.MostPopularConcepts);
    }

    [Fact]
    public void StorageConfiguration_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var config = new StorageConfiguration();

        // Assert
        Assert.Contains("MathComicGenerator", config.BasePath);
        Assert.Equal(1024 * 1024 * 1024, config.MaxStorageSize);
        Assert.Equal(100, config.MaxComicsPerUser);
        Assert.True(config.EnableAutoCleanup);
        Assert.Equal(30, config.CleanupAfterDays);
    }

    [Fact]
    public void StorageException_WithMessage_SetsMessageCorrectly()
    {
        // Arrange & Act
        var exception = new StorageException("Test message");

        // Assert
        Assert.Equal("Test message", exception.Message);
    }

    [Fact]
    public void StorageException_WithMessageAndInnerException_SetsPropertiesCorrectly()
    {
        // Arrange
        var innerException = new InvalidOperationException("Inner exception");

        // Act
        var exception = new StorageException("Test message", innerException);

        // Assert
        Assert.Equal("Test message", exception.Message);
        Assert.Equal(innerException, exception.InnerException);
    }

    private MultiPanelComic CreateTestComic()
    {
        return new MultiPanelComic
        {
            Id = Guid.NewGuid().ToString(),
            Title = "Test Comic",
            CreatedAt = DateTime.UtcNow,
            Panels = new List<ComicPanel>
            {
                new ComicPanel
                {
                    Id = Guid.NewGuid().ToString(),
                    Order = 1,
                    ImageUrl = "panel_1.png",
                    Dialogue = new List<string> { "Hello!" },
                    Narration = "Test narration"
                },
                new ComicPanel
                {
                    Id = Guid.NewGuid().ToString(),
                    Order = 2,
                    ImageUrl = "panel_2.png",
                    Dialogue = new List<string> { "World!" },
                    Narration = "Test narration 2"
                }
            },
            Metadata = new ComicMetadata
            {
                MathConcept = "Addition",
                GenerationOptions = new GenerationOptions
                {
                    AgeGroup = AgeGroup.Elementary,
                    PanelCount = 2,
                    VisualStyle = VisualStyle.Cartoon,
                    Language = Language.Chinese
                },
                Format = ImageFormat.PNG,
                Tags = new List<string> { "math", "addition" }
            }
        };
    }

    public void Dispose()
    {
        // 清理测试数据
        if (Directory.Exists(_testDataPath))
        {
            try
            {
                Directory.Delete(_testDataPath, true);
            }
            catch
            {
                // 忽略清理错误
            }
        }
    }
}