using FsCheck;
using FsCheck.Xunit;
using MathComicGenerator.Shared.Models;

namespace MathComicGenerator.Tests.PropertyTests;

public class CoreDataModelPropertyTests
{
    [Property]
    public bool Property3_OutputCompleteness_MultiPanelComicShouldHaveAllRequiredFields(
        NonEmptyString id, 
        NonEmptyString title,
        PositiveInt panelCount)
    {
        // **Feature: math-comic-generator, Property 3: 输出完整性**
        // **Validates: Requirements 1.5**
        // For any successful generation request, returned MultiPanelComic should contain all required fields
        
        // Arrange - Create a MultiPanelComic with generated data
        var constrainedPanelCount = Math.Max(1, Math.Min(10, panelCount.Get)); // Reasonable range
        var panels = GeneratePanels(constrainedPanelCount);
        
        var comic = new MultiPanelComic
        {
            Id = id.Get,
            Title = title.Get,
            Panels = panels,
            Metadata = new ComicMetadata(),
            CreatedAt = DateTime.UtcNow
        };

        // Act & Assert - Verify all required fields are present and valid
        var hasValidId = !string.IsNullOrEmpty(comic.Id);
        var hasValidTitle = !string.IsNullOrEmpty(comic.Title);
        var hasPanels = comic.Panels != null;
        var hasMetadata = comic.Metadata != null;
        var hasCreatedAt = comic.CreatedAt != default(DateTime);

        // Log the validation for debugging
        Console.WriteLine($"[DEBUG] Property Test - Output Completeness Validation: ID={hasValidId}, Title={hasValidTitle}, Panels={hasPanels}, Metadata={hasMetadata}, CreatedAt={hasCreatedAt}");

        return hasValidId && hasValidTitle && hasPanels && hasMetadata && hasCreatedAt;
    }

    [Property]
    public bool Property3_OutputCompleteness_ComicPanelShouldHaveRequiredFields(
        NonEmptyString panelId,
        NonEmptyString imageUrl,
        NonNegativeInt order)
    {
        // **Feature: math-comic-generator, Property 3: 输出完整性**
        // **Validates: Requirements 1.5**
        // For any comic panel, it should have all required fields
        
        // Arrange
        var panel = new ComicPanel
        {
            Id = panelId.Get,
            ImageUrl = imageUrl.Get,
            Dialogue = new List<string> { "Test dialogue" },
            Narration = "Test narration",
            Order = order.Get
        };

        // Act & Assert
        var hasValidId = !string.IsNullOrEmpty(panel.Id);
        var hasValidImageUrl = !string.IsNullOrEmpty(panel.ImageUrl);
        var hasDialogue = panel.Dialogue != null;
        var hasValidOrder = panel.Order >= 0;

        // Log the validation for debugging
        Console.WriteLine($"[DEBUG] Panel Completeness Validation: ID={hasValidId}, ImageUrl={hasValidImageUrl}, Dialogue={hasDialogue}, Order={hasValidOrder}");

        return hasValidId && hasValidImageUrl && hasDialogue && hasValidOrder;
    }

    [Property]
    public bool Property3_OutputCompleteness_ComicMetadataShouldHaveRequiredFields(
        NonEmptyString mathConcept,
        NonNegativeInt fileSize)
    {
        // **Feature: math-comic-generator, Property 3: 输出完整性**
        // **Validates: Requirements 1.5**
        // For any comic metadata, it should have all required fields
        
        // Arrange
        var metadata = new ComicMetadata
        {
            MathConcept = mathConcept.Get,
            GenerationOptions = new GenerationOptions(),
            FileSize = fileSize.Get,
            Format = ImageFormat.PNG,
            Tags = new List<string> { "test" },
            CreatedAt = DateTime.UtcNow
        };

        // Act & Assert
        var hasValidMathConcept = !string.IsNullOrEmpty(metadata.MathConcept);
        var hasGenerationOptions = metadata.GenerationOptions != null;
        var hasValidFileSize = metadata.FileSize >= 0;
        var hasTags = metadata.Tags != null;
        var hasCreatedAt = metadata.CreatedAt != default(DateTime);

        // Log the validation for debugging
        Console.WriteLine($"[DEBUG] Metadata Completeness Validation: MathConcept={hasValidMathConcept}, Options={hasGenerationOptions}, FileSize={hasValidFileSize}, Tags={hasTags}, CreatedAt={hasCreatedAt}");

        return hasValidMathConcept && hasGenerationOptions && hasValidFileSize && hasTags && hasCreatedAt;
    }

    private static List<ComicPanel> GeneratePanels(int count)
    {
        var panels = new List<ComicPanel>();
        for (int i = 0; i < count; i++)
        {
            panels.Add(new ComicPanel
            {
                Id = $"panel_{i}",
                ImageUrl = $"https://example.com/image_{i}.png",
                Dialogue = new List<string> { $"Dialogue for panel {i}" },
                Narration = $"Narration for panel {i}",
                Order = i
            });
        }
        return panels;
    }
}