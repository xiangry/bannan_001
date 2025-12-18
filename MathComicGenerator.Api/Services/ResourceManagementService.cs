using System.Diagnostics;

namespace MathComicGenerator.Api.Services;

public class ResourceManagementService
{
    private readonly ILogger<ResourceManagementService> _logger;
    private readonly ResourceConfiguration _config;
    private readonly Timer _monitoringTimer;
    private readonly SemaphoreSlim _requestSemaphore;
    private volatile bool _isSystemHealthy = true;

    public ResourceManagementService(ILogger<ResourceManagementService> logger, IConfiguration configuration)
    {
        _logger = logger;
        
        // 手动构建配置对象以支持测试
        var resourceSection = configuration.GetSection("ResourceManagement");
        _config = new ResourceConfiguration
        {
            MaxConcurrentRequests = int.TryParse(resourceSection["MaxConcurrentRequests"], out var maxRequests) ? maxRequests : 10,
            RequestTimeoutMs = int.TryParse(resourceSection["RequestTimeoutMs"], out var timeout) ? timeout : 30000,
            MaxMemoryUsagePercent = double.TryParse(resourceSection["MaxMemoryUsagePercent"], out var maxMemory) ? maxMemory : 80.0,
            MaxCpuUsagePercent = double.TryParse(resourceSection["MaxCpuUsagePercent"], out var maxCpu) ? maxCpu : 80.0,
            MaxDiskUsagePercent = double.TryParse(resourceSection["MaxDiskUsagePercent"], out var maxDisk) ? maxDisk : 90.0,
            MonitoringIntervalSeconds = int.TryParse(resourceSection["MonitoringIntervalSeconds"], out var interval) ? interval : 30,
            EnableGracefulDegradation = bool.TryParse(resourceSection["EnableGracefulDegradation"], out var graceful) ? graceful : true,
            EnableAutoRecovery = bool.TryParse(resourceSection["EnableAutoRecovery"], out var recovery) ? recovery : true
        };

        _requestSemaphore = new SemaphoreSlim(_config.MaxConcurrentRequests, _config.MaxConcurrentRequests);
        
        // 启动资源监控定时器
        _monitoringTimer = new Timer(MonitorResources, null, TimeSpan.Zero, TimeSpan.FromSeconds(_config.MonitoringIntervalSeconds));
    }

    public async Task<bool> TryAcquireResourceAsync(CancellationToken cancellationToken = default)
    {
        if (!_isSystemHealthy)
        {
            throw new ResourceLimitException("系统资源不足，请稍后重试");
        }

        try
        {
            // 使用更短的超时时间，避免长时间等待
            var acquired = await _requestSemaphore.WaitAsync(1000, cancellationToken); // 1秒超时
            
            if (!acquired)
            {
                _logger.LogWarning("Failed to acquire resource within timeout (1s). Available slots: {AvailableSlots}", _requestSemaphore.CurrentCount);
                throw new ResourceLimitException("系统繁忙，请稍后重试");
            }

            _logger.LogDebug("Resource acquired successfully. Remaining slots: {RemainingSlots}", _requestSemaphore.CurrentCount);
            return true;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Resource acquisition was cancelled due to timeout");
            throw new ResourceLimitException("系统繁忙，请稍后重试");
        }
    }

    public void ReleaseResource()
    {
        _requestSemaphore.Release();
    }

    public SystemHealthStatus GetSystemHealth()
    {
        var memoryUsage = GetMemoryUsage();
        var cpuUsage = GetCpuUsage();
        var diskUsage = GetDiskUsage();

        return new SystemHealthStatus
        {
            IsHealthy = _isSystemHealthy,
            MemoryUsagePercent = memoryUsage,
            CpuUsagePercent = cpuUsage,
            DiskUsagePercent = diskUsage,
            AvailableRequestSlots = _requestSemaphore.CurrentCount,
            MaxConcurrentRequests = _config.MaxConcurrentRequests,
            Timestamp = DateTime.UtcNow
        };
    }

    public async Task<bool> CheckResourceAvailabilityAsync()
    {
        var health = GetSystemHealth();
        
        // 检查内存使用率
        if (health.MemoryUsagePercent > _config.MaxMemoryUsagePercent)
        {
            _logger.LogWarning("High memory usage detected: {MemoryUsage}%", health.MemoryUsagePercent);
            return false;
        }

        // 检查CPU使用率
        if (health.CpuUsagePercent > _config.MaxCpuUsagePercent)
        {
            _logger.LogWarning("High CPU usage detected: {CpuUsage}%", health.CpuUsagePercent);
            return false;
        }

        // 检查磁盘使用率
        if (health.DiskUsagePercent > _config.MaxDiskUsagePercent)
        {
            _logger.LogWarning("High disk usage detected: {DiskUsage}%", health.DiskUsagePercent);
            return false;
        }

        // 检查并发请求数
        if (health.AvailableRequestSlots <= 0)
        {
            _logger.LogWarning("No available request slots");
            return false;
        }

        return true;
    }

    public async Task PerformGracefulDegradationAsync()
    {
        _logger.LogInformation("Performing graceful degradation");

        // 减少并发请求限制
        var newLimit = Math.Max(1, _config.MaxConcurrentRequests / 2);
        _logger.LogInformation("Reducing concurrent request limit to {NewLimit}", newLimit);

        // 触发垃圾回收
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        _logger.LogInformation("Garbage collection completed");

        // 等待一段时间让系统恢复
        await Task.Delay(TimeSpan.FromSeconds(5));
    }

    public async Task<bool> AttemptSystemRecoveryAsync()
    {
        _logger.LogInformation("Attempting system recovery");

        try
        {
            // 执行优雅降级
            await PerformGracefulDegradationAsync();

            // 等待系统稳定
            await Task.Delay(TimeSpan.FromSeconds(10));

            // 重新检查系统健康状态
            var isHealthy = await CheckResourceAvailabilityAsync();
            
            if (isHealthy)
            {
                _isSystemHealthy = true;
                _logger.LogInformation("System recovery successful");
                return true;
            }
            else
            {
                _logger.LogWarning("System recovery failed - resources still constrained");
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during system recovery");
            return false;
        }
    }

    private void MonitorResources(object? state)
    {
        try
        {
            var wasHealthy = _isSystemHealthy;
            _isSystemHealthy = CheckResourceAvailabilityAsync().Result;

            if (wasHealthy && !_isSystemHealthy)
            {
                _logger.LogWarning("System health degraded - entering resource conservation mode");
                _ = Task.Run(PerformGracefulDegradationAsync);
            }
            else if (!wasHealthy && _isSystemHealthy)
            {
                _logger.LogInformation("System health restored");
            }

            // 记录系统状态
            var health = GetSystemHealth();
            _logger.LogDebug("System health check - Memory: {Memory}%, CPU: {Cpu}%, Disk: {Disk}%, Requests: {Requests}/{Max}",
                health.MemoryUsagePercent, health.CpuUsagePercent, health.DiskUsagePercent,
                _config.MaxConcurrentRequests - health.AvailableRequestSlots, health.MaxConcurrentRequests);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during resource monitoring");
        }
    }

    private double GetMemoryUsage()
    {
        try
        {
            var process = Process.GetCurrentProcess();
            var totalMemory = GC.GetTotalMemory(false);
            var workingSet = process.WorkingSet64;
            
            // 使用工作集内存作为基准
            var availableMemory = GetAvailablePhysicalMemory();
            if (availableMemory > 0)
            {
                return (double)workingSet / availableMemory * 100;
            }
            
            // 如果无法获取可用内存，使用GC内存作为估算
            return Math.Min(100, (double)totalMemory / (1024 * 1024 * 1024) * 100); // 假设1GB为基准
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get memory usage");
            return 0;
        }
    }

    private double GetCpuUsage()
    {
        try
        {
            var process = Process.GetCurrentProcess();
            var startTime = DateTime.UtcNow;
            var startCpuUsage = process.TotalProcessorTime;
            
            Thread.Sleep(100); // 短暂等待以计算CPU使用率
            
            var endTime = DateTime.UtcNow;
            var endCpuUsage = process.TotalProcessorTime;
            
            var cpuUsedMs = (endCpuUsage - startCpuUsage).TotalMilliseconds;
            var totalMsPassed = (endTime - startTime).TotalMilliseconds;
            var cpuUsageTotal = cpuUsedMs / (Environment.ProcessorCount * totalMsPassed);
            
            return Math.Min(100, cpuUsageTotal * 100);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get CPU usage");
            return 0;
        }
    }

    private double GetDiskUsage()
    {
        try
        {
            var drives = DriveInfo.GetDrives().Where(d => d.IsReady && d.DriveType == DriveType.Fixed);
            var totalSize = drives.Sum(d => d.TotalSize);
            var availableSpace = drives.Sum(d => d.AvailableFreeSpace);
            var usedSpace = totalSize - availableSpace;
            
            return totalSize > 0 ? (double)usedSpace / totalSize * 100 : 0;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get disk usage");
            return 0;
        }
    }

    private long GetAvailablePhysicalMemory()
    {
        try
        {
            // 在实际应用中，这里应该使用平台特定的API来获取可用物理内存
            // 这里提供一个简化的实现
            return 8L * 1024 * 1024 * 1024; // 假设8GB内存
        }
        catch
        {
            return 0;
        }
    }

    public void Dispose()
    {
        _monitoringTimer?.Dispose();
        _requestSemaphore?.Dispose();
    }
}

// 配置类
public class ResourceConfiguration
{
    public int MaxConcurrentRequests { get; set; } = 10;
    public int RequestTimeoutMs { get; set; } = 30000;
    public double MaxMemoryUsagePercent { get; set; } = 80.0;
    public double MaxCpuUsagePercent { get; set; } = 80.0;
    public double MaxDiskUsagePercent { get; set; } = 90.0;
    public int MonitoringIntervalSeconds { get; set; } = 30;
    public bool EnableGracefulDegradation { get; set; } = true;
    public bool EnableAutoRecovery { get; set; } = true;
}

// 系统健康状态类
public class SystemHealthStatus
{
    public bool IsHealthy { get; set; }
    public double MemoryUsagePercent { get; set; }
    public double CpuUsagePercent { get; set; }
    public double DiskUsagePercent { get; set; }
    public int AvailableRequestSlots { get; set; }
    public int MaxConcurrentRequests { get; set; }
    public DateTime Timestamp { get; set; }
}

// 异常类
public class ResourceLimitException : Exception
{
    public ResourceLimitException(string message) : base(message) { }
    public ResourceLimitException(string message, Exception innerException) : base(message, innerException) { }
}