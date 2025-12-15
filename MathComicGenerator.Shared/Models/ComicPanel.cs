namespace MathComicGenerator.Shared.Models;

public class ComicPanel
{
    public string Id { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public List<string> Dialogue { get; set; } = new();
    public string? Narration { get; set; }
    public int Order { get; set; }
}