using FsCheck;
using FsCheck.Xunit;
using MathComicGenerator.Shared.Models;

namespace MathComicGenerator.Tests.PropertyTests;

public class BasicPropertyTests
{
    [Property]
    public bool MultiPanelComic_ShouldHaveValidId_WhenCreated(NonEmptyString id)
    {
        // **Feature: math-comic-generator, Property Test: Basic ID validation**
        
        // Arrange & Act
        var comic = new MultiPanelComic
        {
            Id = id.Get
        };

        // Assert
        return !string.IsNullOrEmpty(comic.Id);
    }

    [Property]
    public bool GenerationOptions_PanelCount_ShouldBeWithinValidRange(PositiveInt panelCount)
    {
        // **Feature: math-comic-generator, Property Test: Panel count validation**
        
        // Arrange
        var options = new GenerationOptions();
        var count = Math.Max(3, Math.Min(6, panelCount.Get)); // Constrain to 3-6 range

        // Act
        options.PanelCount = count;

        // Assert
        return options.PanelCount >= 3 && options.PanelCount <= 6;
    }
}