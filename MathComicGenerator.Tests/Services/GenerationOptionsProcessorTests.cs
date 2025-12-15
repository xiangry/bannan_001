using MathComicGenerator.Shared.Models;
using MathComicGenerator.Shared.Services;

namespace MathComicGenerator.Tests.Services;

public class GenerationOptionsProcessorTests
{
    private readonly GenerationOptionsProcessor _processor;

    public GenerationOptionsProcessorTests()
    {
        _processor = new GenerationOptionsProcessor();
    }

    [Fact]
    public void ValidateOptions_ValidOptions_ReturnsValid()
    {
        // Arrange
        var options = new GenerationOptions
        {
            PanelCount = 4,
            AgeGroup = AgeGroup.Elementary,
            VisualStyle = VisualStyle.Cartoon,
            Language = Language.Chinese
        };

        // Act
        var result = _processor.ValidateOptions(options);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.ErrorMessage);
    }

    [Fact]
    public void ValidateOptions_InvalidPanelCount_ReturnsInvalid()
    {
        // Arrange
        var options = new GenerationOptions
        {
            PanelCount = 2, // Invalid: less than 3
            AgeGroup = AgeGroup.Elementary,
            VisualStyle = VisualStyle.Cartoon,
            Language = Language.Chinese
        };

        // Act
        var result = _processor.ValidateOptions(options);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("面板数量必须在3-6之间", result.ErrorMessage);
    }

    [Fact]
    public void ValidateOptions_PanelCountTooHigh_ReturnsInvalid()
    {
        // Arrange
        var options = new GenerationOptions
        {
            PanelCount = 7, // Invalid: more than 6
            AgeGroup = AgeGroup.Elementary,
            VisualStyle = VisualStyle.Cartoon,
            Language = Language.Chinese
        };

        // Act
        var result = _processor.ValidateOptions(options);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("面板数量必须在3-6之间", result.ErrorMessage);
    }

    [Fact]
    public void AdjustForAgeGroup_PreschoolAge_AdjustsAppropriately()
    {
        // Arrange
        var options = new GenerationOptions
        {
            PanelCount = 6, // Too many for preschool
            AgeGroup = AgeGroup.Elementary,
            VisualStyle = VisualStyle.Realistic, // Not ideal for preschool
            Language = Language.Chinese
        };

        // Act
        var result = _processor.AdjustForAgeGroup(options, AgeGroup.Preschool);

        // Assert
        Assert.Equal(4, result.PanelCount); // Adjusted to max for preschool
        Assert.Equal(VisualStyle.Cartoon, result.VisualStyle); // Adjusted to cartoon
        Assert.Equal(AgeGroup.Preschool, result.AgeGroup);
    }

    [Fact]
    public void AreOptionsConsistent_ConsistentOptions_ReturnsTrue()
    {
        // Arrange
        var options = new GenerationOptions
        {
            PanelCount = 3,
            AgeGroup = AgeGroup.Preschool,
            VisualStyle = VisualStyle.Cartoon,
            Language = Language.Chinese
        };

        // Act
        var result = _processor.AreOptionsConsistent(options);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void AreOptionsConsistent_InconsistentOptions_ReturnsFalse()
    {
        // Arrange
        var options = new GenerationOptions
        {
            PanelCount = 6, // Too many for preschool
            AgeGroup = AgeGroup.Preschool,
            VisualStyle = VisualStyle.Minimalist, // Not appropriate for preschool
            Language = Language.Chinese
        };

        // Act
        var result = _processor.AreOptionsConsistent(options);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ApplyDefaults_NullOptions_ReturnsDefaults()
    {
        // Act
        var result = _processor.ApplyDefaults(null);

        // Assert
        Assert.Equal(4, result.PanelCount);
        Assert.Equal(AgeGroup.Elementary, result.AgeGroup);
        Assert.Equal(VisualStyle.Cartoon, result.VisualStyle);
        Assert.Equal(Language.Chinese, result.Language);
    }

    [Fact]
    public void ApplyDefaults_InvalidOptions_FixesInvalidValues()
    {
        // Arrange
        var options = new GenerationOptions
        {
            PanelCount = 10, // Invalid: too high
            AgeGroup = AgeGroup.Elementary,
            VisualStyle = VisualStyle.Cartoon,
            Language = Language.Chinese
        };

        // Act
        var result = _processor.ApplyDefaults(options);

        // Assert
        Assert.Equal(4, result.PanelCount); // Fixed to default
        Assert.Equal(AgeGroup.Elementary, result.AgeGroup);
        Assert.Equal(VisualStyle.Cartoon, result.VisualStyle);
        Assert.Equal(Language.Chinese, result.Language);
    }
}