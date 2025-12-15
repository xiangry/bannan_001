namespace MathComicGenerator.Shared.Interfaces;

public interface IMathContentDetector
{
    bool ContainsMathematicalConcepts(string content);
    double CalculateMathRelevanceScore(string content);
    List<string> ExtractMathKeywords(string content);
    bool IsEducationallyAppropriate(string content);
}