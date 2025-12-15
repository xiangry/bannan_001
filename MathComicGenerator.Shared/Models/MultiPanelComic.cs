namespace MathComicGenerator.Shared.Models;

public class MultiPanelComic
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public List<ComicPanel> Panels { get; set; } = new();
    public ComicMetadata Metadata { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}