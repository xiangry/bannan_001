namespace MathComicGenerator.Shared.Interfaces;

public interface IGeminiAPIService
{
    Task<ComicContent> GenerateComicContentAsync(string prompt);
    Task<ErrorResponse> HandleAPIErrorAsync(APIError error);
}

public class ComicContent
{
    public string Title { get; set; } = string.Empty;
    public List<PanelContent> Panels { get; set; } = new();
}

public class PanelContent
{
    public string ImageDescription { get; set; } = string.Empty;
    public List<string> Dialogue { get; set; } = new();
    public string? Narration { get; set; }
}

public class APIError
{
    public string ErrorCode { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class ErrorResponse
{
    public string UserMessage { get; set; } = string.Empty;
    public bool ShouldRetry { get; set; }
    public TimeSpan? RetryAfter { get; set; }
    public string[] ResolutionSteps { get; set; } = Array.Empty<string>();
}