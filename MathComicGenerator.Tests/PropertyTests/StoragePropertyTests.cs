using FsCheck;
using FsCheck.Xunit;
using MathComicGenerator.Api.Services;
using MathComicGenerator.Shared.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace MathComicGenerator.Tests.PropertyTests;

public class StoragePropertyTests
{
    private readonly StorageService _storageService;
    private readonly Mock<ILogger<StorageService>> _mockLogger;
    private readonly IConfiguration _configuration;
    private readonly string _testStoragePath;

    public StoragePropertyTests()
    {
        _mockLogger = new Mock<ILogger<StorageService>>();
        _testStoragePath = Path.Combine(Path.GetTempPath(), "MathComicGenerator_Tests", Guid.NewGuid().ToString());
        
        // Setup test configuration
        var configData = new Dictionary<string, string>
        {
            {"Storage:BasePath", _testStoragePath},
            {"Storage:MaxStorageSize", "1073741824"}, // 1GB
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
    public bool Property15_SaveFunctionality_ComicCanBeSavedAndRetrieved(NonEmptyString title, PositiveInt panelCount)
    {
        // **Feature: math-comic-generator, Property 15: 保存功能可用性**
        // **Validates: Requirements 5.1**
        // For any completed comic generation, system should provide save option
        
        try
        {
            // Arrange - Create a test comic
            var validPanelCount = Math.Max(3, Math.Min(6, panelCount.Get));
            var comic = CreateTestComic(title.Get, validPanelCount);
            
            // Act - Save and retrieve comic
            var saveTask = _storageService.SaveComicAsync(comic);
            saveTask.Wait();
            var savedComicId = saveTask.Result;
            
            var retrieveTask = _storageService.LoadComicAsync(savedComicId);
            retrieveTask.Wait();
            var retrievedComic = retrieveTask.Result;
            
            // Assert - Comic should be saved and retrievable
            var saveSuccessful = !string.IsNullOrEmpty(savedComicId);
            var retrieveSuccessful = retrievedComic != null;
            var dataIntact = retrievedComic?.Id == comic.Id && 
                           retrievedComic?.Title == comic.Title &&
                           retrievedComic?.Panels?.Count == comic.Panels.Count;
            
            // Log the validation for debugging
            Console.WriteLine($"[DEBUG] Save Functionality: SaveSuccessful={saveSuccessful}, RetrieveSuccessful={retrieveSuccessful}, DataIntact={dataIntact}");
            
            // Cleanup
            CleanupTestComic(savedComicId);
            
            return saveSuccessful && retrieveSuccessful && dataIntact;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DEBUG] Save Functionality Error: {ex.Message}");
            return false;
        }
    }

    [Property]
    public bool Property15_SaveFunctionality_MultipleComicsCanBeSaved(PositiveInt comicCount)
    {
        // **Feature: math-comic-generator, Property 15: 保存功能可用性**
        // **Validates: Requirements 5.1**
        // Multiple comics should be able to be saved independently
        
        try
        {
            // Arrange - Create multiple test comics
            var testComicCount = Math.Min(5, comicCount.Get); // Limit for test performance
            var comics = new List<MultiPanelComic>();
            var savedIds = new List<string>();
            
            for (int i = 0; i < testComicCount; i++)
            {
                comics.Add(CreateTestComic($"Test Comic {i}", 4));
            }
            
            // Act - Save all comics
            foreach (var comic in comics)
            {
                var saveTask = _storageService.SaveComicAsync(comic);
                saveTask.Wait();
                savedIds.Add(saveTask.Result);
            }
            
            // Assert - All comics should be saved successfully
            var allSaved = savedIds.All(id => !string.IsNullOrEmpty(id));
            var uniqueIds = savedIds.Distinct().Count() == savedIds.Count;
            
            // Verify retrieval
            var allRetrievable = true;
            foreach (var id in savedIds)
            {
                var retrieveTask = _storageService.LoadComicAsync(id);
                retrieveTask.Wait();
                if (retrieveTask.Result == null)
                {
                    allRetrievable = false;
                    break;
                }
            }
            
            // Log the validation for debugging
            Console.WriteLine($"[DEBUG] Multiple Comics Save: Count={testComicCount}, AllSaved={allSaved}, UniqueIds={uniqueIds}, AllRetrievable={allRetrievable}");
            
            // Cleanup
            foreach (var id in savedIds)
            {
                CleanupTestComic(id);
            }
            
            return allSaved && uniqueIds && allRetrievable;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DEBUG] Multiple Comics Save Error: {ex.Message}");
            return false;
        }
    }

    [Property]
    public bool Property15_SaveFunctionality_SavedComicsHaveUniqueIds(NonEmptyString title1, NonEmptyString title2)
    {
        // **Feature: math-comic-generator, Property 15: 保存功能可用性**
        // **Validates: Requirements 5.1**
        // Each saved comic should have a unique identifier
        
        try
        {
            // Arrange - Create two different comics
            var comic1 = CreateTestComic(title1.Get, 4);
            var comic2 = CreateTestComic(title2.Get, 4);
            
            // Act - Save both comics
            var saveTask1 = _storageService.SaveComicAsync(comic1);
            saveTask1.Wait();
            var savedId1 = saveTask1.Result;
            
            var saveTask2 = _storageService.SaveComicAsync(comic2);
            saveTask2.Wait();
            var savedId2 = saveTask2.Result;
            
            // Assert - IDs should be unique and valid
            var bothSaved = !string.IsNullOrEmpty(savedId1) && !string.IsNullOrEmpty(savedId2);
            var idsUnique = savedId1 != savedId2;
            
            // Log the validation for debugging
            Console.WriteLine($"[DEBUG] Unique IDs: BothSaved={bothSaved}, IdsUnique={idsUnique}, ID1={savedId1}, ID2={savedId2}");
            
            // Cleanup
            CleanupTestComic(savedId1);
            CleanupTestComic(savedId2);
            
            return bothSaved && idsUnique;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DEBUG] Unique IDs Error: {ex.Message}");
            return false;
        }
    }

    [Property]
    public bool Property15_SaveFunctionality_SaveOperationIsIdempotent(NonEmptyString title)
    {
        // **Feature: math-comic-generator, Property 15: 保存功能可用性**
        // **Validates: Requirements 5.1**
        // Saving the same comic multiple times should be handled gracefully
        
        try
        {
            // Arrange - Create a test comic
            var comic = CreateTestComic(title.Get, 4);
            
            // Act - Save the same comic multiple times
            var saveTask1 = _storageService.SaveComicAsync(comic);
            saveTask1.Wait();
            var savedId1 = saveTask1.Result;
            
            var saveTask2 = _storageService.SaveComicAsync(comic);
            saveTask2.Wait();
            var savedId2 = saveTask2.Result;
            
            // Assert - Both saves should succeed (may return same or different IDs)
            var firstSaveSuccessful = !string.IsNullOrEmpty(savedId1);
            var secondSaveSuccessful = !string.IsNullOrEmpty(savedId2);
            
            // Verify both can be retrieved
            var retrieveTask1 = _storageService.LoadComicAsync(savedId1);
            retrieveTask1.Wait();
            var retrieved1 = retrieveTask1.Result;
            
            var retrieveTask2 = _storageService.LoadComicAsync(savedId2);
            retrieveTask2.Wait();
            var retrieved2 = retrieveTask2.Result;
            
            var bothRetrievable = retrieved1 != null && retrieved2 != null;
            
            // Log the validation for debugging
            Console.WriteLine($"[DEBUG] Idempotent Save: FirstSave={firstSaveSuccessful}, SecondSave={secondSaveSuccessful}, BothRetrievable={bothRetrievable}");
            
            // Cleanup
            CleanupTestComic(savedId1);
            if (savedId1 != savedId2)
            {
                CleanupTestComic(savedId2);
            }
            
            return firstSaveSuccessful && secondSaveSuccessful && bothRetrievable;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DEBUG] Idempotent Save Error: {ex.Message}");
            return false;
        }
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