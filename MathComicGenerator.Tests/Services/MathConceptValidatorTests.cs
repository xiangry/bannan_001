using MathComicGenerator.Shared.Models;
using MathComicGenerator.Shared.Services;

namespace MathComicGenerator.Tests.Services;

public class MathConceptValidatorTests
{
    private readonly MathConceptValidator _validator;

    public MathConceptValidatorTests()
    {
        _validator = new MathConceptValidator();
    }

    [Fact]
    public void ValidateInput_EmptyInput_ReturnsInvalid()
    {
        // Act
        var result = _validator.ValidateInput("");

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal("请输入学习内容", result.ErrorMessage);
        Assert.NotEmpty(result.Suggestions);
    }

    [Fact]
    public void ValidateInput_WhitespaceInput_ReturnsInvalid()
    {
        // Act
        var result = _validator.ValidateInput("   ");

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal("请输入学习内容", result.ErrorMessage);
    }

    [Fact]
    public void ValidateInput_ValidMathConcept_ReturnsValid()
    {
        // Act
        var result = _validator.ValidateInput("加法运算");

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.ErrorMessage);
    }

    [Fact]
    public void ValidateInput_NonMathContent_ReturnsValid()
    {
        // Act - 现在任何内容都被接受，由AI来处理
        var result = _validator.ValidateInput("电影故事");

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.ErrorMessage);
    }

    [Fact]
    public void ValidateInput_TooLongInput_ReturnsInvalid()
    {
        // Arrange
        var longInput = new string('a', 201);

        // Act
        var result = _validator.ValidateInput(longInput);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("过长", result.ErrorMessage);
    }

    [Fact]
    public void IsMathematicalContent_MathKeywords_ReturnsTrue()
    {
        // Act
        var result = _validator.IsMathematicalContent("学习加法运算");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsMathematicalContent_NoMathKeywords_ReturnsFalse()
    {
        // Act
        var result = _validator.IsMathematicalContent("看电影故事");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ParseMathConcept_ValidInput_ReturnsCorrectModel()
    {
        // Act
        var result = _validator.ParseMathConcept("加法运算");

        // Assert
        Assert.Equal("加法运算", result.Topic);
        Assert.Equal(AgeGroup.Elementary, result.AgeGroup); // Default value
        Assert.Contains("加法", result.Keywords);
    }

    [Fact]
    public void GetSuggestions_InvalidInput_ReturnsSuggestions()
    {
        // Act
        var result = _validator.GetSuggestions("电影");

        // Assert
        Assert.NotEmpty(result);
        Assert.Contains(result, s => s.Contains("加法") || s.Contains("数学"));
    }
}