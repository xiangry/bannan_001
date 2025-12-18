using MathComicGenerator.Shared.Interfaces;
using MathComicGenerator.Shared.Models;
using MathComicGenerator.Shared.Services;
using System.Text.RegularExpressions;

namespace MathComicGenerator.Api.Services;

public class ComicGenerationService : IComicGenerationService
{
    private readonly IGeminiAPIService _geminiAPIService;
    private readonly IDeepSeekAPIService _deepSeekAPIService;
    private readonly MathConceptValidator _validator;
    private readonly MathContentDetector _contentDetector;
    private readonly ContentSafetyFilter _safetyFilter;
    private readonly ILogger<ComicGenerationService> _logger;

    // 不当内容关键词
    private static readonly HashSet<string> UnsafeKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "暴力", "打架", "伤害", "恐怖", "害怕", "死亡", "血", "武器",
        "violence", "fight", "hurt", "scary", "fear", "death", "blood", "weapon",
        "危险", "不安全", "坏人", "小偷", "犯罪",
        "danger", "unsafe", "bad guy", "thief", "crime"
    };

    public ComicGenerationService(
        IGeminiAPIService geminiAPIService,
        IDeepSeekAPIService deepSeekAPIService,
        ILogger<ComicGenerationService> logger)
    {
        _geminiAPIService = geminiAPIService;
        _deepSeekAPIService = deepSeekAPIService;
        _validator = new MathConceptValidator();
        _contentDetector = new MathContentDetector();
        _safetyFilter = new ContentSafetyFilter();
        _logger = logger;
    }

    public async Task<MultiPanelComic> GenerateComicAsync(MathConcept concept, GenerationOptions options)
    {
        _logger.LogInformation("Starting comic generation for concept: {Topic}", concept.Topic);

        try
        {
            // 1. 验证输入
            var validationResult = ValidateConcept(concept.Topic);
            if (!validationResult.IsValid)
            {
                throw new ArgumentException(validationResult.ErrorMessage);
            }

            // 2. 验证面板数量
            ValidatePanelCount(options.PanelCount);

            // 3. 创建增强的提示词
            var enhancedPrompt = CreateEnhancedPrompt(concept, options);

            // 4. 调用AI生成内容（使用备用API逻辑）
            var comicContent = await GenerateContentWithFallbackAsync(enhancedPrompt);

            // 5. 内容安全过滤
            var filteredContent = ApplyContentSafetyFilter(comicContent);

            // 6. 调整语言复杂度
            var adjustedContent = AdjustLanguageComplexity(filteredContent, options.AgeGroup);

            // 7. 确保面板数量正确
            var finalContent = EnsurePanelCount(adjustedContent, options.PanelCount);

            // 8. 创建最终的漫画对象
            var comic = await CreateMultiPanelComicAsync(finalContent, concept, options);

            _logger.LogInformation("Comic generation completed successfully for concept: {Topic}", concept.Topic);
            return comic;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating comic for concept: {Topic}", concept.Topic);
            throw;
        }
    }

    public async Task<MultiPanelComic> GenerateComicFromPromptAsync(string prompt, GenerationOptions options)
    {
        _logger.LogInformation("Starting comic generation from custom prompt");

        try
        {
            // 1. 验证面板数量
            ValidatePanelCount(options.PanelCount);

            // 2. 增强用户提示词
            var enhancedPrompt = EnhanceUserPrompt(prompt, options);

            // 3. 调用AI生成内容（使用备用API逻辑）
            var comicContent = await GenerateContentWithFallbackAsync(enhancedPrompt);

            // 4. 内容安全过滤
            var filteredContent = ApplyContentSafetyFilter(comicContent);

            // 5. 调整语言复杂度
            var adjustedContent = AdjustLanguageComplexity(filteredContent, options.AgeGroup);

            // 6. 确保面板数量正确
            var finalContent = EnsurePanelCount(adjustedContent, options.PanelCount);

            // 7. 创建最终的漫画对象（从提示词推断概念）
            var inferredConcept = InferMathConceptFromPrompt(prompt);
            var comic = await CreateMultiPanelComicAsync(finalContent, inferredConcept, options);

            _logger.LogInformation("Comic generation from prompt completed successfully");
            return comic;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating comic from prompt");
            throw;
        }
    }

    public ValidationResult ValidateConcept(string concept)
    {
        return _validator.ValidateInput(concept);
    }

    private void ValidatePanelCount(int panelCount)
    {
        if (panelCount < 3 || panelCount > 6)
        {
            throw new ArgumentException($"面板数量必须在3-6之间，当前值：{panelCount}");
        }
    }

    private string CreateEnhancedPrompt(MathConcept concept, GenerationOptions options)
    {
        var ageGroupDescription = GetAgeGroupDescription(options.AgeGroup);
        var visualStyleDescription = GetVisualStyleDescription(options.VisualStyle);
        var languageInstruction = GetLanguageInstruction(options.Language);

        return $@"
请为{ageGroupDescription}创建一个关于'{concept.Topic}'的{options.PanelCount}格漫画故事。

数学概念详情：
- 主题：{concept.Topic}
- 难度级别：{concept.Difficulty}
- 关键词：{string.Join(", ", concept.Keywords)}

要求：
1. 创建恰好{options.PanelCount}个连续的漫画面板
2. {visualStyleDescription}
3. 内容必须适合儿童，积极正面，无暴力或恐怖元素
4. {languageInstruction}
5. 每个面板都要推进故事情节，帮助理解数学概念
6. 包含有趣的角色（如小动物、友好的老师、同学等）
7. 通过具体的例子和情境来解释抽象的数学概念

面板结构要求：
- 面板1：介绍问题或情境
- 面板2-{options.PanelCount - 1}：逐步解决问题，展示数学概念
- 面板{options.PanelCount}：总结和应用

请确保内容教育性强，富有趣味性，能够帮助儿童更好地理解和记忆数学概念。";
    }

    private string GetAgeGroupDescription(AgeGroup ageGroup)
    {
        return ageGroup switch
        {
            AgeGroup.Preschool => "5-6岁学龄前儿童",
            AgeGroup.Elementary => "6岁以上小学及以上学生",
            _ => "小学生"
        };
    }

    private string GetVisualStyleDescription(VisualStyle style)
    {
        return style switch
        {
            VisualStyle.Cartoon => "使用卡通风格，色彩鲜艳，角色可爱",
            VisualStyle.Realistic => "使用写实风格，接近真实场景",
            VisualStyle.Minimalist => "使用简约风格，线条简洁，重点突出",
            VisualStyle.Colorful => "使用丰富多彩的颜色，视觉效果活泼",
            _ => "使用卡通风格"
        };
    }

    private string GetLanguageInstruction(Language language)
    {
        return language switch
        {
            Language.Chinese => "使用简体中文，语言简单易懂",
            Language.English => "Use simple English, easy to understand",
            _ => "使用简体中文"
        };
    }

    private ComicContent ApplyContentSafetyFilter(ComicContent content)
    {
        var filteredContent = new ComicContent
        {
            Title = _safetyFilter.FilterText(content.Title),
            Panels = new List<PanelContent>()
        };

        foreach (var panel in content.Panels)
        {
            var filteredPanel = new PanelContent
            {
                ImageDescription = _safetyFilter.FilterText(panel.ImageDescription),
                Dialogue = panel.Dialogue.Select(d => _safetyFilter.FilterText(d)).ToList(),
                Narration = panel.Narration != null ? _safetyFilter.FilterText(panel.Narration) : null
            };

            // 检查是否包含不当内容
            if (!ContainsUnsafeContent(filteredPanel))
            {
                filteredContent.Panels.Add(filteredPanel);
            }
            else
            {
                _logger.LogWarning("Filtered out unsafe content in panel");
                // 创建一个安全的替代面板
                filteredContent.Panels.Add(CreateSafeFallbackPanel());
            }
        }

        return filteredContent;
    }

    private bool ContainsUnsafeContent(PanelContent panel)
    {
        var allText = $"{panel.ImageDescription} {string.Join(" ", panel.Dialogue)} {panel.Narration}";
        return UnsafeKeywords.Any(keyword => allText.Contains(keyword, StringComparison.OrdinalIgnoreCase));
    }

    private PanelContent CreateSafeFallbackPanel()
    {
        return new PanelContent
        {
            ImageDescription = "一个友好的老师在教室里微笑着指向黑板",
            Dialogue = new List<string> { "让我们一起学习数学吧！" },
            Narration = "学习数学是一件快乐的事情"
        };
    }

    private ComicContent AdjustLanguageComplexity(ComicContent content, AgeGroup ageGroup)
    {
        var complexityLevel = GetComplexityLevel(ageGroup);
        
        var adjustedContent = new ComicContent
        {
            Title = AdjustTitleComplexity(content.Title, complexityLevel),
            Panels = new List<PanelContent>()
        };

        foreach (var panel in content.Panels)
        {
            adjustedContent.Panels.Add(new PanelContent
            {
                ImageDescription = AdjustTextComplexity(panel.ImageDescription, complexityLevel),
                Dialogue = panel.Dialogue.Select(d => AdjustTextComplexity(d, complexityLevel)).ToList(),
                Narration = panel.Narration != null ? AdjustTextComplexity(panel.Narration, complexityLevel) : null
            });
        }

        return adjustedContent;
    }

    private int GetComplexityLevel(AgeGroup ageGroup)
    {
        return ageGroup switch
        {
            AgeGroup.Preschool => 1,    // 最简单
            AgeGroup.Elementary => 2,   // 简单到中等
            _ => 2
        };
    }

    private string AdjustTitleComplexity(string text, int complexityLevel)
    {
        if (string.IsNullOrEmpty(text)) return text;

        // 对于标题，只进行简单的词汇替换，不截断长度
        var simplifications = new Dictionary<string, string>
        {
            { "计算", "算" },
            { "运算", "算" },
            { "数学", "数字" },
            { "解决", "做" },
            { "理解", "知道" }
        };

        var result = text;
        if (complexityLevel == 1) // 只对学龄前进行简化
        {
            foreach (var pair in simplifications)
            {
                result = result.Replace(pair.Key, pair.Value);
            }
        }

        return result;
    }

    private string AdjustTextComplexity(string text, int complexityLevel)
    {
        if (string.IsNullOrEmpty(text)) return text;

        // 根据复杂度级别调整文本
        switch (complexityLevel)
        {
            case 1: // 学龄前 - 最简单
                return SimplifyForPreschool(text);
            case 2: // 小学 - 简单
                return SimplifyForElementary(text);
            case 3: // 中学 - 中等
                return text; // 保持原样
            case 4: // 高中 - 可以更复杂
                return text; // 保持原样
            default:
                return text;
        }
    }

    private string SimplifyForPreschool(string text)
    {
        // 替换复杂词汇为简单词汇
        var simplifications = new Dictionary<string, string>
        {
            { "计算", "算" },
            { "运算", "算" },
            { "数学", "数字" },
            { "解决", "做" },
            { "理解", "知道" },
            { "学习", "学" }
        };

        var result = text;
        foreach (var pair in simplifications)
        {
            result = result.Replace(pair.Key, pair.Value);
        }

        // 限制句子长度（仅对对话和叙述，不对标题）
        if (result.Length > 20)
        {
            var sentences = result.Split('。', '！', '？');
            result = sentences.FirstOrDefault()?.Trim() ?? result.Substring(0, Math.Min(20, result.Length));
            if (!result.EndsWith('。') && !result.EndsWith('！') && !result.EndsWith('？'))
            {
                result += "。";
            }
        }

        return result;
    }

    private string SimplifyForElementary(string text)
    {
        // 替换一些复杂词汇
        var simplifications = new Dictionary<string, string>
        {
            { "非常", "很" },
            { "特别", "很" },
            { "困难", "难" },
            { "简单", "容易" }
        };

        var result = text;
        foreach (var pair in simplifications)
        {
            result = result.Replace(pair.Key, pair.Value);
        }

        return result;
    }

    private ComicContent EnsurePanelCount(ComicContent content, int targetPanelCount)
    {
        var panels = content.Panels.ToList();

        if (panels.Count == targetPanelCount)
        {
            return content;
        }

        if (panels.Count > targetPanelCount)
        {
            // 如果面板太多，保留最重要的面板
            panels = panels.Take(targetPanelCount).ToList();
        }
        else
        {
            // 如果面板太少，添加补充面板
            while (panels.Count < targetPanelCount)
            {
                panels.Add(CreateSupplementaryPanel(panels.Count + 1, targetPanelCount));
            }
        }

        return new ComicContent
        {
            Title = content.Title,
            Panels = panels
        };
    }

    private PanelContent CreateSupplementaryPanel(int panelNumber, int totalPanels)
    {
        if (panelNumber == totalPanels)
        {
            // 最后一个面板 - 总结
            return new PanelContent
            {
                ImageDescription = "所有角色一起庆祝学会了新的数学知识",
                Dialogue = new List<string> { "太好了！我们学会了新的数学知识！" },
                Narration = "通过这个故事，我们学会了重要的数学概念。"
            };
        }
        else
        {
            // 中间面板 - 过渡
            return new PanelContent
            {
                ImageDescription = "角色们继续探索和学习数学概念",
                Dialogue = new List<string> { "让我们继续学习吧！" },
                Narration = "学习过程中，我们发现了更多有趣的数学规律。"
            };
        }
    }

    private string EnhanceUserPrompt(string userPrompt, GenerationOptions options)
    {
        var ageGroupDescription = GetAgeGroupDescription(options.AgeGroup);
        var visualStyleDescription = GetVisualStyleDescription(options.VisualStyle);
        var languageInstruction = GetLanguageInstruction(options.Language);

        return $@"
基于以下用户提示词，为{ageGroupDescription}创建一个{options.PanelCount}格教育漫画：

用户提示词：
{userPrompt}

技术要求：
1. 创建恰好{options.PanelCount}个连续的漫画面板
2. {visualStyleDescription}
3. 内容必须适合儿童，积极正面，无暴力或恐怖元素
4. {languageInstruction}
5. 确保内容具有教育价值，特别是数学学习方面
6. 包含有趣的角色和引人入胜的故事情节
7. 每个面板都要推进故事，帮助理解相关概念

请基于用户的创意想法，创造一个既有趣又有教育意义的漫画故事。";
    }

    private MathConcept InferMathConceptFromPrompt(string prompt)
    {
        // 从提示词中推断数学概念
        var mathKeywords = new Dictionary<string, string>
        {
            { "加法", "加法运算" },
            { "减法", "减法运算" },
            { "乘法", "乘法运算" },
            { "除法", "除法运算" },
            { "分数", "分数概念" },
            { "几何", "几何图形" },
            { "圆形", "圆形和圆周" },
            { "三角形", "三角形性质" },
            { "正方形", "正方形和矩形" },
            { "数字", "数字认知" },
            { "计数", "数数和计数" },
            { "比较", "数量比较" },
            { "测量", "长度和测量" },
            { "时间", "时间概念" },
            { "钱币", "货币和购物" }
        };

        var detectedConcept = "数学概念"; // 默认值
        var keywords = new List<string>();

        foreach (var keyword in mathKeywords)
        {
            if (prompt.Contains(keyword.Key, StringComparison.OrdinalIgnoreCase))
            {
                detectedConcept = keyword.Value;
                keywords.Add(keyword.Key);
                break;
            }
        }

        return new MathConcept
        {
            Topic = detectedConcept,
            Keywords = keywords,
            Difficulty = DifficultyLevel.Beginner
        };
    }

    private async Task<MultiPanelComic> CreateMultiPanelComicAsync(ComicContent content, MathConcept concept, GenerationOptions options)
    {
        var comic = new MultiPanelComic
        {
            Id = Guid.NewGuid().ToString(),
            Title = content.Title,
            Panels = new List<ComicPanel>(),
            CreatedAt = DateTime.UtcNow,
            Metadata = new ComicMetadata
            {
                MathConcept = concept.Topic,
                GenerationOptions = options,
                Format = ImageFormat.PNG,
                Tags = concept.Keywords.ToList()
            }
        };

        // 不生成本地图片，只创建面板内容结构
        for (int i = 0; i < content.Panels.Count; i++)
        {
            var panel = content.Panels[i];
            
            comic.Panels.Add(new ComicPanel
            {
                Id = Guid.NewGuid().ToString(),
                Order = i + 1,
                ImageUrl = $"/api/placeholder/panel_{i + 1}", // 占位符URL，实际图片由AI生成
                Dialogue = panel.Dialogue,
                Narration = panel.Narration
            });
        }

        return comic;
    }

    /// <summary>
    /// 使用备用API逻辑生成内容，优先使用Gemini，失败时使用DeepSeek
    /// </summary>
    private async Task<ComicContent> GenerateContentWithFallbackAsync(string prompt)
    {
        var startTime = DateTime.UtcNow;
        Exception? geminiException = null;
        Exception? deepSeekException = null;

        try
        {
            // 首先尝试使用Gemini API
            _logger.LogInformation("尝试使用Gemini API生成内容，提示词长度: {PromptLength}", prompt.Length);
            var geminiStartTime = DateTime.UtcNow;
            
            var result = await _geminiAPIService.GenerateComicContentAsync(prompt);
            
            var geminiDuration = DateTime.UtcNow - geminiStartTime;
            _logger.LogInformation("Gemini API调用成功，耗时: {Duration}ms", geminiDuration.TotalMilliseconds);
            
            return result;
        }
        catch (Exception geminiEx)
        {
            geminiException = geminiEx;
            var geminiDuration = DateTime.UtcNow - startTime;
            _logger.LogWarning(geminiEx, "Gemini API调用失败，耗时: {Duration}ms，错误类型: {ExceptionType}，错误消息: {ErrorMessage}", 
                geminiDuration.TotalMilliseconds, geminiEx.GetType().Name, geminiEx.Message);
            
            try
            {
                // 使用DeepSeek API作为备用
                var deepSeekStartTime = DateTime.UtcNow;
                _logger.LogInformation("尝试使用DeepSeek API作为备用");
                
                var systemPrompt = "你是一个专业的儿童教育漫画创作助手。请根据用户的提示词生成适合儿童的多格漫画内容。";
                var deepSeekResponse = await _deepSeekAPIService.GeneratePromptAsync(systemPrompt, prompt);
                
                var deepSeekDuration = DateTime.UtcNow - deepSeekStartTime;
                _logger.LogInformation("DeepSeek API调用成功，耗时: {Duration}ms", deepSeekDuration.TotalMilliseconds);
                
                // 解析DeepSeek的响应为ComicContent格式
                return ParseDeepSeekResponse(deepSeekResponse);
            }
            catch (Exception deepSeekEx)
            {
                deepSeekException = deepSeekEx;
                var totalDuration = DateTime.UtcNow - startTime;
                _logger.LogError(deepSeekEx, "DeepSeek API也调用失败，总耗时: {Duration}ms，错误类型: {ExceptionType}，错误消息: {ErrorMessage}", 
                    totalDuration.TotalMilliseconds, deepSeekEx.GetType().Name, deepSeekEx.Message);
                
                // 如果两个API都失败，抛出详细的异常信息
                var errorMessage = $"所有API调用都失败。Gemini错误: {geminiException.Message}; DeepSeek错误: {deepSeekException.Message}; 总耗时: {totalDuration.TotalMilliseconds}ms";
                _logger.LogError("API调用完全失败: {ErrorMessage}", errorMessage);
                
                throw new InvalidOperationException(errorMessage, new AggregateException(geminiException, deepSeekException));
            }
        }
    }

    /// <summary>
    /// 解析DeepSeek API的响应为ComicContent格式
    /// </summary>
    private ComicContent ParseDeepSeekResponse(string response)
    {
        try
        {
            // 尝试从响应中提取漫画内容
            var lines = response.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            var title = "数学漫画";
            var panels = new List<PanelContent>();

            // 简单的解析逻辑
            var currentPanel = new PanelContent();
            var panelCount = 0;

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                
                if (trimmedLine.Contains("标题") || trimmedLine.Contains("title"))
                {
                    title = ExtractTitle(trimmedLine);
                }
                else if (trimmedLine.Contains("面板") || trimmedLine.Contains("panel"))
                {
                    if (currentPanel.ImageDescription != null)
                    {
                        panels.Add(currentPanel);
                    }
                    currentPanel = new PanelContent();
                    panelCount++;
                }
                else if (trimmedLine.Contains("图像") || trimmedLine.Contains("场景"))
                {
                    currentPanel.ImageDescription = ExtractContent(trimmedLine);
                }
                else if (trimmedLine.Contains("对话") || trimmedLine.Contains("dialogue"))
                {
                    currentPanel.Dialogue = new List<string> { ExtractContent(trimmedLine) };
                }
                else if (trimmedLine.Contains("旁白") || trimmedLine.Contains("narration"))
                {
                    currentPanel.Narration = ExtractContent(trimmedLine);
                }
            }

            // 添加最后一个面板
            if (currentPanel.ImageDescription != null)
            {
                panels.Add(currentPanel);
            }

            // 确保至少有3个面板
            while (panels.Count < 3)
            {
                panels.Add(CreateDefaultPanel(panels.Count + 1));
            }

            return new ComicContent
            {
                Title = title,
                Panels = panels
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "解析DeepSeek响应失败");
            return CreateFallbackComicContent("数学学习");
        }
    }

    private string ExtractTitle(string line)
    {
        var colonIndex = line.IndexOf(':');
        if (colonIndex > 0 && colonIndex < line.Length - 1)
        {
            return line.Substring(colonIndex + 1).Trim().Trim('"');
        }
        return "数学漫画";
    }

    private string ExtractContent(string line)
    {
        var colonIndex = line.IndexOf(':');
        if (colonIndex > 0 && colonIndex < line.Length - 1)
        {
            return line.Substring(colonIndex + 1).Trim().Trim('"');
        }
        return line.Trim();
    }

    private PanelContent CreateDefaultPanel(int panelNumber)
    {
        return new PanelContent
        {
            ImageDescription = $"第{panelNumber}个面板：友好的角色在学习数学",
            Dialogue = new List<string> { "让我们一起学习数学吧！" },
            Narration = "数学学习是一件有趣的事情。"
        };
    }

    /// <summary>
    /// 创建备用的漫画内容，当所有API都失败时使用
    /// </summary>
    private ComicContent CreateFallbackComicContent(string topic)
    {
        _logger.LogInformation("创建备用漫画内容，主题: {Topic}", topic);
        
        return new ComicContent
        {
            Title = $"数学学习：{topic}",
            Panels = new List<PanelContent>
            {
                new PanelContent
                {
                    ImageDescription = "一个友好的老师站在黑板前，黑板上写着数学题目",
                    Dialogue = new List<string> { "今天我们来学习有趣的数学！" },
                    Narration = "数学课开始了，老师准备教大家新的知识。"
                },
                new PanelContent
                {
                    ImageDescription = "学生们认真听讲，举手提问",
                    Dialogue = new List<string> { "老师，这个怎么算呢？" },
                    Narration = "同学们积极参与，提出自己的疑问。"
                },
                new PanelContent
                {
                    ImageDescription = "老师耐心解释，在黑板上演示解题步骤",
                    Dialogue = new List<string> { "让我来一步步教你们！" },
                    Narration = "老师详细讲解每个步骤，确保大家都能理解。"
                },
                new PanelContent
                {
                    ImageDescription = "学生们恍然大悟，开心地鼓掌",
                    Dialogue = new List<string> { "原来如此！我明白了！" },
                    Narration = "通过老师的耐心教导，同学们成功掌握了新的数学知识。"
                }
            }
        };
    }
}

// 内容安全过滤器
public class ContentSafetyFilter
{
    private static readonly Dictionary<string, string> SafeReplacements = new()
    {
        { "打架", "讨论" },
        { "暴力", "活动" },
        { "害怕", "好奇" },
        { "恐怖", "有趣" },
        { "危险", "挑战" },
        { "坏人", "朋友" },
        { "fight", "discuss" },
        { "violence", "activity" },
        { "scary", "interesting" },
        { "danger", "challenge" }
    };

    public string FilterText(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;

        var filtered = text;
        foreach (var replacement in SafeReplacements)
        {
            filtered = filtered.Replace(replacement.Key, replacement.Value, StringComparison.OrdinalIgnoreCase);
        }

        return filtered;
    }
}