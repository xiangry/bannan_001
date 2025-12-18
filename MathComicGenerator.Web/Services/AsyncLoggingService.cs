using MathComicGenerator.Shared.Interfaces;
using Microsoft.JSInterop;
using System.Collections.Concurrent;
using System.Text.Json;

namespace MathComicGenerator.Web.Services;

/// <summary>
/// 异步日志服务实现，使用队列和批处理机制避免阻塞UI线程
/// </summary>
public class AsyncLoggingService : IAsyncLoggingService, IDisposable
{
    private readonly IJSRuntime _jsRuntime;
    private readonly ILogger<AsyncLoggingService> _logger;
    private readonly AsyncLoggingConfiguration _config;
    
    private readonly ConcurrentQueue<LogEntry> _logQueue = new();
    private readonly Timer _flushTimer;
    private readonly SemaphoreSlim _flushSemaphore = new(1, 1);
    
    private volatile bool _isEnabled = true;
    private volatile bool _isDisposed = false;
    
    // 统计信息
    private int _totalLogsProcessed = 0;
    private int _droppedLogs = 0;
    private DateTime _lastFlushTime = DateTime.UtcNow;
    private readonly List<double> _processingTimes = new();

    public AsyncLoggingService(
        IJSRuntime jsRuntime, 
        ILogger<AsyncLoggingService> logger,
        IConfiguration configuration)
    {
        _jsRuntime = jsRuntime;
        _logger = logger;
        
        // 加载配置
        _config = configuration.GetSection("AsyncLogging").Get<AsyncLoggingConfiguration>() 
                  ?? new AsyncLoggingConfiguration();

        // 启动定时刷新
        _flushTimer = new Timer(
            async _ => await FlushQueueAsync(), 
            null, 
            TimeSpan.FromMilliseconds(_config.FlushIntervalMs),
            TimeSpan.FromMilliseconds(_config.FlushIntervalMs));

        _logger.LogInformation("AsyncLoggingService initialized with config: {@Config}", _config);
    }

    public async Task LogAsync(string level, string message, object? data = null, string category = "")
    {
        if (!_isEnabled || _isDisposed) return;

        var logEntry = new LogEntry
        {
            Timestamp = DateTime.UtcNow,
            Level = level,
            Message = message,
            Data = data,
            Category = category,
            Type = LogEntryType.General
        };

        EnqueueLog(logEntry);
        await Task.CompletedTask; // 非阻塞返回
    }

    public async Task LogUserActionAsync(string action, object? data = null)
    {
        if (!_isEnabled || _isDisposed) return;

        var logEntry = new LogEntry
        {
            Timestamp = DateTime.UtcNow,
            Level = "INFO",
            Message = $"User action: {action}",
            Data = data,
            Category = "USER_ACTION",
            Type = LogEntryType.UserAction
        };

        EnqueueLog(logEntry);
        await Task.CompletedTask;
    }

    public async Task LogPerformanceAsync(string operation, double duration, object? details = null)
    {
        if (!_isEnabled || _isDisposed) return;

        var logEntry = new LogEntry
        {
            Timestamp = DateTime.UtcNow,
            Level = duration > 2000 ? "WARN" : "INFO", // 超过2秒的操作标记为警告
            Message = $"Performance: {operation}",
            Data = new { duration = $"{duration}ms", details },
            Category = "PERFORMANCE",
            Type = LogEntryType.Performance
        };

        EnqueueLog(logEntry);
        await Task.CompletedTask;
    }

    public async Task LogApiRequestAsync(string method, string url, object? data = null, object? headers = null)
    {
        if (!_isEnabled || _isDisposed) return;

        var logEntry = new LogEntry
        {
            Timestamp = DateTime.UtcNow,
            Level = "INFO",
            Message = $"API Request: {method} {url}",
            Data = new { method, url, data, headers },
            Category = "API_REQUEST",
            Type = LogEntryType.ApiRequest
        };

        EnqueueLog(logEntry);
        await Task.CompletedTask;
    }

    public async Task LogApiResponseAsync(string method, string url, int status, object? data = null, double? duration = null)
    {
        if (!_isEnabled || _isDisposed) return;

        var level = status >= 400 ? "ERROR" : "INFO";
        var logEntry = new LogEntry
        {
            Timestamp = DateTime.UtcNow,
            Level = level,
            Message = $"API Response: {method} {url} - {status}",
            Data = new { method, url, status, data, duration = duration.HasValue ? $"{duration}ms" : null },
            Category = "API_RESPONSE",
            Type = LogEntryType.ApiResponse
        };

        EnqueueLog(logEntry);
        await Task.CompletedTask;
    }

    public async Task LogErrorAsync(string message, object? error = null, string category = "")
    {
        if (!_isEnabled || _isDisposed) return;

        var logEntry = new LogEntry
        {
            Timestamp = DateTime.UtcNow,
            Level = "ERROR",
            Message = message,
            Data = error,
            Category = string.IsNullOrEmpty(category) ? "ERROR" : category,
            Type = LogEntryType.Error
        };

        EnqueueLog(logEntry);
        
        // 错误日志立即刷新到控制台
        if (_config.EnableConsoleOutput)
        {
            try
            {
                await _jsRuntime.InvokeVoidAsync("console.error", 
                    $"[{logEntry.Timestamp:HH:mm:ss}] [{logEntry.Category}] {logEntry.Message}", 
                    error);
            }
            catch
            {
                // 忽略JavaScript调用错误
            }
        }

        await Task.CompletedTask;
    }

    public void EnableLogging()
    {
        _isEnabled = true;
        _logger.LogInformation("Async logging enabled");
    }

    public void DisableLogging()
    {
        _isEnabled = false;
        _logger.LogInformation("Async logging disabled");
    }

    public async Task FlushAsync()
    {
        if (_isDisposed) return;
        await FlushQueueAsync();
    }

    public async Task<LoggingStatistics> GetStatisticsAsync()
    {
        return new LoggingStatistics
        {
            TotalLogsProcessed = _totalLogsProcessed,
            QueuedLogs = _logQueue.Count,
            DroppedLogs = _droppedLogs,
            AverageProcessingTime = _processingTimes.Count > 0 ? _processingTimes.Average() : 0,
            LastFlushTime = _lastFlushTime,
            IsEnabled = _isEnabled
        };
    }

    private void EnqueueLog(LogEntry logEntry)
    {
        if (_logQueue.Count >= _config.MaxQueueSize)
        {
            // 队列满了，丢弃最旧的日志
            if (_logQueue.TryDequeue(out _))
            {
                Interlocked.Increment(ref _droppedLogs);
            }
        }

        _logQueue.Enqueue(logEntry);
    }

    private async Task FlushQueueAsync()
    {
        if (_isDisposed || !_flushSemaphore.Wait(100)) return;

        try
        {
            var startTime = DateTime.UtcNow;
            var batch = new List<LogEntry>();
            
            // 收集一批日志
            for (int i = 0; i < _config.BatchSize && _logQueue.TryDequeue(out var logEntry); i++)
            {
                batch.Add(logEntry);
            }

            if (batch.Count == 0) return;

            // 批量处理日志
            await ProcessLogBatch(batch);
            
            // 更新统计信息
            var processingTime = (DateTime.UtcNow - startTime).TotalMilliseconds;
            lock (_processingTimes)
            {
                _processingTimes.Add(processingTime);
                if (_processingTimes.Count > 100) // 只保留最近100次的处理时间
                {
                    _processingTimes.RemoveAt(0);
                }
            }
            
            Interlocked.Add(ref _totalLogsProcessed, batch.Count);
            _lastFlushTime = DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error flushing log queue");
        }
        finally
        {
            _flushSemaphore.Release();
        }
    }

    private async Task ProcessLogBatch(List<LogEntry> batch)
    {
        if (!_config.EnableConsoleOutput) return;

        try
        {
            // 只处理重要的日志类型
            var importantLogs = batch.Where(log => 
                log.Type == LogEntryType.Error || 
                log.Type == LogEntryType.Performance ||
                (log.Type == LogEntryType.ApiResponse && log.Level == "ERROR"))
                .ToList();

            foreach (var log in importantLogs)
            {
                var consoleMethod = log.Level switch
                {
                    "ERROR" => "console.error",
                    "WARN" => "console.warn",
                    _ => "console.log"
                };

                await _jsRuntime.InvokeVoidAsync(consoleMethod,
                    $"[{log.Timestamp:HH:mm:ss}] [{log.Category}] {log.Message}",
                    log.Data);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to output logs to console");
        }
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        
        _isDisposed = true;
        _flushTimer?.Dispose();
        
        // 最后一次刷新
        try
        {
            FlushQueueAsync().Wait(TimeSpan.FromSeconds(5));
        }
        catch
        {
            // 忽略清理时的错误
        }
        
        _flushSemaphore?.Dispose();
    }
}

/// <summary>
/// 日志条目
/// </summary>
internal class LogEntry
{
    public DateTime Timestamp { get; set; }
    public string Level { get; set; } = "";
    public string Message { get; set; } = "";
    public object? Data { get; set; }
    public string Category { get; set; } = "";
    public LogEntryType Type { get; set; }
}

/// <summary>
/// 日志条目类型
/// </summary>
internal enum LogEntryType
{
    General,
    UserAction,
    Performance,
    ApiRequest,
    ApiResponse,
    Error
}

/// <summary>
/// 异步日志配置
/// </summary>
public class AsyncLoggingConfiguration
{
    public bool Enabled { get; set; } = true;
    public int BatchSize { get; set; } = 50;
    public int FlushIntervalMs { get; set; } = 1000;
    public int MaxQueueSize { get; set; } = 5000;
    public bool EnableConsoleOutput { get; set; } = false; // 默认禁用控制台输出以提高性能
}