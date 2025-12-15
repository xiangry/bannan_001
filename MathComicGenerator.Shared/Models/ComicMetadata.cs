namespace MathComicGenerator.Shared.Models;

public class ComicMetadata
{
    public string MathConcept { get; set; } = string.Empty;
    public GenerationOptions GenerationOptions { get; set; } = new();
    public long FileSize { get; set; }
    public ImageFormat Format { get; set; }
    public List<string> Tags { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}