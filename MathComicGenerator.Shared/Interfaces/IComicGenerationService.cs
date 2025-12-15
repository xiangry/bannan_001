using MathComicGenerator.Shared.Models;

namespace MathComicGenerator.Shared.Interfaces;

public interface IComicGenerationService
{
    Task<MultiPanelComic> GenerateComicAsync(MathConcept concept, GenerationOptions options);
    ValidationResult ValidateConcept(string concept);
}

public class ValidationResult
{
    public bool IsValid { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public List<string> Suggestions { get; set; } = new();
}