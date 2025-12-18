using MathComicGenerator.Shared.Interfaces;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace MathComicGenerator.Web.Services;

/// <summary>
/// UI性能监控服务实现
/// </summary>
public class UIPerformanceService : IUIPerformanceService
{
    private readonly ILogger<UIPerformanceService> _logger;
    private readonly IAsyncLoggingService _asyncLogger;
    private readonly UIPerformanceConfiguration _config;
    
    private readonly ConcurrentDictionary<string, ActiveOperationInfo> _activeOperations = new();
    private readonly ConcurrentQueue<PerformanceMetrics> _completedOperations = new();
    private readonly ConcurrentDictionary<string, TimeSpan> _performanceThresholds = new();
    private readonly ConcurrentQueue<PerformanceWarning> _warnings = new();
    
    private volatile bool _isEnabled = true;
    private readonly object _statsLock = new();
    private int _totalOperations = 0;
    private readonly List<TimeSpan> _recentResponseTimes = new();

    public UIPerformanceService(
        ILogger<UIPerformanceService> logger,
        IAsyncLoggingService asyncLogger,
        IConfiguration configuration)
    {
        _logger = logger;
        _asyncLogger = asyncLogger;
        
        _config = configuration.GetSection("UIPerformance").Get<UIPerformanceConfiguration>() 
                  ?? new UIPerformanceConfiguration();

        // 设置默认阈值
        SetDefaultThresholds();
        
        _logger.LogInformation("UIPerformanceService initialized with monitoring enabled: {Enabled}", _isEnabled);
    }

    public string StartOperation(string operationName)
    {
        if (!_isEnabled) return string.Empty;

        var operationId = Guid.NewGuid().ToString("N")[..8];
        var operationInfo = new ActiveOperationInfo
        {
            Id = operationId,
            Name = operationName,
            StartTime = DateTime.UtcNow,
            Stopwatch = Stopwatch.StartNew()
        };

        _activeOperations.TryAdd(operationId, operationInfo);
        
        _ = _asyncLogger.LogPerformanceAsync($"Operation started: {operationName}", 0, new { operationId });
        
        return operationId;
    }

    public void EndOperation(string operationId)
    {
        if (!_isEnabled || string.IsNullOrEmpty(operationId)) return;

        if (_activeOperations.TryRemove(operationId, out var operationInfo))
        {
            operationInfo.Stopwatch.Stop();
            var duration = operationInfo.Stopwatch.Elapsed;
            
            var metrics = new PerformanceMetrics
            {
                OperationName = operationInfo.Name,
                StartTime = operationInfo.StartTime,
                EndTime = DateTime.UtcNow,
                Duration = duration,
                Metadata = new Dictionary<string, object>
                {
                    { "operationId", operationId }
                }
            };

            // 检查是否超过阈值
            if (_performanceThresholds.TryGetValue(operationInfo.Name, out var threshold))
            {
                metrics.ExceededThreshold = duration > threshold;
                
                if (metrics.ExceededThreshold)
                {
                    var warning = new PerformanceWarning
                    {
                        Timestamp = DateTime.UtcNow,
                        OperationName = operationInfo.Name,
                        ActualDuration = duration,
                        ExpectedThreshold = threshold,
                        Severity = duration > threshold * 2 ? "HIGH" : "MEDIUM",
                        Message = $"Operation '{operationInfo.Name}' took {duration.TotalMilliseconds:F0}ms, exceeding threshold of {threshold.TotalMilliseconds:F0}ms"
                    };
                    
                    _warnings.Enqueue(warning);
                    
                    // 限制警告队列大小
                    while (_warnings.Count > 100)
                    {
                        _warnings.TryDequeue(out _);
                    }
                }
            }

            // 添加到完成操作队列
            _completedOperations.Enqueue(metrics);
            
            // 限制队列大小
            while (_completedOperations.Count > 1000)
            {
                _completedOperations.TryDequeue(out _);
            }

            // 更新统计信息
            lock (_statsLock)
            {
                _totalOperations++;
                _recentResponseTimes.Add(duration);
                
                // 只保留最近100次的响应时间
                if (_recentResponseTimes.Count > 100)
                {
                    _recentResponseTimes.RemoveAt(0);
                }
            }

            _ = _asyncLogger.LogPerformanceAsync($"Operation completed: {operationInfo.Name}", 
                duration.TotalMilliseconds, 
                new { 
                    operationId, 
                    exceededThreshold = metrics.ExceededThreshold,
                    thresholdMs = threshold.TotalMilliseconds
                });
        }
    }

    public async Task RecordOperationAsync(string operationName, TimeSpan duration, Dictionary<string, object>? metadata = null)
    {
        if (!_isEnabled) return;

        var metrics = new PerformanceMetrics
        {
            OperationName = operationName,
            StartTime = DateTime.UtcNow - duration,
            EndTime = DateTime.UtcNow,
            Duration = duration,
            Metadata = metadata ?? new Dictionary<string, object>()
        };

        // 检查阈值
        if (_performanceThresholds.TryGetValue(operationName, out var threshold))
        {
            metrics.ExceededThreshold = duration > threshold;
        }

        _completedOperations.Enqueue(metrics);
        
        // 限制队列大小
        while (_completedOperations.Count > 1000)
        {
            _completedOperations.TryDequeue(out _);
        }

        await _asyncLogger.LogPerformanceAsync($"Operation recorded: {operationName}", 
            duration.TotalMilliseconds, metadata);
    }

    public void SetPerformanceThreshold(string operation, TimeSpan threshold)
    {
        _performanceThresholds.AddOrUpdate(operation, threshold, (key, oldValue) => threshold);
        _logger.LogInformation("Performance threshold set for {Operation}: {Threshold}ms", 
            operation, threshold.TotalMilliseconds);
    }

    public async Task<PerformanceReport> GetPerformanceReportAsync()
    {
        var report = new PerformanceReport
        {
            GeneratedAt = DateTime.UtcNow,
            ReportPeriod = TimeSpan.FromHours(1) // 默认1小时报告期
        };

        // 获取最近的操作数据
        var recentOperations = _completedOperations.ToArray()
            .Where(op => op.EndTime > DateTime.UtcNow.AddHours(-1))
            .ToList();

        // 按操作名称分组统计
        var operationGroups = recentOperations.GroupBy(op => op.OperationName);
        
        foreach (var group in operationGroups)
        {
            var durations = group.Select(op => op.Duration).OrderBy(d => d).ToList();
            
            var summary = new OperationSummary
            {
                OperationName = group.Key,
                TotalExecutions = durations.Count,
                AverageDuration = TimeSpan.FromMilliseconds(durations.Average(d => d.TotalMilliseconds)),
                MinDuration = durations.First(),
                MaxDuration = durations.Last(),
                MedianDuration = durations[durations.Count / 2],
                ThresholdViolations = group.Count(op => op.ExceededThreshold),
                SuccessRate = 1.0 - (double)group.Count(op => op.ExceededThreshold) / durations.Count
            };
            
            report.OperationSummaries.Add(summary);
        }

        // 获取最近的警告
        report.Warnings = _warnings.ToArray()
            .Where(w => w.Timestamp > DateTime.UtcNow.AddHours(-1))
            .OrderByDescending(w => w.Timestamp)
            .Take(50)
            .ToList();

        // 整体统计
        report.Overall = new OverallStatistics
        {
            TotalOperations = recentOperations.Count,
            AverageResponseTime = recentOperations.Count > 0 
                ? TimeSpan.FromMilliseconds(recentOperations.Average(op => op.Duration.TotalMilliseconds))
                : TimeSpan.Zero,
            OverallSuccessRate = recentOperations.Count > 0 
                ? 1.0 - (double)recentOperations.Count(op => op.ExceededThreshold) / recentOperations.Count
                : 1.0,
            TotalWarnings = report.Warnings.Count,
            SlowestOperations = report.OperationSummaries
                .OrderByDescending(s => s.AverageDuration)
                .Take(5)
                .Select(s => s.OperationName)
                .ToList(),
            MostFrequentOperations = report.OperationSummaries
                .OrderByDescending(s => s.TotalExecutions)
                .Take(5)
                .Select(s => s.OperationName)
                .ToList()
        };

        return report;
    }

    public async Task<RealTimeMetrics> GetRealTimeMetricsAsync()
    {
        var now = DateTime.UtcNow;
        
        var metrics = new RealTimeMetrics
        {
            Timestamp = now,
            ActiveOperations = _activeOperations.Values.Select(op => new ActiveOperation
            {
                Id = op.Id,
                Name = op.Name,
                StartTime = op.StartTime,
                ElapsedTime = now - op.StartTime,
                Status = "Running"
            }).ToList(),
            QueuedOperations = 0, // 暂时不实现队列
            AverageResponseTime = GetAverageResponseTime(),
            OperationsPerMinute = GetOperationsPerMinute(),
            SystemLoad = GetSystemLoad()
        };

        return metrics;
    }

    public async Task ClearMetricsAsync()
    {
        _completedOperations.Clear();
        _warnings.Clear();
        
        lock (_statsLock)
        {
            _totalOperations = 0;
            _recentResponseTimes.Clear();
        }

        await _asyncLogger.LogAsync("INFO", "Performance metrics cleared", null, "PERFORMANCE");
    }

    public void EnableMonitoring()
    {
        _isEnabled = true;
        _logger.LogInformation("Performance monitoring enabled");
    }

    public void DisableMonitoring()
    {
        _isEnabled = false;
        _logger.LogInformation("Performance monitoring disabled");
    }

    private void SetDefaultThresholds()
    {
        // 设置默认的性能阈值
        _performanceThresholds.TryAdd("prompt_generation", TimeSpan.FromMilliseconds(500));
        _performanceThresholds.TryAdd("comic_generation", TimeSpan.FromMilliseconds(1000));
        _performanceThresholds.TryAdd("ui_update", TimeSpan.FromMilliseconds(100));
        _performanceThresholds.TryAdd("api_request", TimeSpan.FromMilliseconds(300));
        _performanceThresholds.TryAdd("form_submit", TimeSpan.FromMilliseconds(200));
        _performanceThresholds.TryAdd("component_render", TimeSpan.FromMilliseconds(50));
    }

    private TimeSpan GetAverageResponseTime()
    {
        lock (_statsLock)
        {
            return _recentResponseTimes.Count > 0 
                ? TimeSpan.FromMilliseconds(_recentResponseTimes.Average(t => t.TotalMilliseconds))
                : TimeSpan.Zero;
        }
    }

    private int GetOperationsPerMinute()
    {
        var oneMinuteAgo = DateTime.UtcNow.AddMinutes(-1);
        return _completedOperations.Count(op => op.EndTime > oneMinuteAgo);
    }

    private double GetSystemLoad()
    {
        // 简化的系统负载计算
        var activeCount = _activeOperations.Count;
        var warningCount = _warnings.Count(w => w.Timestamp > DateTime.UtcNow.AddMinutes(-5));
        
        return Math.Min(1.0, (activeCount * 0.1) + (warningCount * 0.05));
    }
}

/// <summary>
/// 活动操作信息
/// </summary>
internal class ActiveOperationInfo
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public DateTime StartTime { get; set; }
    public Stopwatch Stopwatch { get; set; } = new();
}

/// <summary>
/// UI性能配置
/// </summary>
public class UIPerformanceConfiguration
{
    public bool EnableAsyncLogging { get; set; } = true;
    public int LoggingQueueSize { get; set; } = 1000;
    public int UIUpdateThresholdMs { get; set; } = 500;
    public bool PerformanceMonitoring { get; set; } = true;
    public bool DiagnosticMode { get; set; } = false;
}