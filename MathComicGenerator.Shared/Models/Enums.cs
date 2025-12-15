namespace MathComicGenerator.Shared.Models;

public enum DifficultyLevel
{
    Beginner,
    Elementary,
    Intermediate,
    Advanced
}

public enum AgeGroup
{
    Preschool,      // 3-5岁
    Elementary,     // 6-11岁
    MiddleSchool,   // 12-14岁
    HighSchool      // 15-18岁
}

public enum VisualStyle
{
    Cartoon,
    Realistic,
    Minimalist,
    Colorful
}

public enum Language
{
    Chinese,
    English
}

public enum ImageFormat
{
    PNG,
    JPEG,
    SVG
}

public enum ExportFormat
{
    JSON,
    PDF,
    ZIP
}