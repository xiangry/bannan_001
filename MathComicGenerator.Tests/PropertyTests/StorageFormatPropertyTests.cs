using FsCheck;
using FsCheck.Xunit;
using MathComicGenerator.Api.Services;
using MathComicGenerator.Shared.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;

namespace MathComicGenerator.Tests.PropertyTests;

public class StorageFormatPropertyTests
{
    private readonly StorageService _storageService;
    private readonly Mock<ILogger<StorageService>> _mockLogger;
    private readonly IConfiguration _configuration;
    private readonly string _testStoragePath;

    public StorageFormatPropertyTests()
    {
        _mockLogger = new Mock<ILogger<StorageService>>();
        _testStoragePath = Path.Combine(Path.GetTempPath(), "MathComicGenerator_Tests", Guid.NewGuid().ToString());
        
        var configData = new Dictionary<string, string>
        {
            {"Storage:BasePath", _testStoragePath},
            {"Storage:MaxStorageSize", "1073741824"},
            {"Storage:MaxComicsPerUser", "100"},
            {"Storage:EnableAutoCleanup", "true"},
            {"Storage:CleanupAfterDays", "30"}
        };

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        _storageService = new StorageService(_mockLogger.Object, _configuration);
    }

    [Property]
    public bool Property16_StorageFormatSpecification_ComicsAreSavedInCommonImageFormat(NonEmptyString title)
    {
        // **Feature: math-comic-generator, Property 16: 存储格式规范**
        // **Validates: Requirements 5.2**
        // For any saved comic, it should be stored in common image format
        
        try
        {
            // Arrange - Create a test comic with image format metadata
            var comic = CreateTestComic(title.Get, 4);
            comic.Metadata.Format = ImageFormat.PNG; // Common image format
            
            // Act - Save comic
            var saveTask = _storageService.SaveComicAsync(comic);
            saveTask.Wait();
            var savedComicId = saveTask.Result;
            
            // Retrieve and verify format
            var retrieveTask = _storageService.LoadComicAsync(savedComicId);
            retrieveTask.Wait();
            var retrievedComic = retrieveTask.Result;
            
            // Assert - Format should be preserved and be a common format
            var formatPreserved = retrievedComic?.Metadata?.Format == comic.Metadata.Format;
            var isCommonFormat = IsCommonImageFormat(retrievedComic?.Metadata?.Format);
            
            // Log the validation for debugging
            Console.WriteLine($"[DEBUG] Storage Format: Format={retrievedComic?.Metadata?.Format}, Preserved={formatPreserved}, CommonFormat={isCommonFormat}");
            
            // Cleanup
            CleanupTestComic(savedComicId);
            
            return formatPreserved && isCommonFormat;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DEBUG] Storage Format Error: {ex.Message}");
            return false;
        }
    }

    [Property]
    public bool Property16_StorageFormatSpecification_JsonFormatIsValid(NonEmptyString title, PositiveInt panelCount)
    {
        // **Feature: math-comic-generator, Property 16: 存储格式规范**
        // **Validates: Requirements 5.2**
        // Stored comic data should be in valid JSON format
        
        try
        {
            // Arrange - Create a test comic
            var validPanelCount = Math.Max(3, Math.Min(6, panelCount.Get));
            var comic = CreateTestComic(title.Get, validPanelCount);
            
            // Act - Save comic and check file format
            var saveTask = _storageService.SaveComicAsync(comic);
            saveTask.Wait();
            var savedComicId = saveTask.Result;
            
            // Check if the saved file is valid JSON
            var comicDirectory = Path.Combine(_testStoragePath, "comics", savedComicId);
            var comicDataPath = Path.Combine(comicDirectory, "comic.json");
            
            var jsonExists = File.Exists(comicDataPath);
            var validJson = false;
            
            if (jsonExists)
            {
                var jsonContent = File.ReadAllText(comicDataPath);
                try
                {
                    var parsedComic = JsonSerializer.Deserialize<MultiPanelComic>(jsonContent);
                    validJson = parsedComic != null;
                }
                catch
                {
                    validJson = false;
                }
            }
            
            // Assert - JSON file should exist and be valid
            Console.WriteLine($"[DEBUG] JSON Format: FileExists={jsonExists}, ValidJSON={validJson}");
            
            // Cleanup
            CleanupTestComic(savedComicId);
            
            return jsonExists && validJson;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DEBUG] JSON Format Error: {ex.Message}");
            return false;
        }
    }

    [Property]
    public bool Property16_StorageFormatSpecification_AllImageFormatsAreSupported()
    {
        // **Feature: math-comic-generator, Property 16: 存储格式规范**
        // **Validates: Requirements 5.2**
        // All defined image formats should be supported for storage
        
        try
        {
            var supportedFormats = Enum.GetValues<ImageFormat>();
            var allSupported = true;
            
            foreach (var format in supportedFormats)
            {
                // Create comic with specific format
                var comic = CreateTestComic($"Test {format}", 4);
                comic.Metadata.Format = format;
                
                // Save and retrieve
                var saveTask = _storageService.SaveComicAsync(comic);
                saveTask.Wait();
                var savedComicId = saveTask.Result;
                
                var retrieveTask = _storageService.LoadComicAsync(savedComicId);
                retrieveTask.Wait();
                var retrievedComic = retrieveTask.Result;
                
                // Check if format is preserved
                var formatSupported = retrievedComic?.Metadata?.Format == format;
                
                Console.WriteLine($"[DEBUG] Format Support: Format={format}, Supported={formatSupported}");
                
                if (!formatSupported)
                {
                    allSupported = false;
                }
                
                // Cleanup
                CleanupTestComic(savedComicId);
            }
            
            return allSupported;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DEBUG] Format Support Error: {ex.Message}");
            return false;
        }
    }

    private bool IsCommonImageFormat(ImageFormat? format)
    {
        if (!format.HasValue) return false;
        
        var commonFormats = new[] { ImageFormat.PNG, ImageFormat.JPEG, ImageFormat.GIF, ImageFormat.WEBP };
        return commonFormats.Contains(format.Value);
    }

    private MultiPanelComic CreateTestComic(string title, int panelCount)
    {
        var panels = new List<ComicPanel>();
        for (int i = 0; i < panelCount; i++)
        {
            panels.Add(new ComicPanel
            {
                Id = $"panel_{i}",
                ImageUrl = $"https://example.com/panel_{i}.png",
                Dialogue = new List<string> { $"Panel {i} dialogue" },
                Narration = $"Panel {i} narration",
                Order = i
            });
        }

        return new MultiPanelComic
        {
            Id = Guid.NewGuid().ToString(),
            Title = title,
            Panels = panels,
            Metadata = new ComicMetadata
            {
                MathConcept = "Test Math Concept",
                GenerationOptions = new GenerationOptions
                {
                    PanelCount = panelCount,
                    AgeGroup = AgeGroup.Elementary,
                    VisualStyle = VisualStyle.Cartoon,
                    Language = Language.Chinese
                },
                FileSize = 1024,
                Format = ImageFormat.PNG,
                Tags = new List<string> { "test", "math" },
                CreatedAt = DateTime.UtcNow
            },
            CreatedAt = DateTime.UtcNow
        };
    }

    private void CleanupTestComic(string comicId)
    {
        try
        {
            if (!string.IsNullOrEmpty(comicId))
            {
                var deleteTask = _storageService.DeleteComicAsync(comicId);
                deleteTask.Wait();
            }
        }
        catch
        {
            // Ignore cleanup errors
        }
    }
}