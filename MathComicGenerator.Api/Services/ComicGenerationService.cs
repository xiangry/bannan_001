using MathComicGenerator.Shared.Interfaces;
using MathComicGenerator.Shared.Models;
using MathComicGenerator.Shared.Services;
using System.Text.RegularExpressions;

namespace MathComicGenerator.Api.Services;

public class ComicGenerationService : IComicGenerationService
{
    private readonly IGeminiAPIService _geminiAPIService;
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
        ILogger<ComicGenerationService> logger)
    {
        _geminiAPIService = geminiAPIService;
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

            // 4. 调用AI生成内容
            var comicContent = await _geminiAPIService.GenerateComicContentAsync(enhancedPrompt);

            // 5. 内容安全过滤
            var filteredContent = ApplyContentSafetyFilter(comicContent);

            // 6. 调整语言复杂度
            var adjustedContent = AdjustLanguageComplexity(filteredContent, options.AgeGroup);

            // 7. 确保面板数量正确
            var finalContent = EnsurePanelCount(adjustedContent, options.PanelCount);

            // 8. 创建最终的漫画对象
            var comic = CreateMultiPanelComic(finalContent, concept, options);

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

            // 3. 调用AI生成内容
            var comicContent = await _geminiAPIService.GenerateComicContentAsync(enhancedPrompt);

            // 4. 内容安全过滤
            var filteredContent = ApplyContentSafetyFilter(comicContent);

            // 5. 调整语言复杂度
            var adjustedContent = AdjustLanguageComplexity(filteredContent, options.AgeGroup);

            // 6. 确保面板数量正确
            var finalContent = EnsurePanelCount(adjustedContent, options.PanelCount);

            // 7. 创建最终的漫画对象（从提示词推断概念）
            var inferredConcept = InferMathConceptFromPrompt(prompt);
            var comic = CreateMultiPanelComic(finalContent, inferredConcept, options);

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
            AgeGroup.Preschool => "3-5岁学龄前儿童",
            AgeGroup.Elementary => "6-11岁小学生",
            AgeGroup.MiddleSchool => "12-14岁中学生",
            AgeGroup.HighSchool => "15-18岁高中生",
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
            Title = AdjustTextComplexity(content.Title, complexityLevel),
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
            AgeGroup.Elementary => 2,   // 简单
            AgeGroup.MiddleSchool => 3, // 中等
            AgeGroup.HighSchool => 4,   // 复杂
            _ => 2
        };
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

        // 限制句子长度
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

    private MultiPanelComic CreateMultiPanelComic(ComicContent content, MathConcept concept, GenerationOptions options)
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

        for (int i = 0; i < content.Panels.Count; i++)
        {
            var panel = content.Panels[i];
            comic.Panels.Add(new ComicPanel
            {
                Id = Guid.NewGuid().ToString(),
                Order = i + 1,
                ImageUrl = $"panel_{i + 1}.png", // 占位符，实际应该生成图片
                Dialogue = panel.Dialogue,
                Narration = panel.Narration
            });
        }

        return comic;
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