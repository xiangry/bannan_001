using MathComicGenerator.Shared.Models;

namespace MathComicGenerator.Shared.Interfaces;

public interface IGenerationOptionsProcessor
{
    ValidationResult ValidateOptions(GenerationOptions options);
    GenerationOptions ApplyDefaults(GenerationOptions? options);
    GenerationOptions AdjustForAgeGroup(GenerationOptions options, AgeGroup ageGroup);
    bool AreOptionsConsistent(GenerationOptions options);
}