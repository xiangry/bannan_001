using MathComicGenerator.Shared.Interfaces;
using Microsoft.AspNetCore.Components;

namespace MathComicGenerator.Web.Components;

/// <summary>
/// 优化的Blazor组件基类，提供非阻塞的日志记录和性能监控功能
/// </summary>
public abstract class OptimizedComponentBase : ComponentBase, IDisposable
{
    [Inject] protected IAsyncLoggingService AsyncLogger { get; set; } = default!;
    [Inject] protected IUIPerformanceService PerformanceService { get; set; } = default!;
    [Inject] protected ILogger<OptimizedComponentBase> Logger { get; set; } = default!;

    private readonly Dictionary<string, string> _activeOperations = new();
    private bool _disposed = false;

    /// <summary>
    /// 组件名称，用于日志记录
    /// </summary>
    protected virtual string ComponentName => GetType().Name;

    protected override async Task OnInitializedAsync()
    {
        await LogComponentLifecycleAsync("Initialized");
        await base.OnInitializedAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await LogComponentLifecycleAsync("FirstRender");
        }
        await base.OnAfterRenderAsync(firstRender);
    }

    /// <summary>
    /// 记录用户操作
    /// </summary>
    protected async Task LogUserActionAsync(string action, object? data = null)
    {
        try
        {
            await AsyncLogger.LogUserActionAsync($"{ComponentName}.{action}", data);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to log user action: {Action}", action);
        }
    }

    /// <summary>
    /// 记录组件状态变化
    /// </summary>
    protected async Task LogStateChangeAsync(string from, string to, object? data = null)
    {
        try
        {
            await AsyncLogger.LogAsync("INFO", $"State change: {from} -> {to}", 
                new { component = ComponentName, from, to, data }, "STATE_CHANGE");
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to log state change");
        }
    }

    /// <summary>
    /// 记录API请求
    /// </summary>
    protected async Task LogApiRequestAsync(string method, string url, object? data = null)
    {
        try
        {
            await AsyncLogger.LogApiRequestAsync(method, url, data);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to log API request");
        }
    }

    /// <summary>
    /// 记录API响应
    /// </summary>
    protected async Task LogApiResponseAsync(string method, string url, int status, object? data = null, double? duration = null)
    {
        try
        {
            await AsyncLogger.LogApiResponseAsync(method, url, status, data, duration);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to log API response");
        }
    }

    /// <summary>
    /// 记录错误
    /// </summary>
    protected async Task LogErrorAsync(string message, Exception? exception = null, object? data = null)
    {
        try
        {
            await AsyncLogger.LogErrorAsync($"{ComponentName}: {message}", 
                exception != null ? new { exception = exception.ToString(), data } : data);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to log error: {Message}", message);
        }
    }

    /// <summary>
    /// 开始性能跟踪
    /// </summary>
    protected string StartPerformanceTracking(string operationName)
    {
        try
        {
            var fullOperationName = $"{ComponentName}.{operationName}";
            var operationId = PerformanceService.StartOperation(fullOperationName);
            
            if (!string.IsNullOrEmpty(operationId))
            {
                _activeOperations[operationName] = operationId;
            }
            
            return operationId;
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to start performance tracking for: {Operation}", operationName);
            return string.Empty;
        }
    }

    /// <summary>
    /// 结束性能跟踪
    /// </summary>
    protected void EndPerformanceTracking(string operationName)
    {
        try
        {
            if (_activeOperations.TryGetValue(operationName, out var operationId))
            {
                PerformanceService.EndOperation(operationId);
                _activeOperations.Remove(operationName);
            }
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to end performance tracking for: {Operation}", operationName);
        }
    }

    /// <summary>
    /// 执行带性能跟踪的操作
    /// </summary>
    protected async Task<T> ExecuteWithPerformanceTrackingAsync<T>(string operationName, Func<Task<T>> operation)
    {
        var operationId = StartPerformanceTracking(operationName);
        try
        {
            return await operation();
        }
        finally
        {
            if (!string.IsNullOrEmpty(operationId))
            {
                EndPerformanceTracking(operationName);
            }
        }
    }

    /// <summary>
    /// 执行带性能跟踪的操作（无返回值）
    /// </summary>
    protected async Task ExecuteWithPerformanceTrackingAsync(string operationName, Func<Task> operation)
    {
        var operationId = StartPerformanceTracking(operationName);
        try
        {
            await operation();
        }
        finally
        {
            if (!string.IsNullOrEmpty(operationId))
            {
                EndPerformanceTracking(operationName);
            }
        }
    }

    /// <summary>
    /// 安全的StateHasChanged调用，包含性能跟踪
    /// </summary>
    protected void SafeStateHasChanged()
    {
        try
        {
            var operationId = StartPerformanceTracking("StateHasChanged");
            StateHasChanged();
            EndPerformanceTracking("StateHasChanged");
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Error during StateHasChanged in {Component}", ComponentName);
        }
    }

    /// <summary>
    /// 显示用户通知
    /// </summary>
    protected async Task ShowNotificationAsync(string message, string type = "info")
    {
        try
        {
            await LogUserActionAsync("ShowNotification", new { message, type });
            // 这里可以实现实际的通知显示逻辑
            // 例如触发一个事件或调用父组件的方法
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to show notification: {Message}", message);
        }
    }

    /// <summary>
    /// 记录组件生命周期事件
    /// </summary>
    private async Task LogComponentLifecycleAsync(string lifecycle)
    {
        try
        {
            await AsyncLogger.LogAsync("DEBUG", $"Component lifecycle: {lifecycle}", 
                new { component = ComponentName, timestamp = DateTime.UtcNow }, "COMPONENT_LIFECYCLE");
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to log component lifecycle: {Lifecycle}", lifecycle);
        }
    }

    /// <summary>
    /// 验证输入并记录结果
    /// </summary>
    protected async Task<bool> ValidateInputAsync(string fieldName, object? value, Func<object?, (bool isValid, string? errorMessage)> validator)
    {
        try
        {
            var (isValid, errorMessage) = validator(value);
            
            await AsyncLogger.LogAsync(isValid ? "DEBUG" : "WARN", 
                $"Validation: {fieldName}", 
                new { fieldName, value, isValid, errorMessage }, 
                "VALIDATION");
            
            return isValid;
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Error during validation of {FieldName}", fieldName);
            return false;
        }
    }

    /// <summary>
    /// 处理异步操作的通用方法
    /// </summary>
    protected async Task HandleAsyncOperationAsync(string operationName, Func<Task> operation, 
        Action<Exception>? onError = null)
    {
        await ExecuteWithPerformanceTrackingAsync(operationName, async () =>
        {
            try
            {
                await operation();
            }
            catch (Exception ex)
            {
                await LogErrorAsync($"Error in {operationName}", ex);
                onError?.Invoke(ex);
                throw; // 重新抛出异常，让调用者处理
            }
        });
    }

    /// <summary>
    /// 处理异步操作的通用方法（带返回值）
    /// </summary>
    protected async Task<T?> HandleAsyncOperationAsync<T>(string operationName, Func<Task<T>> operation, 
        Func<Exception, T>? onError = null)
    {
        return await ExecuteWithPerformanceTrackingAsync(operationName, async () =>
        {
            try
            {
                return await operation();
            }
            catch (Exception ex)
            {
                await LogErrorAsync($"Error in {operationName}", ex);
                
                if (onError != null)
                {
                    return onError(ex);
                }
                
                throw; // 重新抛出异常，让调用者处理
            }
        });
    }

    public virtual void Dispose()
    {
        if (_disposed) return;
        
        try
        {
            // 结束所有活动的性能跟踪
            foreach (var kvp in _activeOperations.ToList())
            {
                EndPerformanceTracking(kvp.Key);
            }
            
            _activeOperations.Clear();
            
            // 记录组件销毁
            _ = Task.Run(async () =>
            {
                try
                {
                    await LogComponentLifecycleAsync("Disposed");
                }
                catch
                {
                    // 忽略销毁时的日志错误
                }
            });
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Error during component disposal");
        }
        finally
        {
            _disposed = true;
        }
    }
}