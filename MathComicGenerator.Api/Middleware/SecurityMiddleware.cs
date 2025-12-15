namespace MathComicGenerator.Api.Middleware;

public class SecurityMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SecurityMiddleware> _logger;
    private readonly SecurityConfiguration _config;

    public SecurityMiddleware(RequestDelegate next, ILogger<SecurityMiddleware> logger, IConfiguration configuration)
    {
        _next = next;
        _logger = logger;
        _config = configuration.GetSection("Security").Get<SecurityConfiguration>() ?? new SecurityConfiguration();
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // 添加安全头
        AddSecurityHeaders(context);

        // 检查速率限制
        if (!await CheckRateLimit(context))
        {
            context.Response.StatusCode = 429;
            await context.Response.WriteAsync("Too Many Requests");
            return;
        }

        // 验证来源
        if (!ValidateOrigin(context))
        {
            context.Response.StatusCode = 403;
            await context.Response.WriteAsync("Forbidden");
            return;
        }

        await _next(context);
    }

    private void AddSecurityHeaders(HttpContext context)
    {
        var response = context.Response;

        // 内容安全策略
        if (!response.Headers.ContainsKey("Content-Security-Policy"))
        {
            response.Headers["Content-Security-Policy"] = 
                "default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline'; img-src 'self' data: https:;";
        }

        // 严格传输安全
        if (!response.Headers.ContainsKey("Strict-Transport-Security"))
        {
            response.Headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";
        }

        // 推荐人策略
        if (!response.Headers.ContainsKey("Referrer-Policy"))
        {
            response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        }

        // 权限策略
        if (!response.Headers.ContainsKey("Permissions-Policy"))
        {
            response.Headers["Permissions-Policy"] = 
                "camera=(), microphone=(), geolocation=(), payment=()";
        }

        // 移除服务器信息
        response.Headers.Remove("Server");
        response.Headers.Remove("X-Powered-By");
    }

    private async Task<bool> CheckRateLimit(HttpContext context)
    {
        if (!_config.EnableRateLimit)
            return true;

        var clientIp = GetClientIpAddress(context);
        var key = $"rate_limit_{clientIp}";
        
        // 这里应该使用Redis或内存缓存来实现速率限制
        // 简化实现，实际应用中需要更复杂的逻辑
        var requestCount = GetRequestCount(key);
        
        if (requestCount > _config.MaxRequestsPerMinute)
        {
            _logger.LogWarning("Rate limit exceeded for IP: {ClientIp}", clientIp);
            return false;
        }

        IncrementRequestCount(key);
        return true;
    }

    private bool ValidateOrigin(HttpContext context)
    {
        if (!_config.EnableOriginValidation)
            return true;

        var origin = context.Request.Headers["Origin"].FirstOrDefault();
        var referer = context.Request.Headers["Referer"].FirstOrDefault();

        // 允许同源请求
        if (string.IsNullOrEmpty(origin) && string.IsNullOrEmpty(referer))
            return true;

        // 检查允许的来源
        var allowedOrigins = _config.AllowedOrigins;
        if (allowedOrigins.Contains("*"))
            return true;

        if (!string.IsNullOrEmpty(origin) && allowedOrigins.Contains(origin))
            return true;

        if (!string.IsNullOrEmpty(referer))
        {
            var refererUri = new Uri(referer);
            var refererOrigin = $"{refererUri.Scheme}://{refererUri.Host}:{refererUri.Port}";
            if (allowedOrigins.Contains(refererOrigin))
                return true;
        }

        _logger.LogWarning("Invalid origin detected: {Origin}, Referer: {Referer}", origin, referer);
        return false;
    }

    private string GetClientIpAddress(HttpContext context)
    {
        // 检查代理头
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            return forwardedFor.Split(',')[0].Trim();
        }

        var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
        {
            return realIp;
        }

        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    private int GetRequestCount(string key)
    {
        // 简化实现 - 实际应用中应使用Redis或分布式缓存
        // 这里只是演示，不会在多实例环境中正常工作
        return 0;
    }

    private void IncrementRequestCount(string key)
    {
        // 简化实现 - 实际应用中应使用Redis或分布式缓存
        // 这里只是演示
    }
}

public class SecurityConfiguration
{
    public bool EnableRateLimit { get; set; } = true;
    public int MaxRequestsPerMinute { get; set; } = 60;
    public bool EnableOriginValidation { get; set; } = true;
    public List<string> AllowedOrigins { get; set; } = new() { "*" };
    public bool EnableSecurityHeaders { get; set; } = true;
}