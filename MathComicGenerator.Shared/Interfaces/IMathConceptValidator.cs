using MathComicGenerator.Shared.Models;

namespace MathComicGenerator.Shared.Interfaces;

public interface IMathConceptValidator
{
    ValidationResult ValidateInput(string input);
    bool IsMathematicalContent(string content);
    MathConcept ParseMathConcept(string input);
    List<string> GetSuggestions(string invalidInput);
}