using MathComicGenerator.Shared.Interfaces;
using MathComicGenerator.Shared.Models;
using System.Text.Json;

namespace MathComicGenerator.Api.Services;

public class StorageService : IStorageService
{
    private readonly ILogger<StorageService> _logger;
    private readonly StorageConfiguration _config;
    private readonly string _storageBasePath;
    private readonly string _metadataPath;

    public StorageService(ILogger<StorageService> logger, IConfiguration configuration)
    {
        _logger = logger;
        
        // 手动构建配置对象以支持测试
        var storageSection = configuration.GetSection("Storage");
        _config = new StorageConfiguration
        {
            BasePath = storageSection["BasePath"] ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MathComicGenerator"),
            MaxStorageSize = long.TryParse(storageSection["MaxStorageSize"], out var maxSize) ? maxSize : 1024 * 1024 * 1024,
            MaxComicsPerUser = int.TryParse(storageSection["MaxComicsPerUser"], out var maxComics) ? maxComics : 100,
            EnableAutoCleanup = bool.TryParse(storageSection["EnableAutoCleanup"], out var autoCleanup) ? autoCleanup : true,
            CleanupAfterDays = int.TryParse(storageSection["CleanupAfterDays"], out var cleanupDays) ? cleanupDays : 30
        };

        _storageBasePath = Path.Combine(_config.BasePath, "comics");
        _metadataPath = Path.Combine(_config.BasePath, "metadata");

        EnsureDirectoriesExist();
    }

    public async Task<string> SaveComicAsync(MultiPanelComic comic)
    {
        try
        {
            _logger.LogInformation("Saving comic: {ComicId}", comic.Id);

            // 创建漫画目录
            var comicDirectory = Path.Combine(_storageBasePath, comic.Id);
            Directory.CreateDirectory(comicDirectory);

            // 保存漫画数据
            var comicDataPath = Path.Combine(comicDirectory, "comic.json");
            var comicJson = JsonSerializer.Serialize(comic, GetJsonOptions());
            await File.WriteAllTextAsync(comicDataPath, comicJson);

            // 保存元数据
            await SaveMetadataAsync(comic);

            // 面板图片已经在ComicGenerationService中生成，这里不需要重复生成

            // 创建导出格式
            await CreateExportFormatsAsync(comic, comicDirectory);

            _logger.LogInformation("Comic saved successfully: {ComicId}", comic.Id);
            return comic.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving comic: {ComicId}", comic.Id);
            throw new StorageException($"Failed to save comic {comic.Id}", ex);
        }
    }

    public async Task<MultiPanelComic?> LoadComicAsync(string id)
    {
        try
        {
            _logger.LogInformation("Loading comic: {ComicId}", id);

            var comicDataPath = Path.Combine(_storageBasePath, id, "comic.json");
            
            if (!File.Exists(comicDataPath))
            {
                _logger.LogWarning("Comic not found: {ComicId}", id);
                return null;
            }

            var comicJson = await File.ReadAllTextAsync(comicDataPath);
            var comic = JsonSerializer.Deserialize<MultiPanelComic>(comicJson, GetJsonOptions());

            if (comic != null)
            {
                // 更新图片URL为实际路径
                UpdateImageUrls(comic, id);
            }

            _logger.LogInformation("Comic loaded successfully: {ComicId}", id);
            return comic;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading comic: {ComicId}", id);
            throw new StorageException($"Failed to load comic {id}", ex);
        }
    }

    public async Task<List<ComicMetadata>> ListComicsAsync()
    {
        try
        {
            _logger.LogInformation("Listing all comics");

            var metadataFiles = Directory.GetFiles(_metadataPath, "*.json");
            var comicMetadataList = new List<ComicMetadata>();

            foreach (var metadataFile in metadataFiles)
            {
                try
                {
                    var metadataJson = await File.ReadAllTextAsync(metadataFile);
                    var metadata = JsonSerializer.Deserialize<ComicMetadataEntry>(metadataJson, GetJsonOptions());
                    
                    if (metadata?.Metadata != null)
                    {
                        comicMetadataList.Add(metadata.Metadata);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to read metadata file: {File}", metadataFile);
                }
            }

            // 按创建时间倒序排列
            comicMetadataList.Sort((a, b) => 
                DateTime.Compare(b.CreatedAt, a.CreatedAt));

            _logger.LogInformation("Found {Count} comics", comicMetadataList.Count);
            return comicMetadataList;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing comics");
            throw new StorageException("Failed to list comics", ex);
        }
    }

    public async Task<bool> DeleteComicAsync(string id)
    {
        try
        {
            _logger.LogInformation("Deleting comic: {ComicId}", id);

            var comicDirectory = Path.Combine(_storageBasePath, id);
            var metadataFile = Path.Combine(_metadataPath, $"{id}.json");

            var deleted = false;

            // 删除漫画目录
            if (Directory.Exists(comicDirectory))
            {
                Directory.Delete(comicDirectory, true);
                deleted = true;
            }

            // 删除元数据文件
            if (File.Exists(metadataFile))
            {
                File.Delete(metadataFile);
                deleted = true;
            }

            if (deleted)
            {
                _logger.LogInformation("Comic deleted successfully: {ComicId}", id);
            }
            else
            {
                _logger.LogWarning("Comic not found for deletion: {ComicId}", id);
            }

            return deleted;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting comic: {ComicId}", id);
            throw new StorageException($"Failed to delete comic {id}", ex);
        }
    }

    public async Task<byte[]> ExportComicAsync(string id, ExportFormat format)
    {
        try
        {
            _logger.LogInformation("Exporting comic {ComicId} in format {Format}", id, format);

            var comic = await LoadComicAsync(id);
            if (comic == null)
            {
                throw new StorageException($"Comic {id} not found");
            }

            return format switch
            {
                ExportFormat.JSON => await ExportAsJsonAsync(comic),
                ExportFormat.PDF => await ExportAsPdfAsync(comic),
                ExportFormat.ZIP => await ExportAsZipAsync(comic, id),
                _ => throw new ArgumentException($"Unsupported export format: {format}")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting comic: {ComicId}", id);
            throw new StorageException($"Failed to export comic {id}", ex);
        }
    }

    public async Task<ComicStatistics> GetStatisticsAsync()
    {
        try
        {
            var comics = await ListComicsAsync();
            
            var stats = new ComicStatistics
            {
                TotalComics = comics.Count,
                ComicsByAgeGroup = comics.GroupBy(c => c.GenerationOptions?.AgeGroup ?? AgeGroup.Elementary)
                                       .ToDictionary(g => g.Key, g => g.Count()),
                ComicsByVisualStyle = comics.GroupBy(c => c.GenerationOptions?.VisualStyle ?? VisualStyle.Cartoon)
                                          .ToDictionary(g => g.Key, g => g.Count()),
                TotalStorageSize = await CalculateTotalStorageSizeAsync(),
                MostPopularConcepts = GetMostPopularConcepts(comics)
            };

            return stats;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating statistics");
            throw new StorageException("Failed to calculate statistics", ex);
        }
    }

    private void EnsureDirectoriesExist()
    {
        Directory.CreateDirectory(_storageBasePath);
        Directory.CreateDirectory(_metadataPath);
    }

    private async Task SaveMetadataAsync(MultiPanelComic comic)
    {
        var metadataEntry = new ComicMetadataEntry
        {
            Id = comic.Id,
            Title = comic.Title,
            CreatedAt = comic.CreatedAt,
            Metadata = comic.Metadata
        };

        var metadataPath = Path.Combine(_metadataPath, $"{comic.Id}.json");
        var metadataJson = JsonSerializer.Serialize(metadataEntry, GetJsonOptions());
        await File.WriteAllTextAsync(metadataPath, metadataJson);
    }

    private async Task GenerateAndSavePanelImagesAsync(MultiPanelComic comic, string comicDirectory)
    {
        // 占位符实现 - 在实际应用中，这里会调用图像生成服务
        var imagesDirectory = Path.Combine(comicDirectory, "images");
        Directory.CreateDirectory(imagesDirectory);

        for (int i = 0; i < comic.Panels.Count; i++)
        {
            var panel = comic.Panels[i];
            var imagePath = Path.Combine(imagesDirectory, $"panel_{i + 1}.png");
            
            // 创建占位符图片文件
            await CreatePlaceholderImageAsync(imagePath, panel.ImageUrl ?? "");
            
            // 更新面板的图片URL
            panel.ImageUrl = $"/api/comics/{comic.Id}/images/panel_{i + 1}.png";
        }
    }

    private async Task CreatePlaceholderImageAsync(string imagePath, string description)
    {
        // 创建一个简单的文本文件作为占位符
        // 在实际实现中，这里会生成真实的图片
        var placeholderContent = $"Panel Image Placeholder\nDescription: {description}\nGenerated: {DateTime.Now}";
        await File.WriteAllTextAsync(imagePath.Replace(".png", ".txt"), placeholderContent);
    }

    private async Task CreateExportFormatsAsync(MultiPanelComic comic, string comicDirectory)
    {
        var exportDirectory = Path.Combine(comicDirectory, "exports");
        Directory.CreateDirectory(exportDirectory);

        // 创建JSON导出
        var jsonExportPath = Path.Combine(exportDirectory, "comic.json");
        var jsonContent = JsonSerializer.Serialize(comic, GetJsonOptions());
        await File.WriteAllTextAsync(jsonExportPath, jsonContent);

        // 创建文本摘要
        var summaryPath = Path.Combine(exportDirectory, "summary.txt");
        var summary = CreateComicSummary(comic);
        await File.WriteAllTextAsync(summaryPath, summary);
    }

    private string CreateComicSummary(MultiPanelComic comic)
    {
        var summary = $"漫画标题: {comic.Title}\n";
        summary += $"数学概念: {comic.Metadata.MathConcept}\n";
        summary += $"创建时间: {comic.CreatedAt:yyyy-MM-dd HH:mm:ss}\n";
        summary += $"面板数量: {comic.Panels.Count}\n";
        summary += $"年龄组: {comic.Metadata.GenerationOptions.AgeGroup}\n";
        summary += $"视觉风格: {comic.Metadata.GenerationOptions.VisualStyle}\n\n";

        summary += "面板内容:\n";
        for (int i = 0; i < comic.Panels.Count; i++)
        {
            var panel = comic.Panels[i];
            summary += $"面板 {i + 1}:\n";
            summary += $"  对话: {string.Join(", ", panel.Dialogue)}\n";
            if (!string.IsNullOrEmpty(panel.Narration))
            {
                summary += $"  旁白: {panel.Narration}\n";
            }
            summary += "\n";
        }

        return summary;
    }

    private void UpdateImageUrls(MultiPanelComic comic, string comicId)
    {
        for (int i = 0; i < comic.Panels.Count; i++)
        {
            var panel = comic.Panels[i];
            panel.ImageUrl = $"/api/comics/{comicId}/images/panel_{i + 1}.png";
        }
    }

    private async Task<byte[]> ExportAsJsonAsync(MultiPanelComic comic)
    {
        var json = JsonSerializer.Serialize(comic, GetJsonOptions());
        return System.Text.Encoding.UTF8.GetBytes(json);
    }

    private async Task<byte[]> ExportAsPdfAsync(MultiPanelComic comic)
    {
        // 占位符实现 - 在实际应用中会生成真实的PDF
        var pdfContent = $"PDF Export of Comic: {comic.Title}\nGenerated: {DateTime.Now}";
        return System.Text.Encoding.UTF8.GetBytes(pdfContent);
    }

    private async Task<byte[]> ExportAsZipAsync(MultiPanelComic comic, string comicId)
    {
        // 占位符实现 - 在实际应用中会创建真实的ZIP文件
        var zipContent = $"ZIP Export of Comic: {comic.Title}\nComic ID: {comicId}\nGenerated: {DateTime.Now}";
        return System.Text.Encoding.UTF8.GetBytes(zipContent);
    }

    private async Task<long> CalculateTotalStorageSizeAsync()
    {
        long totalSize = 0;
        
        if (Directory.Exists(_storageBasePath))
        {
            var directories = Directory.GetDirectories(_storageBasePath);
            foreach (var directory in directories)
            {
                totalSize += CalculateDirectorySize(directory);
            }
        }

        return totalSize;
    }

    private long CalculateDirectorySize(string directoryPath)
    {
        long size = 0;
        
        try
        {
            var files = Directory.GetFiles(directoryPath, "*", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                var fileInfo = new FileInfo(file);
                size += fileInfo.Length;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error calculating directory size: {Directory}", directoryPath);
        }

        return size;
    }

    private List<string> GetMostPopularConcepts(List<ComicMetadata> comics)
    {
        return comics.GroupBy(c => c.MathConcept)
                    .OrderByDescending(g => g.Count())
                    .Take(10)
                    .Select(g => g.Key)
                    .ToList();
    }

    private JsonSerializerOptions GetJsonOptions()
    {
        return new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };
    }
}

// 配置类
public class StorageConfiguration
{
    public string BasePath { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MathComicGenerator");
    public long MaxStorageSize { get; set; } = 1024 * 1024 * 1024; // 1GB
    public int MaxComicsPerUser { get; set; } = 100;
    public bool EnableAutoCleanup { get; set; } = true;
    public int CleanupAfterDays { get; set; } = 30;
}

// 辅助类
public class ComicMetadataEntry
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public ComicMetadata Metadata { get; set; } = new();
}



// 异常类
public class StorageException : Exception
{
    public StorageException(string message) : base(message) { }
    public StorageException(string message, Exception innerException) : base(message, innerException) { }
}