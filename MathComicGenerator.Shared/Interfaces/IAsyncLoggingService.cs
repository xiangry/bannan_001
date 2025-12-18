using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MathComicGenerator.Shared.Interfaces;

/// <summary>
/// 异步日志服务接口，提供非阻塞的日志记录功能
/// </summary>
public interface IAsyncLoggingService
{
    /// <summary>
    /// 异步记录日志
    /// </summary>
    /// <param name="level">日志级别</param>
    /// <param name="message">日志消息</param>
    /// <param name="data">附加数据</param>
    /// <param name="category">日志分类</param>
    Task LogAsync(string level, string message, object? data = null, string category = "");

    /// <summary>
    /// 记录用户操作
    /// </summary>
    /// <param name="action">操作名称</param>
    /// <param name="data">操作数据</param>
    Task LogUserActionAsync(string action, object? data = null);

    /// <summary>
    /// 记录性能指标
    /// </summary>
    /// <param name="operation">操作名称</param>
    /// <param name="duration">持续时间（毫秒）</param>
    /// <param name="details">详细信息</param>
    Task LogPerformanceAsync(string operation, double duration, object? details = null);

    /// <summary>
    /// 记录API请求
    /// </summary>
    /// <param name="method">HTTP方法</param>
    /// <param name="url">请求URL</param>
    /// <param name="data">请求数据</param>
    /// <param name="headers">请求头</param>
    Task LogApiRequestAsync(string method, string url, object? data = null, object? headers = null);

    /// <summary>
    /// 记录API响应
    /// </summary>
    /// <param name="method">HTTP方法</param>
    /// <param name="url">请求URL</param>
    /// <param name="status">响应状态码</param>
    /// <param name="data">响应数据</param>
    /// <param name="duration">请求持续时间</param>
    Task LogApiResponseAsync(string method, string url, int status, object? data = null, double? duration = null);

    /// <summary>
    /// 记录错误
    /// </summary>
    /// <param name="message">错误消息</param>
    /// <param name="error">错误对象或详情</param>
    /// <param name="category">错误分类</param>
    Task LogErrorAsync(string message, object? error = null, string category = "");

    /// <summary>
    /// 启用日志记录
    /// </summary>
    void EnableLogging();

    /// <summary>
    /// 禁用日志记录
    /// </summary>
    void DisableLogging();

    /// <summary>
    /// 刷新日志队列，确保所有待处理的日志都被写入
    /// </summary>
    Task FlushAsync();

    /// <summary>
    /// 获取日志统计信息
    /// </summary>
    Task<LoggingStatistics> GetStatisticsAsync();
}

/// <summary>
/// 日志统计信息
/// </summary>
public class LoggingStatistics
{
    public int TotalLogsProcessed { get; set; }
    public int QueuedLogs { get; set; }
    public int DroppedLogs { get; set; }
    public double AverageProcessingTime { get; set; }
    public DateTime LastFlushTime { get; set; }
    public bool IsEnabled { get; set; }
}