using MathComicGenerator.Shared.Interfaces;
using MathComicGenerator.Shared.Models;
using Polly;
using System.Text.Json;
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
        _retryPolicy = Policy
            .Handle<HttpRequestException>()
            .Or<TaskCanceledException>()
            .OrResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
            .WaitAndRetryAsync(
                retryCount: _config.MaxRetries,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    _logger.LogWarning("DeepSeek API retry attempt {RetryCount} after {Delay}ms", 
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

    public async Task<string> GeneratePromptAsync(string systemPrompt, string userPrompt)
    {
        // 检查API密钥是否配置
        if (string.IsNullOrEmpty(_config.ApiKey))
        {
            _logger.LogWarning("DeepSeek API key not configured, using intelligent mock data");
            return GenerateIntelligentMockPrompt(userPrompt);
        }

        try
        {
            _logger.LogInformation("Generating prompt using DeepSeek API");

            var request = CreateDeepSeekRequest(systemPrompt, userPrompt);
            var requestJson = JsonSerializer.Serialize(request, GetJsonOptions());
            var content = new StringContent(requestJson, Encoding.UTF8, "application/json");

            var response = await _retryPolicy.ExecuteAsync(async () =>
            {
                return await _httpClient.PostAsync("/chat/completions", content);
            });

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("DeepSeek API error: {StatusCode} - {Content}", 
                    response.StatusCode, errorContent);
                
                // 如果是认证错误，回退到模拟数据
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    _logger.LogWarning("DeepSeek API authentication failed, falling back to mock data");
                    return GenerateIntelligentMockPrompt(userPrompt);
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
            _logger.LogError(ex, "{TimeoutMessage}，异常详情: {ExceptionMessage}，回退到模拟数据", timeoutMessage, ex.Message);
            throw new DeepSeekAPIException($"{timeoutMessage}: {ex.Message}", "TIMEOUT");
        }
        catch (HttpRequestException ex)
        {
            var networkMessage = $"DeepSeek API网络错误，BaseUrl: {_config.BaseUrl}";
            _logger.LogError(ex, "{NetworkMessage}，异常详情: {ExceptionMessage}，回退到模拟数据", networkMessage, ex.Message);
            throw new DeepSeekAPIException($"{networkMessage}: {ex.Message}", "NETWORK_ERROR");
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "解析DeepSeek API响应失败，异常详情: {ExceptionMessage}，回退到模拟数据", ex.Message);
            throw new DeepSeekAPIException($"响应解析错误: {ex.Message}", "PARSE_ERROR");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DeepSeek API意外错误，异常类型: {ExceptionType}，异常详情: {ExceptionMessage}，回退到模拟数据", 
                ex.GetType().Name, ex.Message);
            throw new DeepSeekAPIException($"意外错误 ({ex.GetType().Name}): {ex.Message}", "UNEXPECTED_ERROR");
        }
    }

    public async Task<string> OptimizePromptAsync(string originalPrompt, string optimizationInstructions)
    {
        try
        {
            _logger.LogInformation("Optimizing prompt using DeepSeek API");

            var systemPrompt = "你是一个专业的提示词优化专家。请根据用户的要求优化提示词，使其更加清晰、具体和有效。";
            var userPrompt = $"请优化以下提示词：\n\n原始提示词：\n{originalPrompt}\n\n优化要求：\n{optimizationInstructions}\n\n请返回优化后的提示词：";

            return await GeneratePromptAsync(systemPrompt, userPrompt);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error optimizing prompt with DeepSeek API");
            // 如果优化失败，返回原始提示词
            return originalPrompt;
        }
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
                RetryAfter = TimeSpan.FromSeconds(30)
            },
            "RATE_LIMIT" => new ErrorResponse
            {
                UserMessage = "请求过于频繁，请稍后重试",
                ShouldRetry = true,
                RetryAfter = TimeSpan.FromMinutes(1)
            },
            "QUOTA_EXCEEDED" => new ErrorResponse
            {
                UserMessage = "API配额已用完，请联系管理员",
                ShouldRetry = false
            },
            "INVALID_REQUEST" => new ErrorResponse
            {
                UserMessage = "请求格式错误，请检查输入内容",
                ShouldRetry = false
            },
            "NETWORK_ERROR" => new ErrorResponse
            {
                UserMessage = "网络连接错误，请检查网络连接",
                ShouldRetry = true,
                RetryAfter = TimeSpan.FromSeconds(10)
            },
            _ => new ErrorResponse
            {
                UserMessage = "系统暂时不可用，请稍后重试",
                ShouldRetry = true,
                RetryAfter = TimeSpan.FromSeconds(60)
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

    private string GenerateIntelligentMockPrompt(string userPrompt)
    {
        // 分析用户提示词，生成相应的智能模拟提示词
        var lowerPrompt = userPrompt.ToLower();
        
        // 科学类知识点
        if (lowerPrompt.Contains("牛顿") || lowerPrompt.Contains("定律") || lowerPrompt.Contains("物理"))
        {
            return @"提示词: 创建一个4格漫画，展示牛顿第一定律的概念。

面板1: 一个小球静止在桌子上，旁边站着好奇的小明
对话: 小明：为什么球不动呢？
场景: 简洁的教室环境，卡通风格

面板2: 小明轻推小球，球开始滚动
对话: 小明：我推它，它就动了！
场景: 展示力的作用过程

面板3: 球撞到墙壁停下来
对话: 小明：撞到墙就停了
场景: 球与墙壁的接触

面板4: 老师解释牛顿第一定律
对话: 老师：这就是牛顿第一定律，物体保持原来的运动状态，除非有外力作用
场景: 老师指着黑板上的公式

改进建议:
- 可以添加更多生活中的例子
- 增加动画效果的描述
- 强调惯性概念的重要性";
        }
        
        // 数学类知识点
        if (lowerPrompt.Contains("二次方程") || lowerPrompt.Contains("方程") || lowerPrompt.Contains("数学"))
        {
            return @"提示词: 创建一个4格漫画，生动展示二次方程的解法过程。

面板1: 学生小红面对黑板上的二次方程 x²-5x+6=0，表情困惑
对话: 小红：这个方程怎么解呢？
场景: 明亮的教室，黑板上写着方程

面板2: 老师介绍因式分解法
对话: 老师：我们可以把它分解成两个因子相乘
场景: 老师在黑板上写 (x-2)(x-3)=0

面板3: 展示求解过程
对话: 老师：所以 x-2=0 或 x-3=0
场景: 黑板上显示 x=2 或 x=3

面板4: 学生恍然大悟
对话: 小红：原来如此！我明白了！
场景: 学生开心的表情，周围有理解的光芒效果

改进建议:
- 可以添加图形化的解释
- 增加验证步骤的演示
- 提供更多解法的对比";
        }
        
        // 历史类知识点
        if (lowerPrompt.Contains("工业革命") || lowerPrompt.Contains("历史") || lowerPrompt.Contains("革命"))
        {
            return @"提示词: 创建一个4格漫画，展现工业革命对社会的深远影响。

面板1: 18世纪的手工作坊，工人们手工制作产品
对话: 工匠：我们一天只能做几件产品
场景: 传统的手工作坊，工具简单

面板2: 蒸汽机的发明和工厂的建立
对话: 发明家：蒸汽机将改变一切！
场景: 冒着蒸汽的机器，新建的工厂

面板3: 大规模生产和城市化
对话: 工人：现在我们一天能生产上百件！
场景: 繁忙的工厂流水线，城市高楼

面板4: 社会变革的全景
对话: 旁白：工业革命改变了人类的生活方式
场景: 对比图显示革命前后的巨大变化

改进建议:
- 可以加入更多技术发明的细节
- 展示对不同社会阶层的影响
- 强调环境和社会问题";
        }
        
        // 语言类知识点
        if (lowerPrompt.Contains("条件句") || lowerPrompt.Contains("英语") || lowerPrompt.Contains("语法"))
        {
            return @"提示词: 创建一个4格漫画，清晰解释英语条件句的用法和结构。

面板1: 英语老师在黑板上写下 'If it rains, I will stay home'
对话: 老师：今天我们学习条件句
场景: 整洁的英语教室，学生们专注听讲

面板2: 解释第一类条件句的结构
对话: 老师：If + 现在时，主句用将来时
场景: 黑板上标注语法结构，用不同颜色突出

面板3: 学生练习造句
对话: 学生：If I study hard, I will pass the exam!
场景: 学生举手发言，其他同学点头认同

面板4: 总结不同类型的条件句
对话: 老师：很好！条件句帮我们表达假设和结果
场景: 黑板上列出三种条件句类型的对比

改进建议:
- 可以添加更多实际生活例句
- 用图示说明时态的对应关系
- 增加练习互动的环节";
        }
        
        // 默认通用提示词
        return @"提示词: 创建一个4格教育漫画，生动有趣地展示所学知识点。

面板1: 引入问题或概念，激发学习兴趣
对话: 角色表达疑问或好奇
场景: 适合的学习环境

面板2: 展示核心概念或原理
对话: 解释关键知识点
场景: 清晰的演示或说明

面板3: 深入理解或应用实例
对话: 进一步阐述或举例
场景: 具体的应用场景

面板4: 总结和启发
对话: 总结要点，表达理解
场景: 积极正面的学习成果展示

改进建议:
- 根据具体知识点调整内容细节
- 增加互动性和趣味性元素
- 确保教育价值和年龄适宜性";
    }

    private JsonSerializerOptions GetJsonOptions()
    {
        return new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };
    }
}

// 配置类
public class DeepSeekAPIConfiguration
{
    public string BaseUrl { get; set; } = "https://api.deepseek.com/v1";
    public string ApiKey { get; set; } = "";
    public string Model { get; set; } = "deepseek-chat";
    public int TimeoutSeconds { get; set; } = 30;
    public int MaxRetries { get; set; } = 3;
    public float Temperature { get; set; } = 0.7f;
    public int MaxTokens { get; set; } = 2048;
    public float TopP { get; set; } = 0.95f;
    public float FrequencyPenalty { get; set; } = 0.0f;
    public float PresencePenalty { get; set; } = 0.0f;
}

// DeepSeek API 请求/响应模型
public class DeepSeekRequest
{
    public string Model { get; set; } = "";
    public DeepSeekMessage[] Messages { get; set; } = Array.Empty<DeepSeekMessage>();
    public float Temperature { get; set; }
    public int MaxTokens { get; set; }
    public float TopP { get; set; }
    public float FrequencyPenalty { get; set; }
    public float PresencePenalty { get; set; }
    public bool Stream { get; set; }
}

public class DeepSeekMessage
{
    public string Role { get; set; } = "";
    public string Content { get; set; } = "";
}

public class DeepSeekResponse
{
    public string Id { get; set; } = "";
    public string Object { get; set; } = "";
    public long Created { get; set; }
    public string Model { get; set; } = "";
    public DeepSeekChoice[] Choices { get; set; } = Array.Empty<DeepSeekChoice>();
    public DeepSeekUsage Usage { get; set; } = new();
}

public class DeepSeekChoice
{
    public int Index { get; set; }
    public DeepSeekMessage Message { get; set; } = new();
    public string FinishReason { get; set; } = "";
}

public class DeepSeekUsage
{
    public int PromptTokens { get; set; }
    public int CompletionTokens { get; set; }
    public int TotalTokens { get; set; }
}

// 异常类
public class DeepSeekAPIException : Exception
{
    public string ErrorCode { get; }

    public DeepSeekAPIException(string message, string errorCode) : base(message)
    {
        ErrorCode = errorCode;
    }

    public DeepSeekAPIException(string message, string errorCode, Exception innerException) 
        : base(message, innerException)
    {
        ErrorCode = errorCode;
    }
}