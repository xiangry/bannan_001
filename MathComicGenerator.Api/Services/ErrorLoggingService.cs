using System.Text.Json;

namespace MathComicGenerator.Api.Services;

public class ErrorLoggingService
{
    private readonly ILogger<ErrorLoggingService> _logger;
    private readonly string _errorLogPath;
    private readonly SemaphoreSlim _fileSemaphore;

    public ErrorLoggingService(ILogger<ErrorLoggingService> logger, IConfiguration configuration)
    {
        _logger = logger;
        
        // 手动获取配置值以支持测试
        var logPath = "./logs";
        try
        {
            var configValue = configuration["Logging:ErrorLogPath"];
            if (!string.IsNullOrEmpty(configValue))
            {
                logPath = configValue;
            }
        }
        catch
        {
            // 使用默认值
        }
        
        _errorLogPath = Path.Combine(logPath, "errors");
        _fileSemaphore = new SemaphoreSlim(1, 1);
        
        Directory.CreateDirectory(_errorLogPath);
    }

    public async Task LogErrorAsync(Exception exception, string? context = null, Dictionary<string, object>? additionalData = null)
    {
        try
        {
            var errorEntry = new ErrorLogEntry
            {
                Id = Guid.NewGuid().ToString(),
                Timestamp = DateTime.UtcNow,
                ExceptionType = exception.GetType().Name,
                Message = exception.Message,
                StackTrace = exception.StackTrace,
                Context = context,
                AdditionalData = additionalData ?? new Dictionary<string, object>(),
                InnerException = exception.InnerException?.Message
            };

            // 记录到结构化日志
            _logger.LogError(exception, "Error logged: {ErrorId} - {Context}", errorEntry.Id, context);

            // 保存到文件
            await SaveErrorToFileAsync(errorEntry);

            // 发送用户通知（如果需要）
            await NotifyUserIfNecessaryAsync(errorEntry);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Failed to log error - this is a critical system issue");
        }
    }

    public async Task<List<ErrorLogEntry>> GetRecentErrorsAsync(int count = 50)
    {
        try
        {
            var errorFiles = Directory.GetFiles(_errorLogPath, "*.json")
                                   .OrderByDescending(f => File.GetCreationTime(f))
                                   .Take(count);

            var errors = new List<ErrorLogEntry>();

            foreach (var file in errorFiles)
            {
                try
                {
                    var json = await File.ReadAllTextAsync(file);
                    var error = JsonSerializer.Deserialize<ErrorLogEntry>(json, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    });
                    if (error != null)
                    {
                        errors.Add(error);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to read error log file: {File}", file);
                }
            }

            return errors.OrderByDescending(e => e.Timestamp).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve recent errors");
            return new List<ErrorLogEntry>();
        }
    }

    public async Task<ErrorStatistics> GetErrorStatisticsAsync(TimeSpan period)
    {
        try
        {
            var cutoffTime = DateTime.UtcNow - period;
            var recentErrors = await GetRecentErrorsAsync(1000); // 获取更多错误用于统计
            
            var relevantErrors = recentErrors.Where(e => e.Timestamp >= cutoffTime).ToList();

            return new ErrorStatistics
            {
                TotalErrors = relevantErrors.Count,
                ErrorsByType = relevantErrors.GroupBy(e => e.ExceptionType)
                                           .ToDictionary(g => g.Key, g => g.Count()),
                ErrorsByHour = relevantErrors.GroupBy(e => e.Timestamp.Hour)
                                           .ToDictionary(g => g.Key, g => g.Count()),
                MostCommonErrors = relevantErrors.GroupBy(e => e.Message)
                                               .OrderByDescending(g => g.Count())
                                               .Take(10)
                                               .ToDictionary(g => g.Key, g => g.Count()),
                Period = period,
                GeneratedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate error statistics");
            return new ErrorStatistics { Period = period, GeneratedAt = DateTime.UtcNow };
        }
    }

    public async Task CleanupOldErrorsAsync(TimeSpan retentionPeriod)
    {
        try
        {
            var cutoffTime = DateTime.UtcNow - retentionPeriod;
            var errorFiles = Directory.GetFiles(_errorLogPath, "*.json");
            var deletedCount = 0;

            foreach (var file in errorFiles)
            {
                try
                {
                    var creationTime = File.GetCreationTime(file);
                    if (creationTime < cutoffTime)
                    {
                        File.Delete(file);
                        deletedCount++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete old error log file: {File}", file);
                }
            }

            _logger.LogInformation("Cleaned up {Count} old error log files", deletedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup old error logs");
        }
    }

    private async Task SaveErrorToFileAsync(ErrorLogEntry errorEntry)
    {
        await _fileSemaphore.WaitAsync();
        try
        {
            var fileName = $"{errorEntry.Timestamp:yyyyMMdd_HHmmss}_{errorEntry.Id}.json";
            var filePath = Path.Combine(_errorLogPath, fileName);
            
            var json = JsonSerializer.Serialize(errorEntry, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await File.WriteAllTextAsync(filePath, json);
        }
        finally
        {
            _fileSemaphore.Release();
        }
    }

    private async Task NotifyUserIfNecessaryAsync(ErrorLogEntry errorEntry)
    {
        // 根据错误类型决定是否需要通知用户
        var criticalErrorTypes = new[]
        {
            nameof(OutOfMemoryException),
            nameof(StackOverflowException),
            nameof(AccessViolationException)
        };

        if (criticalErrorTypes.Contains(errorEntry.ExceptionType))
        {
            _logger.LogCritical("Critical error detected: {ErrorType} - {Message}", 
                errorEntry.ExceptionType, errorEntry.Message);
            
            // 在实际应用中，这里可以发送邮件、短信或其他通知
            // 目前只记录到日志
        }
    }

    public void Dispose()
    {
        _fileSemaphore?.Dispose();
    }
}

// 错误日志条目
public class ErrorLogEntry
{
    public string Id { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string ExceptionType { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? StackTrace { get; set; }
    public string? Context { get; set; }
    public Dictionary<string, object> AdditionalData { get; set; } = new();
    public string? InnerException { get; set; }
}

// 错误统计
public class ErrorStatistics
{
    public int TotalErrors { get; set; }
    public Dictionary<string, int> ErrorsByType { get; set; } = new();
    public Dictionary<int, int> ErrorsByHour { get; set; } = new();
    public Dictionary<string, int> MostCommonErrors { get; set; } = new();
    public TimeSpan Period { get; set; }
    public DateTime GeneratedAt { get; set; }
}