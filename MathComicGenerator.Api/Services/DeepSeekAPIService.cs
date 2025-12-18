using MathComicGenerator.Shared.Interfaces;
using MathComicGenerator.Shared.Models;
using Polly;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text;

namespace MathComicGenerator.Api.Services;

/// <summary>
/// DeepSeek API服务实现
/// </summary>
public class DeepSeekAPIService : IDeepSeekAPIService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<DeepSeekAPIService> _logger;
    private readonly DeepSeekAPIConfiguration _config;
    private readonly IAsyncPolicy<HttpResponseMessage> _retryPolicy;

    public DeepSeekAPIService(
        HttpClient httpClient, 
        ILogger<DeepSeekAPIService> logger,
        IConfiguration configuration)
    {
        _httpClient = httpClient;
        _logger = logger;
        _config = configuration.GetSection("DeepSeekAPI").Get<DeepSeekAPIConfiguration>() 
                  ?? new DeepSeekAPIConfiguration();

        // 配置重试策略
        // 仅在网络异常或非成功HTTP状态码时重试，不对超时（TaskCanceled）进行重试，避免累计长时间阻塞
        _retryPolicy = Policy
            .Handle<HttpRequestException>()
            .OrResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
            .WaitAndRetryAsync(
                retryCount: Math.Max(0, _config.MaxRetries),
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    _logger.LogWarning("DeepSeek API retry attempt {RetryCount} after {Delay}ms due to {Reason}", 
                        retryCount, timespan.TotalMilliseconds, outcome.Exception?.Message ?? (outcome.Result != null ? outcome.Result.StatusCode.ToString() : "Unknown"));
                });
                
        _logger.LogInformation("DeepSeek retry policy configured: MaxRetries={MaxRetries}, TimeoutSeconds={TimeoutSeconds}", _config.MaxRetries, _config.TimeoutSeconds);
 
        ConfigureHttpClient();
    }

    private void ConfigureHttpClient()
    {
        _httpClient.BaseAddress = new Uri(_config.BaseUrl);
        _httpClient.Timeout = TimeSpan.FromSeconds(_config.TimeoutSeconds);
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_config.ApiKey}");
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "MathComicGenerator/1.0");
    }

    public async Task<string> GeneratePromptAsync(string systemPrompt, string userPrompt)
    {
        // 检查API密钥是否配置
        if (string.IsNullOrEmpty(_config.ApiKey))
        {
            _logger.LogError("DeepSeek API key not configured");
            throw new ConfigurationException(
                "API key is not configured. Please configure the DeepSeek API key in appsettings.json",
                new[]
                {
                    "1. Open appsettings.json file",
                    "2. Add or update the DeepSeekAPI:ApiKey configuration",
                    "3. Obtain a valid API key from DeepSeek platform",
                    "4. Restart the application"
                });
        }

        try
        {
            _logger.LogInformation("Generating prompt using DeepSeek API");

            var request = CreateDeepSeekRequest(systemPrompt, userPrompt);
            var requestJson = JsonSerializer.Serialize(request, GetJsonOptions());
            
            // 调试日志：记录发送给DeepSeek的请求
            _logger.LogInformation("Sending request to DeepSeek API: {RequestJson}", requestJson);
            
            // 强制输出到控制台
            Console.WriteLine("=== DeepSeek API Request ===");
            Console.WriteLine($"URL: {_httpClient.BaseAddress}/chat/completions");
            Console.WriteLine($"Headers: Authorization: Bearer {_config.ApiKey.Substring(0, 10)}...");
            Console.WriteLine($"Request Body: {requestJson}");
            Console.WriteLine("============================");
            
            // 写入文件以便检查
            var debugFile = Path.Combine(Directory.GetCurrentDirectory(), "logs", "deepseek-debug.json");
            Directory.CreateDirectory(Path.GetDirectoryName(debugFile)!);
            await File.WriteAllTextAsync(debugFile, requestJson);
            
            // 每次重试都创建新的 HttpContent，避免重用导致的问题
            var requestStart = DateTime.UtcNow;
            var response = await _retryPolicy.ExecuteAsync(async () =>
            {
                using var attemptContent = new StringContent(requestJson, Encoding.UTF8, "application/json");
                return await _httpClient.PostAsync("/chat/completions", attemptContent);
            });
            var requestDuration = DateTime.UtcNow - requestStart;

            // 记录响应状态与耗时，便于排查长耗时请求
            _logger.LogInformation("DeepSeek API responded with {Status} in {Duration}ms", response.StatusCode, (long)requestDuration.TotalMilliseconds);
 
             // 写入响应调试文件，方便离线分析（避免记录完整API密钥）
             try
             {
                 var debugResponse = new
                 {
                     Timestamp = DateTime.UtcNow,
                     Url = _httpClient.BaseAddress + "/chat/completions",
                     RequestPreview = requestJson.Length > 200 ? requestJson.Substring(0, 200) + "..." : requestJson,
                     ResponseStatus = response.StatusCode.ToString(),
                     DurationMs = (long)requestDuration.TotalMilliseconds
                 };

                 var responseContentForFile = await response.Content.ReadAsStringAsync();
                 var debugObj = new {
                     debug = debugResponse,
                     ResponseBodyPreview = responseContentForFile.Length > 2000 ? responseContentForFile.Substring(0, 2000) + "..." : responseContentForFile
                 };
                 var debugFilePath = Path.Combine(Directory.GetCurrentDirectory(), "logs", $"deepseek-response-{DateTime.UtcNow:yyyyMMdd_HHmmss_fff}.json");
                 Directory.CreateDirectory(Path.GetDirectoryName(debugFilePath)!);
                 await File.WriteAllTextAsync(debugFilePath, JsonSerializer.Serialize(debugObj, GetJsonOptions()));
             }
             catch
             {
                 // 忽略调试写入错误，避免影响正常流程
             }
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("DeepSeek API error: {StatusCode} - {Content}", 
                    response.StatusCode, errorContent);
                _logger.LogError("Request that failed: {RequestJson}", requestJson);
                
                // 强制输出错误到控制台
                Console.WriteLine("=== DeepSeek API Error ===");
                Console.WriteLine($"Status Code: {response.StatusCode}");
                Console.WriteLine($"Error Content: {errorContent}");
                Console.WriteLine($"Failed Request: {requestJson}");
                Console.WriteLine("==========================");
                
                // 如果是认证错误，抛出异常
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    _logger.LogError("DeepSeek API authentication failed");
                    throw new AuthenticationException(
                        "API authentication failed. Please verify your API key",
                        new[]
                        {
                            "1. Verify your API key is correct in appsettings.json",
                            "2. Check if your API key has expired",
                            "3. Ensure your account has sufficient credits",
                            "4. Contact DeepSeek support if the issue persists"
                        });
                }
                
                throw new DeepSeekAPIException($"API request failed: {response.StatusCode}", 
                    response.StatusCode.ToString());
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var deepSeekResponse = JsonSerializer.Deserialize<DeepSeekResponse>(responseContent, GetJsonOptions());

            if (deepSeekResponse?.Choices?.Any() != true)
            {
                throw new DeepSeekAPIException("No content generated", "NO_CONTENT");
            }

            var generatedContent = deepSeekResponse.Choices.First().Message.Content;
            _logger.LogInformation("Successfully generated prompt with DeepSeek API");
            
            return generatedContent;
        }
        catch (DeepSeekAPIException)
        {
            // 重新抛出DeepSeek特定异常
            throw;
        }
        catch (TaskCanceledException ex)
        {
            var timeoutMessage = $"DeepSeek API请求超时 (配置超时时间: {_config.TimeoutSeconds}秒)";
            _logger.LogError(ex, "{TimeoutMessage}，异常详情: {ExceptionMessage}", timeoutMessage, ex.Message);
            throw new TimeoutException(
                $"{timeoutMessage}: {ex.Message}",
                ex);
        }
        catch (HttpRequestException ex)
        {
            var networkMessage = $"DeepSeek API网络错误，BaseUrl: {_config.BaseUrl}";
            _logger.LogError(ex, "{NetworkMessage}，异常详情: {ExceptionMessage}", networkMessage, ex.Message);
            throw new NetworkException(
                $"{networkMessage}: {ex.Message}",
                ex,
                new[]
                {
                    "1. Check your internet connection",
                    "2. Verify the API endpoint URL is correct",
                    "3. Check if DeepSeek service is available",
                    "4. Try again after a few minutes"
                });
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "解析DeepSeek API响应失败，异常详情: {ExceptionMessage}", ex.Message);
            throw new DeepSeekAPIException(
                $"响应解析错误: {ex.Message}", 
                "PARSE_ERROR",
                new[]
                {
                    "1. The API response format may have changed",
                    "2. Check if the API is returning valid JSON",
                    "3. Contact support if the issue persists"
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DeepSeek API意外错误，异常类型: {ExceptionType}，异常详情: {ExceptionMessage}", 
                ex.GetType().Name, ex.Message);
            throw new DeepSeekAPIException(
                $"意外错误 ({ex.GetType().Name}): {ex.Message}", 
                "UNEXPECTED_ERROR",
                new[]
                {
                    "1. Check the application logs for more details",
                    "2. Try the request again",
                    "3. Contact technical support if the issue persists"
                });
        }
    }

    public async Task<string> OptimizePromptAsync(string originalPrompt, string optimizationInstructions)
    {
        _logger.LogInformation("Optimizing prompt using DeepSeek API");

        var systemPrompt = "你是一个专业的提示词优化专家。请根据用户的要求优化提示词，使其更加清晰、具体和有效。";
        var userPrompt = $"请优化以下提示词：\n\n原始提示词：\n{originalPrompt}\n\n优化要求：\n{optimizationInstructions}\n\n请返回优化后的提示词：";

        return await GeneratePromptAsync(systemPrompt, userPrompt);
    }

    public async Task<ErrorResponse> HandleAPIErrorAsync(APIError error)
    {
        _logger.LogError("Handling DeepSeek API error: {ErrorCode} - {Message}", error.ErrorCode, error.Message);

        return error.ErrorCode switch
        {
            "TIMEOUT" => new ErrorResponse
            {
                UserMessage = "请求超时，请稍后重试",
                ShouldRetry = true,
                RetryAfter = TimeSpan.FromSeconds(30),
                ResolutionSteps = new[]
                {
                    "1. 等待30秒后重试",
                    "2. 检查网络连接是否稳定",
                    "3. 如果问题持续，请联系技术支持"
                }
            },
            "RATE_LIMIT" => new ErrorResponse
            {
                UserMessage = "请求过于频繁，请稍后重试",
                ShouldRetry = true,
                RetryAfter = TimeSpan.FromMinutes(1),
                ResolutionSteps = new[]
                {
                    "1. 等待1分钟后重试",
                    "2. 减少请求频率",
                    "3. 考虑升级API配额"
                }
            },
            "QUOTA_EXCEEDED" => new ErrorResponse
            {
                UserMessage = "API配额已用完，请联系管理员",
                ShouldRetry = false,
                ResolutionSteps = new[]
                {
                    "1. 检查API配额使用情况",
                    "2. 联系管理员增加配额",
                    "3. 等待配额重置时间"
                }
            },
            "INVALID_REQUEST" => new ErrorResponse
            {
                UserMessage = "请求格式错误，请检查输入内容",
                ShouldRetry = false,
                ResolutionSteps = new[]
                {
                    "1. 检查输入参数格式",
                    "2. 确保所有必需字段都已提供",
                    "3. 参考API文档验证请求格式"
                }
            },
            "NETWORK_ERROR" => new ErrorResponse
            {
                UserMessage = "网络连接错误，请检查网络连接",
                ShouldRetry = true,
                RetryAfter = TimeSpan.FromSeconds(10),
                ResolutionSteps = new[]
                {
                    "1. 检查网络连接",
                    "2. 验证API端点是否可访问",
                    "3. 检查防火墙设置",
                    "4. 10秒后重试"
                }
            },
            _ => new ErrorResponse
            {
                UserMessage = "系统暂时不可用，请稍后重试",
                ShouldRetry = true,
                RetryAfter = TimeSpan.FromSeconds(60),
                ResolutionSteps = new[]
                {
                    "1. 等待1分钟后重试",
                    "2. 检查系统状态页面",
                    "3. 如果问题持续，请联系技术支持"
                }
            }
        };
    }

    private DeepSeekRequest CreateDeepSeekRequest(string systemPrompt, string userPrompt)
    {
        return new DeepSeekRequest
        {
            Model = _config.Model,
            Messages = new[]
            {
                new DeepSeekMessage
                {
                    Role = "system",
                    Content = systemPrompt
                },
                new DeepSeekMessage
                {
                    Role = "user",
                    Content = userPrompt
                }
            },
            Temperature = _config.Temperature,
            MaxTokens = _config.MaxTokens,
            TopP = _config.TopP,
            FrequencyPenalty = _config.FrequencyPenalty,
            PresencePenalty = _config.PresencePenalty,
            Stream = false
        };
    }



    private JsonSerializerOptions GetJsonOptions()
    {
        return new JsonSerializerOptions
        {
            PropertyNamingPolicy = null, // 使用JsonPropertyName属性指定的名称
            PropertyNameCaseInsensitive = true,
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };
    }
}

// 配置类
public class DeepSeekAPIConfiguration
{
    public string BaseUrl { get; set; } = "https://api.deepseek.com/v1";
    public string ApiKey { get; set; } = "";
    public string Model { get; set; } = "deepseek-chat";
    public int TimeoutSeconds { get; set; } = 120;
    public int MaxRetries { get; set; } = 1;
    public float Temperature { get; set; } = 0.7f;
    public int MaxTokens { get; set; } = 2048;
    public float TopP { get; set; } = 0.95f;
    public float FrequencyPenalty { get; set; } = 0.0f;
    public float PresencePenalty { get; set; } = 0.0f;
}

// DeepSeek API 请求/响应模型
public class DeepSeekRequest
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = "";
    
    [JsonPropertyName("messages")]
    public DeepSeekMessage[] Messages { get; set; } = Array.Empty<DeepSeekMessage>();
    
    [JsonPropertyName("temperature")]
    public float Temperature { get; set; }
    
    [JsonPropertyName("max_tokens")]
    public int MaxTokens { get; set; }
    
    [JsonPropertyName("top_p")]
    public float TopP { get; set; }
    
    [JsonPropertyName("frequency_penalty")]
    public float FrequencyPenalty { get; set; }
    
    [JsonPropertyName("presence_penalty")]
    public float PresencePenalty { get; set; }
    
    [JsonPropertyName("stream")]
    public bool Stream { get; set; }
}

public class DeepSeekMessage
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = "";
    
    [JsonPropertyName("content")]
    public string Content { get; set; } = "";
}

public class DeepSeekResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";
    
    [JsonPropertyName("object")]
    public string Object { get; set; } = "";
    
    [JsonPropertyName("created")]
    public long Created { get; set; }
    
    [JsonPropertyName("model")]
    public string Model { get; set; } = "";
    
    [JsonPropertyName("choices")]
    public DeepSeekChoice[] Choices { get; set; } = Array.Empty<DeepSeekChoice>();
    
    [JsonPropertyName("usage")]
    public DeepSeekUsage Usage { get; set; } = new();
}

public class DeepSeekChoice
{
    [JsonPropertyName("index")]
    public int Index { get; set; }
    
    [JsonPropertyName("message")]
    public DeepSeekMessage Message { get; set; } = new();
    
    [JsonPropertyName("finish_reason")]
    public string FinishReason { get; set; } = "";
}

public class DeepSeekUsage
{
    [JsonPropertyName("prompt_tokens")]
    public int PromptTokens { get; set; }
    
    [JsonPropertyName("completion_tokens")]
    public int CompletionTokens { get; set; }
    
    [JsonPropertyName("total_tokens")]
    public int TotalTokens { get; set; }
}

// 异常类
public class DeepSeekAPIException : Exception
{
    public string ErrorCode { get; }
    public string[] ResolutionSteps { get; }

    public DeepSeekAPIException(string message, string errorCode, string[]? resolutionSteps = null) : base(message)
    {
        ErrorCode = errorCode;
        ResolutionSteps = resolutionSteps ?? Array.Empty<string>();
    }

    public DeepSeekAPIException(string message, string errorCode, Exception innerException, string[]? resolutionSteps = null) 
        : base(message, innerException)
    {
        ErrorCode = errorCode;
        ResolutionSteps = resolutionSteps ?? Array.Empty<string>();
    }
}

public class ConfigurationException : Exception
{
    public string[] ResolutionSteps { get; }

    public ConfigurationException(string message, string[]? resolutionSteps = null) : base(message)
    {
        ResolutionSteps = resolutionSteps ?? Array.Empty<string>();
    }

    public ConfigurationException(string message, Exception innerException, string[]? resolutionSteps = null) 
        : base(message, innerException)
    {
        ResolutionSteps = resolutionSteps ?? Array.Empty<string>();
    }
}

public class AuthenticationException : Exception
{
    public string[] ResolutionSteps { get; }

    public AuthenticationException(string message, string[]? resolutionSteps = null) : base(message)
    {
        ResolutionSteps = resolutionSteps ?? Array.Empty<string>();
    }

    public AuthenticationException(string message, Exception innerException, string[]? resolutionSteps = null) 
        : base(message, innerException)
    {
        ResolutionSteps = resolutionSteps ?? Array.Empty<string>();
    }
}

public class NetworkException : Exception
{
    public string[] ResolutionSteps { get; }

    public NetworkException(string message, string[]? resolutionSteps = null) : base(message)
    {
        ResolutionSteps = resolutionSteps ?? Array.Empty<string>();
    }

    public NetworkException(string message, Exception innerException, string[]? resolutionSteps = null) 
        : base(message, innerException)
    {
        ResolutionSteps = resolutionSteps ?? Array.Empty<string>();
    }
}