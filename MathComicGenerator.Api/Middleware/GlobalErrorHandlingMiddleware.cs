using MathComicGenerator.Api.Services;
using System.Net;
using System.Text.Json;

namespace MathComicGenerator.Api.Middleware;

public class GlobalErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalErrorHandlingMiddleware> _logger;

    public GlobalErrorHandlingMiddleware(RequestDelegate next, ILogger<GlobalErrorHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred");
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";
        
        var errorResponse = CreateErrorResponse(exception);
        context.Response.StatusCode = errorResponse.StatusCode;

        var jsonResponse = JsonSerializer.Serialize(errorResponse.Response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(jsonResponse);
    }

    private ErrorResponseInfo CreateErrorResponse(Exception exception)
    {
        return exception switch
        {
            ConfigurationException configEx => new ErrorResponseInfo
            {
                StatusCode = (int)HttpStatusCode.InternalServerError,
                Response = new ErrorResponse
                {
                    Error = "Configuration Error",
                    Message = "系统配置错误",
                    Details = GetUserFriendlyMessage(configEx.Message),
                    Timestamp = DateTime.UtcNow,
                    CanRetry = false,
                    ResolutionSteps = configEx.ResolutionSteps
                }
            },
            AuthenticationException authEx => new ErrorResponseInfo
            {
                StatusCode = (int)HttpStatusCode.Unauthorized,
                Response = new ErrorResponse
                {
                    Error = "Authentication Error",
                    Message = "认证失败",
                    Details = GetUserFriendlyMessage(authEx.Message),
                    Timestamp = DateTime.UtcNow,
                    CanRetry = false,
                    ResolutionSteps = authEx.ResolutionSteps
                }
            },
            NetworkException netEx => new ErrorResponseInfo
            {
                StatusCode = (int)HttpStatusCode.ServiceUnavailable,
                Response = new ErrorResponse
                {
                    Error = "Network Error",
                    Message = "网络连接错误",
                    Details = GetUserFriendlyMessage(netEx.Message),
                    Timestamp = DateTime.UtcNow,
                    CanRetry = true,
                    RetryAfter = TimeSpan.FromSeconds(30),
                    ResolutionSteps = netEx.ResolutionSteps
                }
            },
            DeepSeekAPIException deepSeekEx => new ErrorResponseInfo
            {
                StatusCode = (int)HttpStatusCode.ServiceUnavailable,
                Response = new ErrorResponse
                {
                    Error = "AI Service Error",
                    Message = "AI服务暂时不可用",
                    Details = GetUserFriendlyMessage(deepSeekEx.Message),
                    Timestamp = DateTime.UtcNow,
                    CanRetry = true,
                    RetryAfter = TimeSpan.FromMinutes(1),
                    ResolutionSteps = deepSeekEx.ResolutionSteps
                }
            },
            ArgumentNullException nullEx => new ErrorResponseInfo
            {
                StatusCode = (int)HttpStatusCode.BadRequest,
                Response = new ErrorResponse
                {
                    Error = "Missing Required Parameter",
                    Message = "缺少必需的参数",
                    Details = GetUserFriendlyMessage(nullEx.Message),
                    Timestamp = DateTime.UtcNow,
                    CanRetry = false
                }
            },
            ArgumentException argEx => new ErrorResponseInfo
            {
                StatusCode = (int)HttpStatusCode.BadRequest,
                Response = new ErrorResponse
                {
                    Error = "Invalid Request",
                    Message = GetUserFriendlyMessage(argEx.Message),
                    Details = "请检查输入参数是否正确",
                    Timestamp = DateTime.UtcNow,
                    CanRetry = false
                }
            },
            GeminiAPIException geminiEx => new ErrorResponseInfo
            {
                StatusCode = (int)HttpStatusCode.ServiceUnavailable,
                Response = new ErrorResponse
                {
                    Error = "AI Service Error",
                    Message = "AI服务暂时不可用，请稍后重试",
                    Details = GetUserFriendlyMessage(geminiEx.Message),
                    Timestamp = DateTime.UtcNow,
                    CanRetry = true,
                    RetryAfter = TimeSpan.FromMinutes(1)
                }
            },
            StorageException storageEx => new ErrorResponseInfo
            {
                StatusCode = (int)HttpStatusCode.InternalServerError,
                Response = new ErrorResponse
                {
                    Error = "Storage Error",
                    Message = "存储服务出现问题，请稍后重试",
                    Details = GetUserFriendlyMessage(storageEx.Message),
                    Timestamp = DateTime.UtcNow,
                    CanRetry = true,
                    RetryAfter = TimeSpan.FromSeconds(30)
                }
            },
            ResourceLimitException resourceEx => new ErrorResponseInfo
            {
                StatusCode = (int)HttpStatusCode.TooManyRequests,
                Response = new ErrorResponse
                {
                    Error = "Resource Limit Exceeded",
                    Message = "系统资源不足，请稍后重试",
                    Details = GetUserFriendlyMessage(resourceEx.Message),
                    Timestamp = DateTime.UtcNow,
                    CanRetry = true,
                    RetryAfter = TimeSpan.FromMinutes(5)
                }
            },
            TimeoutException timeoutEx => new ErrorResponseInfo
            {
                StatusCode = (int)HttpStatusCode.RequestTimeout,
                Response = new ErrorResponse
                {
                    Error = "Request Timeout",
                    Message = "请求超时，请稍后重试",
                    Details = "系统处理时间过长",
                    Timestamp = DateTime.UtcNow,
                    CanRetry = true,
                    RetryAfter = TimeSpan.FromSeconds(30)
                }
            },
            UnauthorizedAccessException unauthorizedEx => new ErrorResponseInfo
            {
                StatusCode = (int)HttpStatusCode.Unauthorized,
                Response = new ErrorResponse
                {
                    Error = "Unauthorized",
                    Message = "访问被拒绝",
                    Details = "请检查您的访问权限",
                    Timestamp = DateTime.UtcNow,
                    CanRetry = false
                }
            },
            _ => new ErrorResponseInfo
            {
                StatusCode = (int)HttpStatusCode.InternalServerError,
                Response = new ErrorResponse
                {
                    Error = "Internal Server Error",
                    Message = "系统内部错误，请稍后重试",
                    Details = "如果问题持续存在，请联系技术支持",
                    Timestamp = DateTime.UtcNow,
                    CanRetry = true,
                    RetryAfter = TimeSpan.FromMinutes(1)
                }
            }
        };
    }

    private string GetUserFriendlyMessage(string originalMessage)
    {
        // 将技术性错误消息转换为用户友好的消息
        if (string.IsNullOrEmpty(originalMessage))
            return "发生了未知错误";

        // 移除技术细节，保留用户可理解的部分
        var friendlyMessage = originalMessage;
        
        // 替换常见的技术术语
        var replacements = new Dictionary<string, string>
        {
            { "ArgumentException", "参数错误" },
            { "NullReferenceException", "数据错误" },
            { "HttpRequestException", "网络连接错误" },
            { "JsonException", "数据格式错误" },
            { "TimeoutException", "请求超时" },
            { "OutOfMemoryException", "内存不足" }
        };

        foreach (var replacement in replacements)
        {
            friendlyMessage = friendlyMessage.Replace(replacement.Key, replacement.Value);
        }

        // 限制消息长度
        if (friendlyMessage.Length > 200)
        {
            friendlyMessage = friendlyMessage.Substring(0, 200) + "...";
        }

        return friendlyMessage;
    }
}

public class ErrorResponseInfo
{
    public int StatusCode { get; set; }
    public ErrorResponse Response { get; set; } = new();
}

public class ErrorResponse
{
    public string Error { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public bool CanRetry { get; set; }
    public TimeSpan? RetryAfter { get; set; }
    public string? TraceId { get; set; }
    public string[] ResolutionSteps { get; set; } = Array.Empty<string>();
}