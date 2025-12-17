using FsCheck;
using FsCheck.Xunit;
using MathComicGenerator.Api.Services;
using MathComicGenerator.Shared.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace MathComicGenerator.Tests.PropertyTests;

public class MetadataIntegrityPropertyTests
{
    private readonly StorageService _storageService;
    private readonly Mock<ILogger<StorageService>> _mockLogger;
    private readonly IConfiguration _configuration;
    private readonly string _testStoragePath;

    public MetadataIntegrityPropertyTests()
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
    public bool Property17_MetadataIntegrity_SavedComicContainsCompleteMetadata(NonEmptyString mathConcept, NonEmptyString title)
    {
        // **Feature: math-comic-generator, Property 17: 元数据完整性**
        // **Validates: Requirements 5.3**
        // For any saved comic, it should contain complete metadata information
        
        try
        {
            // Arrange - Create comic with complete metadata
            var comic = CreateTestComicWithMetadata(title.Get, mathConcept.Get, 4);
            
            // Act - Save and retrieve comic
            var saveTask = _storageService.SaveComicAsync(comic);
            saveTask.Wait();
            var savedComicId = saveTask.Result;
            
            var retrieveTask = _storageService.LoadComicAsync(savedComicId);
            retrieveTask.Wait();
            var retrievedComic = retrieveTask.Result;
            
            // Assert - All metadata fields should be present and intact
            var hasMetadata = retrievedComic?.Metadata != null;
            var hasMathConcept = !string.IsNullOrEmpty(retrievedComic?.Metadata?.MathConcept);
            var hasGenerationOptions = retrievedComic?.Metadata?.GenerationOptions != null;
            var hasFileSize = retrievedComic?.Metadata?.FileSize > 0;
            var hasFormat = Enum.IsDefined(typeof(ImageFormat), retrievedComic?.Metadata?.Format ?? ImageFormat.PNG);
            var hasTags = retrievedComic?.Metadata?.Tags != null;
            var hasCreatedAt = retrievedComic?.Metadata?.CreatedAt != default(DateTime);
            
            var metadataComplete = hasMetadata && hasMathConcept && hasGenerationOptions && 
                                 hasFileSize && hasFormat && hasTags && hasCreatedAt;
            
            // Log the validation for debugging
            Console.WriteLine($"[DEBUG] Metadata Integrity: HasMetadata={hasMetadata}, MathConcept={hasMathConcept}, Options={hasGenerationOptions}, FileSize={hasFileSize}, Format={hasFormat}, Tags={hasTags}, CreatedAt={hasCreatedAt}");
            
            // Cleanup
            CleanupTestComic(savedComicId);
            
            return metadataComplete;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DEBUG] Metadata Integrity Error: {ex.Message}");
            return false;
        }
    }

    [Property]
    public bool Property17_MetadataIntegrity_MetadataValuesArePreserved(NonEmptyString mathConcept, PositiveInt fileSize)
    {
        // **Feature: math-comic-generator, Property 17: 元数据完整性**
        // **Validates: Requirements 5.3**
        // Metadata values should be preserved exactly as provided
        
        try
        {
            // Arrange - Create comic with specific metadata values
            var comic = CreateTestComicWithMetadata("Test Comic", mathConcept.Get, 4);
            comic.Metadata.FileSize = fileSize.Get;
            comic.Metadata.Format = ImageFormat.JPEG;
            comic.Metadata.Tags = new List<string> { "test", "property", "metadata" };
            
            var originalCreatedAt = DateTime.UtcNow.AddDays(-1); // Specific timestamp
            comic.Metadata.CreatedAt = originalCreatedAt;
            
            // Act - Save and retrieve
            var saveTask = _storageService.SaveComicAsync(comic);
            saveTask.Wait();
            var savedComicId = saveTask.Result;
            
            var retrieveTask = _storageService.LoadComicAsync(savedComicId);
            retrieveTask.Wait();
            var retrievedComic = retrieveTask.Result;
            
            // Assert - All values should be preserved exactly
            var mathConceptPreserved = retrievedComic?.Metadata?.MathConcept == mathConcept.Get;
            var fileSizePreserved = retrievedComic?.Metadata?.FileSize == fileSize.Get;
            var formatPreserved = retrievedComic?.Metadata?.Format == ImageFormat.JPEG;
            var tagsPreserved = retrievedComic?.Metadata?.Tags?.Count == 3 &&
                              retrievedComic.Metadata.Tags.Contains("test") &&
                              retrievedComic.Metadata.Tags.Contains("property") &&
                              retrievedComic.Metadata.Tags.Contains("metadata");
            var createdAtPreserved = Math.Abs((retrievedComic?.Metadata?.CreatedAt - originalCreatedAt)?.TotalSeconds ?? double.MaxValue) < 1;
            
            var allPreserved = mathConceptPreserved && fileSizePreserved && formatPreserved && 
                             tagsPreserved && createdAtPreserved;
            
            // Log the validation for debugging
            Console.WriteLine($"[DEBUG] Metadata Preservation: MathConcept={mathConceptPreserved}, FileSize={fileSizePreserved}, Format={formatPreserved}, Tags={tagsPreserved}, CreatedAt={createdAtPreserved}");
            
            // Cleanup
            CleanupTestComic(savedComicId);
            
            return allPreserved;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DEBUG] Metadata Preservation Error: {ex.Message}");
            return false;
        }
    }

    [Property]
    public bool Property17_MetadataIntegrity_GenerationOptionsAreCompletelyPreserved(PositiveInt panelCount)
    {
        // **Feature: math-comic-generator, Property 17: 元数据完整性**
        // **Validates: Requirements 5.3**
        // Generation options within metadata should be completely preserved
        
        try
        {
            // Arrange - Create comic with specific generation options
            var validPanelCount = Math.Max(3, Math.Min(6, panelCount.Get));
            var comic = CreateTestComicWithMetadata("Test Comic", "Test Math", validPanelCount);
            
            comic.Metadata.GenerationOptions = new GenerationOptions
            {
                PanelCount = validPanelCount,
                AgeGroup = AgeGroup.Preschool,
                VisualStyle = VisualStyle.Realistic,
                Language = Language.English,
                EnablePinyin = false
            };
            
            // Act - Save and retrieve
            var saveTask = _storageService.SaveComicAsync(comic);
            saveTask.Wait();
            var savedComicId = saveTask.Result;
            
            var retrieveTask = _storageService.LoadComicAsync(savedComicId);
            retrieveTask.Wait();
            var retrievedComic = retrieveTask.Result;
            
            // Assert - All generation options should be preserved
            var options = retrievedComic?.Metadata?.GenerationOptions;
            var panelCountPreserved = options?.PanelCount == validPanelCount;
            var ageGroupPreserved = options?.AgeGroup == AgeGroup.Preschool;
            var visualStylePreserved = options?.VisualStyle == VisualStyle.Realistic;
            var languagePreserved = options?.Language == Language.English;
            var pinyinPreserved = options?.EnablePinyin == false;
            
            var allOptionsPreserved = panelCountPreserved && ageGroupPreserved && 
                                    visualStylePreserved && languagePreserved && pinyinPreserved;
            
            // Log the validation for debugging
            Console.WriteLine($"[DEBUG] Generation Options: PanelCount={panelCountPreserved}, AgeGroup={ageGroupPreserved}, VisualStyle={visualStylePreserved}, Language={languagePreserved}, Pinyin={pinyinPreserved}");
            
            // Cleanup
            CleanupTestComic(savedComicId);
            
            return allOptionsPreserved;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DEBUG] Generation Options Error: {ex.Message}");
            return false;
        }
    }

    [Property]
    public bool Property17_MetadataIntegrity_MetadataIsConsistentAcrossOperations(NonEmptyString mathConcept)
    {
        // **Feature: math-comic-generator, Property 17: 元数据完整性**
        // **Validates: Requirements 5.3**
        // Metadata should remain consistent across multiple storage operations
        
        try
        {
            // Arrange - Create comic with metadata
            var comic = CreateTestComicWithMetadata("Consistency Test", mathConcept.Get, 4);
            
            // Act - Save, retrieve, save again, retrieve again
            var saveTask1 = _storageService.SaveComicAsync(comic);
            saveTask1.Wait();
            var savedComicId1 = saveTask1.Result;
            
            var retrieveTask1 = _storageService.LoadComicAsync(savedComicId1);
            retrieveTask1.Wait();
            var retrievedComic1 = retrieveTask1.Result;
            
            // Save the retrieved comic again (simulating re-save operation)
            var saveTask2 = _storageService.SaveComicAsync(retrievedComic1);
            saveTask2.Wait();
            var savedComicId2 = saveTask2.Result;
            
            var retrieveTask2 = _storageService.LoadComicAsync(savedComicId2);
            retrieveTask2.Wait();
            var retrievedComic2 = retrieveTask2.Result;
            
            // Assert - Metadata should be consistent across operations
            var mathConceptConsistent = retrievedComic1?.Metadata?.MathConcept == retrievedComic2?.Metadata?.MathConcept;
            var fileSizeConsistent = retrievedComic1?.Metadata?.FileSize == retrievedComic2?.Metadata?.FileSize;
            var formatConsistent = retrievedComic1?.Metadata?.Format == retrievedComic2?.Metadata?.Format;
            var tagsConsistent = retrievedComic1?.Metadata?.Tags?.Count == retrievedComic2?.Metadata?.Tags?.Count;
            
            var metadataConsistent = mathConceptConsistent && fileSizeConsistent && 
                                   formatConsistent && tagsConsistent;
            
            // Log the validation for debugging
            Console.WriteLine($"[DEBUG] Metadata Consistency: MathConcept={mathConceptConsistent}, FileSize={fileSizeConsistent}, Format={formatConsistent}, Tags={tagsConsistent}");
            
            // Cleanup
            CleanupTestComic(savedComicId1);
            if (savedComicId1 != savedComicId2)
            {
                CleanupTestComic(savedComicId2);
            }
            
            return metadataConsistent;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DEBUG] Metadata Consistency Error: {ex.Message}");
            return false;
        }
    }

    private MultiPanelComic CreateTestComicWithMetadata(string title, string mathConcept, int panelCount)
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
                MathConcept = mathConcept,
                GenerationOptions = new GenerationOptions
                {
                    PanelCount = panelCount,
                    AgeGroup = AgeGroup.Elementary,
                    VisualStyle = VisualStyle.Cartoon,
                    Language = Language.Chinese,
                    EnablePinyin = true
                },
                FileSize = 2048,
                Format = ImageFormat.PNG,
                Tags = new List<string> { "test", "math", "property" },
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