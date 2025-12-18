using MathComicGenerator.Shared.Interfaces;
using MathComicGenerator.Shared.Models;
using Polly;
using System.Text.Json;
using System.Text;

namespace MathComicGenerator.Api.Services;

public class GeminiAPIService : IGeminiAPIService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GeminiAPIService> _logger;
    private readonly GeminiAPIConfiguration _config;
    private readonly IAsyncPolicy<HttpResponseMessage> _retryPolicy;

    public GeminiAPIService(
        HttpClient httpClient, 
        ILogger<GeminiAPIService> logger,
        IConfiguration configuration)
    {
        _httpClient = httpClient;
        _logger = logger;
        _config = configuration.GetSection("GeminiAPI").Get<GeminiAPIConfiguration>() 
                  ?? new GeminiAPIConfiguration();

        // 配置重试策略
        _retryPolicy = Policy
            .Handle<HttpRequestException>()
            .Or<TaskCanceledException>()
            .OrResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
            .WaitAndRetryAsync(
                retryCount: _config.MaxRetries,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    _logger.LogWarning("Gemini API retry attempt {RetryCount} after {Delay}ms", 
                        retryCount, timespan.TotalMilliseconds);
                });

        ConfigureHttpClient();
    }

    private void ConfigureHttpClient()
    {
        _httpClient.BaseAddress = new Uri(_config.BaseUrl);
        _httpClient.Timeout = TimeSpan.FromSeconds(_config.TimeoutSeconds);
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_config.ApiKey}");
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "MathComicGenerator/1.0");
    }

    public async Task<ComicContent> GenerateComicContentAsync(string prompt)
    {
        try
        {
            _logger.LogInformation("Generating comic content for prompt: {Prompt}", prompt);

            var request = CreateOpenAICompatibleRequest(prompt);
            var requestJson = JsonSerializer.Serialize(request, GetJsonOptions());
            var content = new StringContent(requestJson, Encoding.UTF8, "application/json");

            var response = await _retryPolicy.ExecuteAsync(async () =>
            {
                return await _httpClient.PostAsync("/chat/completions", content);
            });

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Gemini API error: {StatusCode} - {Content}", 
                    response.StatusCode, errorContent);
                
                throw new GeminiAPIException($"API request failed: {response.StatusCode}", 
                    response.StatusCode.ToString());
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var openAIResponse = JsonSerializer.Deserialize<OpenAIResponse>(responseContent, GetJsonOptions());

            if (openAIResponse?.Choices?.Any() != true)
            {
                throw new GeminiAPIException("No content generated", "NO_CONTENT");
            }

            return ParseComicContent(openAIResponse.Choices.First().Message.Content);
        }
        catch (TaskCanceledException ex)
        {
            var timeoutMessage = $"Gemini API请求超时 (配置超时时间: {_config.TimeoutSeconds}秒)";
            _logger.LogError(ex, "{TimeoutMessage}，异常详情: {ExceptionMessage}", timeoutMessage, ex.Message);
            throw new GeminiAPIException($"{timeoutMessage}: {ex.Message}", "TIMEOUT");
        }
        catch (HttpRequestException ex)
        {
            var networkMessage = $"Gemini API网络错误，BaseUrl: {_config.BaseUrl}";
            _logger.LogError(ex, "{NetworkMessage}，异常详情: {ExceptionMessage}", networkMessage, ex.Message);
            throw new GeminiAPIException($"{networkMessage}: {ex.Message}", "NETWORK_ERROR");
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "解析Gemini API响应失败，异常详情: {ExceptionMessage}", ex.Message);
            throw new GeminiAPIException($"响应解析错误: {ex.Message}", "PARSE_ERROR");
        }
    }

    public async Task<ErrorResponse> HandleAPIErrorAsync(APIError error)
    {
        _logger.LogError("Handling API error: {ErrorCode} - {Message}", error.ErrorCode, error.Message);

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

    private OpenAIRequest CreateOpenAICompatibleRequest(string prompt)
    {
        var enhancedPrompt = $@"
请为以下数学概念创建一个适合儿童的4格漫画故事：

数学概念：{prompt}

要求：
1. 创建4个连续的漫画面板
2. 每个面板包含场景描述和对话
3. 内容适合儿童，积极正面
4. 用简单易懂的语言解释数学概念
5. 包含有趣的角色和情节

请按以下JSON格式返回：
{{
  ""title"": ""漫画标题"",
  ""panels"": [
    {{
      ""imageDescription"": ""面板1的场景描述"",
      ""dialogue"": [""角色1对话"", ""角色2对话""],
      ""narration"": ""旁白（可选）""
    }},
    {{
      ""imageDescription"": ""面板2的场景描述"",
      ""dialogue"": [""角色对话""],
      ""narration"": ""旁白""
    }},
    {{
      ""imageDescription"": ""面板3的场景描述"",
      ""dialogue"": [""角色对话""],
      ""narration"": ""旁白""
    }},
    {{
      ""imageDescription"": ""面板4的场景描述"",
      ""dialogue"": [""角色对话""],
      ""narration"": ""旁白""
    }}
  ]
}}";

        return new OpenAIRequest
        {
            Model = "gemini-pro",
            Messages = new[]
            {
                new OpenAIMessage
                {
                    Role = "user",
                    Content = enhancedPrompt
                }
            },
            Temperature = 0.7f,
            MaxTokens = 2048,
            TopP = 0.95f
        };
    }

    private ComicContent ParseComicContent(string responseText)
    {
        try
        {
            // 尝试从响应中提取JSON
            var jsonStart = responseText.IndexOf('{');
            var jsonEnd = responseText.LastIndexOf('}');
            
            if (jsonStart == -1 || jsonEnd == -1 || jsonEnd <= jsonStart)
            {
                throw new JsonException("No valid JSON found in response");
            }

            var jsonContent = responseText.Substring(jsonStart, jsonEnd - jsonStart + 1);
            var comicData = JsonSerializer.Deserialize<ComicContentResponse>(jsonContent, GetJsonOptions());

            if (comicData?.Panels?.Any() != true)
            {
                throw new JsonException("No panels found in response");
            }

            return new ComicContent
            {
                Title = comicData.Title ?? "数学漫画",
                Panels = comicData.Panels.Select(p => new PanelContent
                {
                    ImageDescription = p.ImageDescription ?? "",
                    Dialogue = p.Dialogue ?? new List<string>(),
                    Narration = p.Narration
                }).ToList()
            };
        }
        catch (JsonException)
        {
            // 如果JSON解析失败，创建一个基本的漫画内容
            _logger.LogWarning("Failed to parse structured response, creating basic content");
            return CreateFallbackContent(responseText);
        }
    }

    private ComicContent CreateFallbackContent(string responseText)
    {
        // 创建一个简单的单面板漫画作为后备
        return new ComicContent
        {
            Title = "数学概念解释",
            Panels = new List<PanelContent>
            {
                new PanelContent
                {
                    ImageDescription = "一个友好的老师在黑板前解释数学概念",
                    Dialogue = new List<string> { "让我们一起学习数学吧！" },
                    Narration = responseText.Length > 200 ? responseText.Substring(0, 200) + "..." : responseText
                }
            }
        };
    }

    private JsonSerializerOptions GetJsonOptions()
    {
        return new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };
    }
}

// 配置类
public class GeminiAPIConfiguration
{
    public string BaseUrl { get; set; } = "https://generativelanguage.googleapis.com";
    public string ApiKey { get; set; } = "";
    public int TimeoutSeconds { get; set; } = 30;
    public int MaxRetries { get; set; } = 3;
}

// OpenAI Compatible API Models
public class OpenAIRequest
{
    public string Model { get; set; } = "";
    public OpenAIMessage[] Messages { get; set; } = Array.Empty<OpenAIMessage>();
    public float Temperature { get; set; }
    public int MaxTokens { get; set; }
    public float TopP { get; set; }
}

public class OpenAIMessage
{
    public string Role { get; set; } = "";
    public string Content { get; set; } = "";
}

public class OpenAIResponse
{
    public OpenAIChoice[]? Choices { get; set; }
}

public class OpenAIChoice
{
    public OpenAIMessage Message { get; set; } = new();
}

// Gemini API 请求/响应模型 (保留用于兼容性)
public class GeminiRequest
{
    public GeminiContent[] Contents { get; set; } = Array.Empty<GeminiContent>();
    public GeminiGenerationConfig? GenerationConfig { get; set; }
    public GeminiSafetySetting[]? SafetySettings { get; set; }
}

public class GeminiContent
{
    public GeminiPart[] Parts { get; set; } = Array.Empty<GeminiPart>();
}

public class GeminiPart
{
    public string Text { get; set; } = "";
}

public class GeminiGenerationConfig
{
    public float Temperature { get; set; }
    public int TopK { get; set; }
    public float TopP { get; set; }
    public int MaxOutputTokens { get; set; }
}

public class GeminiSafetySetting
{
    public string Category { get; set; } = "";
    public string Threshold { get; set; } = "";
}

public class GeminiResponse
{
    public GeminiCandidate[]? Candidates { get; set; }
}

public class GeminiCandidate
{
    public GeminiContent Content { get; set; } = new();
}

public class ComicContentResponse
{
    public string? Title { get; set; }
    public List<PanelContentResponse>? Panels { get; set; }
}

public class PanelContentResponse
{
    public string? ImageDescription { get; set; }
    public List<string>? Dialogue { get; set; }
    public string? Narration { get; set; }
}

// 异常类
public class GeminiAPIException : Exception
{
    public string ErrorCode { get; }

    public GeminiAPIException(string message, string errorCode) : base(message)
    {
        ErrorCode = errorCode;
    }

    public GeminiAPIException(string message, string errorCode, Exception innerException) 
        : base(message, innerException)
    {
        ErrorCode = errorCode;
    }
}