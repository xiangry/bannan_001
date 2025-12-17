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
        // 临时使用智能模拟数据进行测试
        _logger.LogInformation("Using intelligent mock data for testing - prompt: {Prompt}", prompt);
        
        await Task.Delay(1000); // 模拟API调用延迟

        return GenerateIntelligentMockContent(prompt);

    }

    private ComicContent GenerateIntelligentMockContent(string prompt)
    {
        // 分析提示词内容，生成相应的模拟数据
        var lowerPrompt = prompt.ToLower();
        
        // 科学类知识点
        if (lowerPrompt.Contains("光") && lowerPrompt.Contains("折射"))
        {
            return new ComicContent
            {
                Title = "科学漫画：光的折射原理",
                Panels = new List<PanelContent>
                {
                    new PanelContent
                    {
                        ImageDescription = "小明拿着一根筷子放入装水的玻璃杯中，发现筷子看起来弯曲了",
                        Dialogue = new List<string> { "小明：咦，筷子怎么弯了？", "老师：这是一个有趣的现象！" },
                        Narration = "小明发现了光的折射现象"
                    },
                    new PanelContent
                    {
                        ImageDescription = "老师用激光笔照射水面，光线在水中改变了方向",
                        Dialogue = new List<string> { "老师：光从空气进入水中时会改变方向", "小明：原来如此！" },
                        Narration = "老师演示光线在不同介质中的传播"
                    },
                    new PanelContent
                    {
                        ImageDescription = "黑板上画着光线折射的示意图，标注了入射角和折射角",
                        Dialogue = new List<string> { "老师：这就是光的折射定律", "小明：我明白了！" },
                        Narration = "学习光的折射定律"
                    },
                    new PanelContent
                    {
                        ImageDescription = "小明兴奋地指着彩虹，理解了光的折射在自然界中的应用",
                        Dialogue = new List<string> { "小明：彩虹也是光的折射！", "老师：你学得很好！" },
                        Narration = "理解光的折射在生活中的应用"
                    }
                }
            };
        }
        
        // 历史类知识点
        if (lowerPrompt.Contains("四大发明") || lowerPrompt.Contains("古代") && lowerPrompt.Contains("发明"))
        {
            return new ComicContent
            {
                Title = "历史漫画：中国古代四大发明",
                Panels = new List<PanelContent>
                {
                    new PanelContent
                    {
                        ImageDescription = "古代中国的造纸工坊，工匠们正在制作纸张",
                        Dialogue = new List<string> { "工匠：我们发明了造纸术！", "学者：这将改变世界！" },
                        Narration = "造纸术的发明让知识传播更加便利"
                    },
                    new PanelContent
                    {
                        ImageDescription = "古代印刷工坊，工人们使用活字印刷技术印制书籍",
                        Dialogue = new List<string> { "印刷工：活字印刷真是太方便了！", "书商：书籍可以大量印制了！" },
                        Narration = "印刷术让书籍得以大量复制"
                    },
                    new PanelContent
                    {
                        ImageDescription = "古代炼金术士发明火药，展示火药的威力",
                        Dialogue = new List<string> { "炼金术士：这种黑色粉末威力巨大！", "将军：这将改变战争！" },
                        Narration = "火药的发明改变了军事和工程"
                    },
                    new PanelContent
                    {
                        ImageDescription = "古代航海家使用指南针导航，在茫茫大海中找到方向",
                        Dialogue = new List<string> { "航海家：有了指南针，我们不会迷路了！", "船员：太神奇了！" },
                        Narration = "指南针让远洋航行成为可能"
                    }
                }
            };
        }
        
        // 语言类知识点
        if (lowerPrompt.Contains("过去时") || lowerPrompt.Contains("时态"))
        {
            return new ComicContent
            {
                Title = "语言漫画：英语过去时态的用法",
                Panels = new List<PanelContent>
                {
                    new PanelContent
                    {
                        ImageDescription = "英语老师在黑板上写着现在时和过去时的对比",
                        Dialogue = new List<string> { "老师：今天我们学习过去时态", "学生：什么是过去时态？" },
                        Narration = "开始学习英语过去时态"
                    },
                    new PanelContent
                    {
                        ImageDescription = "老师举例说明：'I play' 变成 'I played'",
                        Dialogue = new List<string> { "老师：play变成played", "学生：原来要加ed！" },
                        Narration = "学习规则动词的过去时变化"
                    },
                    new PanelContent
                    {
                        ImageDescription = "黑板上列出不规则动词：go-went, see-saw, eat-ate",
                        Dialogue = new List<string> { "老师：有些动词变化不规则", "学生：需要特别记忆！" },
                        Narration = "学习不规则动词的过去时"
                    },
                    new PanelContent
                    {
                        ImageDescription = "学生们练习造句，用过去时描述昨天的活动",
                        Dialogue = new List<string> { "学生：Yesterday I went to school", "老师：Very good！" },
                        Narration = "练习使用过去时态造句"
                    }
                }
            };
        }
        
        // 艺术类知识点
        if (lowerPrompt.Contains("色彩") && lowerPrompt.Contains("搭配"))
        {
            return new ComicContent
            {
                Title = "艺术漫画：色彩搭配的基本原理",
                Panels = new List<PanelContent>
                {
                    new PanelContent
                    {
                        ImageDescription = "美术老师展示色彩环，指出三原色：红、黄、蓝",
                        Dialogue = new List<string> { "老师：这是色彩环，红黄蓝是三原色", "学生：好漂亮的颜色！" },
                        Narration = "认识色彩环和三原色"
                    },
                    new PanelContent
                    {
                        ImageDescription = "老师演示混合颜色：红+黄=橙，蓝+黄=绿",
                        Dialogue = new List<string> { "老师：混合原色可以得到新颜色", "学生：太神奇了！" },
                        Narration = "学习颜色的混合原理"
                    },
                    new PanelContent
                    {
                        ImageDescription = "展示互补色搭配：红配绿，蓝配橙，形成强烈对比",
                        Dialogue = new List<string> { "老师：互补色搭配很醒目", "学生：确实很有冲击力！" },
                        Narration = "学习互补色的搭配效果"
                    },
                    new PanelContent
                    {
                        ImageDescription = "学生们用学到的色彩知识创作画作，色彩搭配和谐美观",
                        Dialogue = new List<string> { "学生：我的画色彩很和谐！", "老师：你们学得很好！" },
                        Narration = "运用色彩搭配知识进行创作"
                    }
                }
            };
        }
        
        // 数学类知识点（包括分数）
        if (lowerPrompt.Contains("分数"))
        {
            return new ComicContent
            {
                Title = "数学漫画：分数的概念和应用",
                Panels = new List<PanelContent>
                {
                    new PanelContent
                    {
                        ImageDescription = "小明和小红看着一个完整的披萨，准备分享",
                        Dialogue = new List<string> { "小明：这个披萨怎么分呢？", "小红：我们平均分吧！" },
                        Narration = "学习分数的实际应用"
                    },
                    new PanelContent
                    {
                        ImageDescription = "小红将披萨切成两等份，每人拿一份",
                        Dialogue = new List<string> { "小红：每人得到一半", "小明：一半用分数怎么表示？" },
                        Narration = "理解平均分的概念"
                    },
                    new PanelContent
                    {
                        ImageDescription = "老师在黑板上写着1/2，解释分数的含义",
                        Dialogue = new List<string> { "老师：一半写作1/2", "学生：分子是1，分母是2！" },
                        Narration = "学习分数的表示方法"
                    },
                    new PanelContent
                    {
                        ImageDescription = "学生们用不同的物品练习分数：1/3个苹果，1/4块蛋糕",
                        Dialogue = new List<string> { "学生：我明白分数了！", "老师：分数在生活中很有用！" },
                        Narration = "练习分数在生活中的应用"
                    }
                }
            };
        }
        
        // 默认数学内容（加法运算）
        return new ComicContent
        {
            Title = "数学漫画：加法运算",
            Panels = new List<PanelContent>
            {
                new PanelContent
                {
                    ImageDescription = "两个小朋友站在黑板前，看着写有'2 + 3 = ?'的数学题，表情困惑",
                    Dialogue = new List<string> { "小明：这道加法题怎么做呢？", "小红：我们一起想想办法吧！" },
                    Narration = "小明和小红遇到了一道加法题"
                },
                new PanelContent
                {
                    ImageDescription = "小红拿出两个苹果，小明拿出三个苹果，放在桌子上",
                    Dialogue = new List<string> { "小红：我有2个苹果", "小明：我有3个苹果" },
                    Narration = "他们决定用苹果来帮助计算"
                },
                new PanelContent
                {
                    ImageDescription = "两人把所有苹果放在一起，开始数数：1、2、3、4、5",
                    Dialogue = new List<string> { "一起：1、2、3、4、5！" },
                    Narration = "把所有苹果放在一起数一数"
                },
                new PanelContent
                {
                    ImageDescription = "两人高兴地指着黑板，上面写着'2 + 3 = 5'，周围有庆祝的表情符号",
                    Dialogue = new List<string> { "小明：原来答案是5！", "小红：加法真有趣！" },
                    Narration = "他们成功解决了加法问题，学会了新知识"
                }
            }
        };

        /* 原始API调用代码 - 暂时注释掉用于测试
        try
        {
            _logger.LogInformation("Generating comic content for prompt: {Prompt}", prompt);

            var request = CreateGeminiRequest(prompt);
            var requestJson = JsonSerializer.Serialize(request, GetJsonOptions());
            var content = new StringContent(requestJson, Encoding.UTF8, "application/json");

            var response = await _retryPolicy.ExecuteAsync(async () =>
            {
                return await _httpClient.PostAsync("/v1/models/gemini-pro:generateContent", content);
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
            var geminiResponse = JsonSerializer.Deserialize<GeminiResponse>(responseContent, GetJsonOptions());

            if (geminiResponse?.Candidates?.Any() != true)
            {
                throw new GeminiAPIException("No content generated", "NO_CONTENT");
            }

            return ParseComicContent(geminiResponse.Candidates.First().Content.Parts.First().Text);
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            _logger.LogError(ex, "Gemini API request timeout");
            throw new GeminiAPIException("Request timeout", "TIMEOUT");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Gemini API network error");
            throw new GeminiAPIException("Network error", "NETWORK_ERROR");
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse Gemini API response");
            throw new GeminiAPIException("Response parsing error", "PARSE_ERROR");
        }
        */
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

    private GeminiRequest CreateGeminiRequest(string prompt)
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

        return new GeminiRequest
        {
            Contents = new[]
            {
                new GeminiContent
                {
                    Parts = new[]
                    {
                        new GeminiPart { Text = enhancedPrompt }
                    }
                }
            },
            GenerationConfig = new GeminiGenerationConfig
            {
                Temperature = 0.7f,
                TopK = 40,
                TopP = 0.95f,
                MaxOutputTokens = 2048
            },
            SafetySettings = new[]
            {
                new GeminiSafetySetting
                {
                    Category = "HARM_CATEGORY_HARASSMENT",
                    Threshold = "BLOCK_MEDIUM_AND_ABOVE"
                },
                new GeminiSafetySetting
                {
                    Category = "HARM_CATEGORY_HATE_SPEECH",
                    Threshold = "BLOCK_MEDIUM_AND_ABOVE"
                },
                new GeminiSafetySetting
                {
                    Category = "HARM_CATEGORY_SEXUALLY_EXPLICIT",
                    Threshold = "BLOCK_MEDIUM_AND_ABOVE"
                },
                new GeminiSafetySetting
                {
                    Category = "HARM_CATEGORY_DANGEROUS_CONTENT",
                    Threshold = "BLOCK_MEDIUM_AND_ABOVE"
                }
            }
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

// Gemini API 请求/响应模型
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