namespace MathComicGenerator.Shared.Models;

public class ComicStatistics
{
    public int TotalComics { get; set; }
    public int ComicsThisMonth { get; set; }
    public int ComicsThisWeek { get; set; }
    public Dictionary<AgeGroup, int> ComicsByAgeGroup { get; set; } = new();
    public Dictionary<VisualStyle, int> ComicsByVisualStyle { get; set; } = new();
    public long TotalStorageSize { get; set; }
    public List<string> MostPopularConcepts { get; set; } = new();
}