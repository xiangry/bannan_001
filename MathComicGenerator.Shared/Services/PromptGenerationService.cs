using MathComicGenerator.Shared.Interfaces;
using MathComicGenerator.Shared.Models;
using Microsoft.Extensions.Logging;
using System.Text;

namespace MathComicGenerator.Shared.Services;

/// <summary>
/// 提示词生成服务实现
/// </summary>
public class PromptGenerationService : IPromptGenerationService
{
    private readonly IDeepSeekAPIService _deepSeekService;
    private readonly ILogger<PromptGenerationService> _logger;

    public PromptGenerationService(
        IDeepSeekAPIService deepSeekService,
        ILogger<PromptGenerationService> logger)
    {
        _deepSeekService = deepSeekService;
        _logger = logger;
    }

    public async Task<PromptGenerationResponse> GeneratePromptAsync(MathConcept mathConcept, GenerationOptions options)
    {
        try
        {
            _logger.LogInformation("Generating prompt for concept: {Concept}", mathConcept.Topic);

            var systemPrompt = BuildSystemPrompt(options);
            var userPrompt = BuildUserPrompt(mathConcept, options);

            var response = await _deepSeekService.GeneratePromptAsync(systemPrompt, userPrompt);

            var generatedPrompt = ExtractPromptFromResponse(response);
            var suggestions = ExtractSuggestionsFromResponse(response);

            return new PromptGenerationResponse
            {
                MathConcept = mathConcept.Topic,
                GeneratedPrompt = generatedPrompt,
                Options = options,
                Suggestions = suggestions
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating prompt for concept: {Concept}", mathConcept.Topic);
            throw;
        }
    }

    public ValidationResult ValidatePrompt(string prompt)
    {
        var errors = new List<string>();
        var suggestions = new List<string>();

        // 检查提示词长度
        if (string.IsNullOrWhiteSpace(prompt))
        {
            errors.Add("提示词不能为空");
        }
        else if (prompt.Length < 10)
        {
            errors.Add("提示词太短，请提供更详细的描述");
            suggestions.Add("添加更多关于漫画风格、角色和场景的描述");
        }
        else if (prompt.Length > 2000)
        {
            errors.Add("提示词太长，请简化描述");
            suggestions.Add("保留核心要素，删除不必要的细节");
        }

        // 检查是否包含数学相关内容
        var mathKeywords = new[] { "数学", "计算", "公式", "图形", "数字", "运算", "方程", "几何", "代数", "统计" };
        if (!mathKeywords.Any(keyword => prompt.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
        {
            suggestions.Add("建议在提示词中明确包含数学概念相关的关键词");
        }

        // 检查是否包含漫画相关描述
        var comicKeywords = new[] { "漫画", "面板", "角色", "对话", "场景", "故事", "情节" };
        if (!comicKeywords.Any(keyword => prompt.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
        {
            suggestions.Add("建议添加漫画结构和视觉元素的描述");
        }

        return new ValidationResult
        {
            IsValid = !errors.Any(),
            ErrorMessage = string.Join("; ", errors),
            Suggestions = suggestions
        };
    }

    public async Task<string> OptimizePromptAsync(string prompt, GenerationOptions options)
    {
        try
        {
            _logger.LogInformation("Optimizing prompt");

            var optimizationInstructions = BuildOptimizationInstructions(options);
            var optimizedPrompt = await _deepSeekService.OptimizePromptAsync(prompt, optimizationInstructions);

            return ExtractOptimizedPromptFromResponse(optimizedPrompt);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error optimizing prompt");
            // 如果优化失败，返回原始提示词
            return prompt;
        }
    }

    private string BuildSystemPrompt(GenerationOptions options)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("你是一个专业的教育漫画提示词生成专家。你的任务是根据知识点生成详细的漫画创作提示词。");
        sb.AppendLine();
        sb.AppendLine("生成的提示词应该包含以下要素：");
        sb.AppendLine("1. 漫画整体结构和面板布局");
        sb.AppendLine("2. 主要角色设计和特征");
        sb.AppendLine("3. 每个面板的具体场景描述");
        sb.AppendLine("4. 对话内容和教育要点");
        sb.AppendLine("5. 视觉风格和色彩方案");
        sb.AppendLine();

        // 根据年龄组调整提示词风格
        sb.AppendLine($"目标年龄组: {GetAgeGroupDescription(options.AgeGroup)}");
        sb.AppendLine($"视觉风格: {GetVisualStyleDescription(options.VisualStyle)}");
        sb.AppendLine($"面板数量: {options.PanelCount}个面板");
        sb.AppendLine($"语言: {(options.Language == Language.Chinese ? "中文" : "英文")}");
        sb.AppendLine();

        sb.AppendLine("请确保生成的提示词：");
        sb.AppendLine("- 适合目标年龄组的理解水平");
        sb.AppendLine("- 包含准确的知识概念");
        sb.AppendLine("- 具有教育价值和趣味性");
        sb.AppendLine("- 描述清晰，便于图像生成");
        sb.AppendLine();
        sb.AppendLine("请按以下格式返回：");
        sb.AppendLine("提示词: [详细的漫画创作提示词]");
        sb.AppendLine();
        sb.AppendLine("改进建议:");
        sb.AppendLine("- [建议1]");
        sb.AppendLine("- [建议2]");
        sb.AppendLine("- [建议3]");

        return sb.ToString();
    }

    private string BuildUserPrompt(MathConcept mathConcept, GenerationOptions options)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine($"请为以下知识点生成漫画创作提示词：");
        sb.AppendLine();
        sb.AppendLine($"知识点: {mathConcept.Topic}");
        
        if (mathConcept.Keywords.Any())
        {
            sb.AppendLine($"关键词: {string.Join(", ", mathConcept.Keywords)}");
        }

        sb.AppendLine();
        sb.AppendLine("请生成一个详细的漫画创作提示词，包含完整的故事情节、角色设计和视觉描述。");
        sb.AppendLine("同时提供3-5个改进建议，帮助用户优化提示词。");

        return sb.ToString();
    }

    private string BuildOptimizationInstructions(GenerationOptions options)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("优化要求:");
        sb.AppendLine("1. 保持教育内容的核心价值");
        sb.AppendLine("2. 增强视觉描述的具体性和生动性");
        sb.AppendLine($"3. 确保适合{GetAgeGroupDescription(options.AgeGroup)}的理解水平");
        sb.AppendLine("4. 优化语言表达的清晰度和准确性");
        sb.AppendLine("5. 保持合理的长度，避免过于冗长");
        sb.AppendLine($"6. 体现{GetVisualStyleDescription(options.VisualStyle)}的特点");
        sb.AppendLine($"7. 确保{options.PanelCount}个面板的结构清晰");
        sb.AppendLine("8. 增加角色互动和情感表达");
        sb.AppendLine("9. 强化教育目标的实现");
        sb.AppendLine("10. 提升整体的趣味性和吸引力");

        return sb.ToString();
    }

    private string ExtractPromptFromResponse(string response)
    {
        // 从AI响应中提取主要的提示词内容
        var lines = response.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var promptLines = new List<string>();
        bool inPromptSection = false;

        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();
            
            // 跳过标题和说明性文字
            if (trimmedLine.StartsWith("提示词:") || 
                trimmedLine.StartsWith("漫画提示词:") ||
                trimmedLine.StartsWith("创作提示词:"))
            {
                inPromptSection = true;
                continue;
            }

            // 停止在建议部分
            if (trimmedLine.StartsWith("建议:") || 
                trimmedLine.StartsWith("改进建议:") ||
                trimmedLine.StartsWith("优化建议:"))
            {
                break;
            }

            if (inPromptSection || (!trimmedLine.StartsWith("##") && !trimmedLine.StartsWith("**")))
            {
                promptLines.Add(trimmedLine);
            }
        }

        var result = string.Join("\n", promptLines).Trim();
        
        // 如果没有找到明确的提示词部分，返回整个响应的前半部分
        if (string.IsNullOrEmpty(result))
        {
            var halfLength = response.Length / 2;
            result = response.Substring(0, Math.Min(halfLength, response.Length)).Trim();
        }

        return result;
    }

    private List<string> ExtractSuggestionsFromResponse(string response)
    {
        var suggestions = new List<string>();
        var lines = response.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        bool inSuggestionsSection = false;

        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();
            
            if (trimmedLine.StartsWith("建议:") || 
                trimmedLine.StartsWith("改进建议:") ||
                trimmedLine.StartsWith("优化建议:"))
            {
                inSuggestionsSection = true;
                continue;
            }

            if (inSuggestionsSection)
            {
                if (trimmedLine.StartsWith("-") || trimmedLine.StartsWith("•") || 
                    trimmedLine.StartsWith("*") || char.IsDigit(trimmedLine[0]))
                {
                    var suggestion = trimmedLine.TrimStart('-', '•', '*', ' ')
                                              .TrimStart("123456789.".ToCharArray())
                                              .Trim();
                    if (!string.IsNullOrEmpty(suggestion))
                    {
                        suggestions.Add(suggestion);
                    }
                }
            }
        }

        // 如果没有找到建议，提供默认建议
        if (!suggestions.Any())
        {
            suggestions.AddRange(new[]
            {
                "可以添加更多角色互动细节",
                "考虑增加视觉元素的描述",
                "确保数学概念解释清晰易懂"
            });
        }

        return suggestions;
    }

    private string ExtractOptimizedPromptFromResponse(string response)
    {
        // 简单提取优化后的提示词
        var lines = response.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        return string.Join("\n", lines.Where(line => 
            !line.Trim().StartsWith("优化后的提示词:") &&
            !line.Trim().StartsWith("##") &&
            !line.Trim().StartsWith("**")
        )).Trim();
    }

    private string GetAgeGroupDescription(AgeGroup ageGroup)
    {
        return ageGroup switch
        {
            AgeGroup.Preschool => "学龄前儿童 (3-5岁) - 需要简单直观的视觉元素",
            AgeGroup.Elementary => "小学生 (6-11岁) - 适合基础数学概念学习",
            AgeGroup.MiddleSchool => "中学生 (12-14岁) - 可以包含更复杂的数学概念",
            AgeGroup.HighSchool => "高中生 (15-18岁) - 适合高级数学概念和抽象思维",
            _ => "通用年龄组"
        };
    }

    private string GetVisualStyleDescription(VisualStyle style)
    {
        return style switch
        {
            VisualStyle.Cartoon => "卡通风格 - 可爱、夸张的角色设计",
            VisualStyle.Colorful => "多彩风格 - 鲜艳明亮的色彩搭配",
            VisualStyle.Realistic => "写实风格 - 接近真实的人物和场景",
            VisualStyle.Minimalist => "简约风格 - 简洁清晰的线条和构图",
            _ => "默认风格"
        };
    }
}