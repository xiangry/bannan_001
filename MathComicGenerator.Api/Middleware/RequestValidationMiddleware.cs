using System.Text.Json;

namespace MathComicGenerator.Api.Middleware;

public class RequestValidationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestValidationMiddleware> _logger;

    public RequestValidationMiddleware(RequestDelegate next, ILogger<RequestValidationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // 记录请求信息
        _logger.LogInformation("Processing request: {Method} {Path}", 
            context.Request.Method, context.Request.Path);

        // 验证Content-Type（对于POST/PUT请求）
        if (IsJsonRequest(context) && !HasValidContentType(context))
        {
            await WriteErrorResponse(context, 400, "Invalid Content-Type. Expected application/json.");
            return;
        }

        // 验证请求大小
        if (context.Request.ContentLength > 10 * 1024 * 1024) // 10MB限制
        {
            await WriteErrorResponse(context, 413, "Request too large. Maximum size is 10MB.");
            return;
        }

        // 添加请求ID用于追踪
        if (!context.Request.Headers.ContainsKey("X-Request-ID"))
        {
            context.Request.Headers.Add("X-Request-ID", Guid.NewGuid().ToString());
        }

        var requestId = context.Request.Headers["X-Request-ID"].FirstOrDefault();
        context.Items["RequestId"] = requestId;

        // 记录请求开始时间
        var startTime = DateTime.UtcNow;
        context.Items["StartTime"] = startTime;

        try
        {
            await _next(context);
        }
        finally
        {
            // 记录请求处理时间
            var endTime = DateTime.UtcNow;
            var duration = endTime - startTime;
            
            _logger.LogInformation("Request completed: {Method} {Path} - {StatusCode} - {Duration}ms - RequestId: {RequestId}",
                context.Request.Method, 
                context.Request.Path, 
                context.Response.StatusCode,
                duration.TotalMilliseconds,
                requestId);
        }
    }

    private bool IsJsonRequest(HttpContext context)
    {
        return context.Request.Method == "POST" || context.Request.Method == "PUT";
    }

    private bool HasValidContentType(HttpContext context)
    {
        var contentType = context.Request.ContentType;
        return !string.IsNullOrEmpty(contentType) && 
               contentType.StartsWith("application/json", StringComparison.OrdinalIgnoreCase);
    }

    private async Task WriteErrorResponse(HttpContext context, int statusCode, string message)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var errorResponse = new
        {
            error = "Validation Error",
            message = message,
            timestamp = DateTime.UtcNow,
            requestId = context.Items["RequestId"]?.ToString()
        };

        var json = JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }
}