using System.Text.Json;

namespace MathComicGenerator.Api.Middleware;

public class ResponseFormattingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ResponseFormattingMiddleware> _logger;

    public ResponseFormattingMiddleware(RequestDelegate next, ILogger<ResponseFormattingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // 保存原始响应流
        var originalBodyStream = context.Response.Body;

        try
        {
            // 创建新的内存流来捕获响应
            using var responseBody = new MemoryStream();
            context.Response.Body = responseBody;

            await _next(context);

            // 添加标准响应头
            AddStandardHeaders(context);

            // 格式化响应
            await FormatResponse(context, originalBodyStream);
        }
        finally
        {
            // 恢复原始响应流
            context.Response.Body = originalBodyStream;
        }
    }

    private void AddStandardHeaders(HttpContext context)
    {
        // 添加请求ID到响应头
        if (context.Items.TryGetValue("RequestId", out var requestId))
        {
            context.Response.Headers["X-Request-ID"] = requestId?.ToString();
        }

        // 添加处理时间
        if (context.Items.TryGetValue("StartTime", out var startTimeObj) && startTimeObj is DateTime startTime)
        {
            var duration = DateTime.UtcNow - startTime;
            context.Response.Headers["X-Processing-Time"] = $"{duration.TotalMilliseconds}ms";
        }

        // 添加API版本
        context.Response.Headers["X-API-Version"] = "1.0";

        // 添加时间戳
        context.Response.Headers["X-Timestamp"] = DateTime.UtcNow.ToString("O");

        // 安全头
        if (!context.Response.Headers.ContainsKey("X-Content-Type-Options"))
        {
            context.Response.Headers["X-Content-Type-Options"] = "nosniff";
        }

        if (!context.Response.Headers.ContainsKey("X-Frame-Options"))
        {
            context.Response.Headers["X-Frame-Options"] = "DENY";
        }

        if (!context.Response.Headers.ContainsKey("X-XSS-Protection"))
        {
            context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
        }
    }

    private async Task FormatResponse(HttpContext context, Stream originalBodyStream)
    {
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var responseText = await new StreamReader(context.Response.Body).ReadToEndAsync();
        context.Response.Body.Seek(0, SeekOrigin.Begin);

        // 如果响应为空，不进行格式化
        if (string.IsNullOrEmpty(responseText))
        {
            await context.Response.Body.CopyToAsync(originalBodyStream);
            return;
        }

        // 只格式化JSON响应
        if (context.Response.ContentType?.Contains("application/json") == true)
        {
            var formattedResponse = await FormatJsonResponse(context, responseText);
            
            // 更新Content-Length
            var formattedBytes = System.Text.Encoding.UTF8.GetBytes(formattedResponse);
            context.Response.ContentLength = formattedBytes.Length;
            
            await originalBodyStream.WriteAsync(formattedBytes);
        }
        else
        {
            // 非JSON响应直接复制
            await context.Response.Body.CopyToAsync(originalBodyStream);
        }
    }

    private async Task<string> FormatJsonResponse(HttpContext context, string responseText)
    {
        try
        {
            // 解析现有响应
            var responseObject = JsonSerializer.Deserialize<object>(responseText);
            
            // 创建标准响应格式
            var standardResponse = new
            {
                success = context.Response.StatusCode >= 200 && context.Response.StatusCode < 300,
                statusCode = context.Response.StatusCode,
                data = responseObject,
                timestamp = DateTime.UtcNow,
                requestId = context.Items["RequestId"]?.ToString(),
                processingTime = GetProcessingTime(context)
            };

            return JsonSerializer.Serialize(standardResponse, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            });
        }
        catch (JsonException)
        {
            // 如果无法解析JSON，返回原始响应
            _logger.LogWarning("Failed to parse response as JSON, returning original response");
            return responseText;
        }
    }

    private string GetProcessingTime(HttpContext context)
    {
        if (context.Items.TryGetValue("StartTime", out var startTimeObj) && startTimeObj is DateTime startTime)
        {
            var duration = DateTime.UtcNow - startTime;
            return $"{duration.TotalMilliseconds:F2}ms";
        }
        return "unknown";
    }
}