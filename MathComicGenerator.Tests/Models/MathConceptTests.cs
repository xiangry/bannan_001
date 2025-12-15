using MathComicGenerator.Shared.Models;

namespace MathComicGenerator.Tests.Models;

public class MathConceptTests
{
    [Fact]
    public void MathConcept_ShouldInitializeWithDefaults()
    {
        // Arrange & Act
        var mathConcept = new MathConcept();

        // Assert
        Assert.NotNull(mathConcept.Topic);
        Assert.NotNull(mathConcept.Keywords);
        Assert.Empty(mathConcept.Keywords);
    }

    [Fact]
    public void MathConcept_ShouldSetProperties()
    {
        // Arrange
        var mathConcept = new MathConcept
        {
            Topic = "Addition",
            Difficulty = DifficultyLevel.Elementary,
            AgeGroup = AgeGroup.Elementary,
            Keywords = new List<string> { "math", "addition", "numbers" }
        };

        // Assert
        Assert.Equal("Addition", mathConcept.Topic);
        Assert.Equal(DifficultyLevel.Elementary, mathConcept.Difficulty);
        Assert.Equal(AgeGroup.Elementary, mathConcept.AgeGroup);
        Assert.Equal(3, mathConcept.Keywords.Count);
    }
}