using MathComicGenerator.Shared.Services;

namespace MathComicGenerator.Tests.Services;

public class MathContentDetectorTests
{
    private readonly MathContentDetector _detector;

    public MathContentDetectorTests()
    {
        _detector = new MathContentDetector();
    }

    [Fact]
    public void ContainsMathematicalConcepts_EmptyInput_ReturnsFalse()
    {
        // Act
        var result = _detector.ContainsMathematicalConcepts("");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ContainsMathematicalConcepts_MathContent_ReturnsTrue()
    {
        // Act
        var result = _detector.ContainsMathematicalConcepts("学习加法运算");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ContainsMathematicalConcepts_NonMathContent_ReturnsFalse()
    {
        // Act
        var result = _detector.ContainsMathematicalConcepts("看电影故事");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void CalculateMathRelevanceScore_MathContent_ReturnsPositiveScore()
    {
        // Act
        var result = _detector.CalculateMathRelevanceScore("学习加法运算");

        // Assert
        Assert.True(result > 0);
    }

    [Fact]
    public void CalculateMathRelevanceScore_NumbersPresent_IncreasesScore()
    {
        // Act
        var result = _detector.CalculateMathRelevanceScore("计算 1 + 2 = 3");

        // Assert
        Assert.True(result > 0);
    }

    [Fact]
    public void ExtractMathKeywords_GeometryTerms_DetectsCorrectly()
    {
        // Act
        var result = _detector.ExtractMathKeywords("学习三角形和圆形");

        // Assert
        Assert.Contains("三角形", result);
        Assert.Contains("圆形", result);
    }

    [Fact]
    public void ExtractMathKeywords_EnglishMathTerms_DetectsCorrectly()
    {
        // Act
        var result = _detector.ExtractMathKeywords("learn addition and subtraction");

        // Assert
        Assert.Contains("addition", result);
        Assert.Contains("subtraction", result);
    }

    [Fact]
    public void IsEducationallyAppropriate_SafeContent_ReturnsTrue()
    {
        // Act
        var result = _detector.IsEducationallyAppropriate("学习加法运算");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsEducationallyAppropriate_UnsafeContent_ReturnsFalse()
    {
        // Act
        var result = _detector.IsEducationallyAppropriate("暴力内容");

        // Assert
        Assert.False(result);
    }
}