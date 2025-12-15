namespace MathComicGenerator.Shared.Models;

public class MathConcept
{
    public string Topic { get; set; } = string.Empty;
    public DifficultyLevel Difficulty { get; set; }
    public AgeGroup AgeGroup { get; set; }
    public List<string> Keywords { get; set; } = new();
}