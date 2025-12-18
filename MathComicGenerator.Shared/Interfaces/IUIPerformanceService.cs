using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MathComicGenerator.Shared.Interfaces;

/// <summary>
/// UI性能监控服务接口
/// </summary>
public interface IUIPerformanceService
{
    /// <summary>
    /// 开始监控操作
    /// </summary>
    /// <param name="operationName">操作名称</param>
    /// <returns>操作ID，用于结束监控</returns>
    string StartOperation(string operationName);

    /// <summary>
    /// 结束监控操作
    /// </summary>
    /// <param name="operationId">操作ID</param>
    void EndOperation(string operationId);

    /// <summary>
    /// 记录操作完成时间
    /// </summary>
    /// <param name="operationName">操作名称</param>
    /// <param name="duration">持续时间</param>
    /// <param name="metadata">附加元数据</param>
    Task RecordOperationAsync(string operationName, TimeSpan duration, Dictionary<string, object>? metadata = null);

    /// <summary>
    /// 设置性能阈值
    /// </summary>
    /// <param name="operation">操作名称</param>
    /// <param name="threshold">阈值</param>
    void SetPerformanceThreshold(string operation, TimeSpan threshold);

    /// <summary>
    /// 获取性能报告
    /// </summary>
    Task<PerformanceReport> GetPerformanceReportAsync();

    /// <summary>
    /// 获取实时性能指标
    /// </summary>
    Task<RealTimeMetrics> GetRealTimeMetricsAsync();

    /// <summary>
    /// 清除性能数据
    /// </summary>
    Task ClearMetricsAsync();

    /// <summary>
    /// 启用性能监控
    /// </summary>
    void EnableMonitoring();

    /// <summary>
    /// 禁用性能监控
    /// </summary>
    void DisableMonitoring();
}

/// <summary>
/// 性能指标数据
/// </summary>
public class PerformanceMetrics
{
    public string OperationName { get; set; } = "";
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public TimeSpan Duration { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
    public bool ExceededThreshold { get; set; }
}

/// <summary>
/// 性能报告
/// </summary>
public class PerformanceReport
{
    public DateTime GeneratedAt { get; set; }
    public TimeSpan ReportPeriod { get; set; }
    public List<OperationSummary> OperationSummaries { get; set; } = new();
    public List<PerformanceWarning> Warnings { get; set; } = new();
    public OverallStatistics Overall { get; set; } = new();
}

/// <summary>
/// 操作汇总
/// </summary>
public class OperationSummary
{
    public string OperationName { get; set; } = "";
    public int TotalExecutions { get; set; }
    public TimeSpan AverageDuration { get; set; }
    public TimeSpan MinDuration { get; set; }
    public TimeSpan MaxDuration { get; set; }
    public TimeSpan MedianDuration { get; set; }
    public int ThresholdViolations { get; set; }
    public double SuccessRate { get; set; }
}

/// <summary>
/// 性能警告
/// </summary>
public class PerformanceWarning
{
    public DateTime Timestamp { get; set; }
    public string OperationName { get; set; } = "";
    public TimeSpan ActualDuration { get; set; }
    public TimeSpan ExpectedThreshold { get; set; }
    public string Severity { get; set; } = "";
    public string Message { get; set; } = "";
}

/// <summary>
/// 整体统计
/// </summary>
public class OverallStatistics
{
    public int TotalOperations { get; set; }
    public TimeSpan AverageResponseTime { get; set; }
    public double OverallSuccessRate { get; set; }
    public int TotalWarnings { get; set; }
    public List<string> SlowestOperations { get; set; } = new();
    public List<string> MostFrequentOperations { get; set; } = new();
}

/// <summary>
/// 实时性能指标
/// </summary>
public class RealTimeMetrics
{
    public DateTime Timestamp { get; set; }
    public List<ActiveOperation> ActiveOperations { get; set; } = new();
    public int QueuedOperations { get; set; }
    public TimeSpan AverageResponseTime { get; set; }
    public int OperationsPerMinute { get; set; }
    public double SystemLoad { get; set; }
}

/// <summary>
/// 活动操作
/// </summary>
public class ActiveOperation
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public DateTime StartTime { get; set; }
    public TimeSpan ElapsedTime { get; set; }
    public string Status { get; set; } = "";
}