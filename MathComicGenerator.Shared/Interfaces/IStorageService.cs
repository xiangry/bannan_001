using MathComicGenerator.Shared.Models;

namespace MathComicGenerator.Shared.Interfaces;

public interface IStorageService
{
    Task<string> SaveComicAsync(MultiPanelComic comic);
    Task<MultiPanelComic?> LoadComicAsync(string id);
    Task<List<ComicMetadata>> ListComicsAsync();
    Task<bool> DeleteComicAsync(string id);
    Task<byte[]> ExportComicAsync(string id, ExportFormat format);
    Task<ComicStatistics> GetStatisticsAsync();
}