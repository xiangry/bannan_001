using MathComicGenerator.Shared.Interfaces;
using MathComicGenerator.Shared.Models;
using System.Text.RegularExpressions;

namespace MathComicGenerator.Shared.Services;

public class MathConceptValidator : IMathConceptValidator
{
    private static readonly HashSet<string> MathKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        // 基础数学概念
        "加法", "减法", "乘法", "除法", "addition", "subtraction", "multiplication", "division",
        "数字", "数学", "计算", "运算", "number", "math", "mathematics", "calculation",
        
        // 几何概念
        "几何", "图形", "三角形", "正方形", "圆形", "rectangle", "triangle", "circle", "square",
        "geometry", "shape", "polygon", "angle", "area", "perimeter",
        
        // 代数概念
        "代数", "方程", "变量", "函数", "algebra", "equation", "variable", "function",
        "expression", "formula", "solve", "unknown",
        
        // 分数和小数
        "分数", "小数", "百分比", "fraction", "decimal", "percentage", "ratio", "proportion",
        
        // 统计和概率
        "统计", "概率", "平均数", "statistics", "probability", "average", "mean", "median",
        
        // 测量
        "测量", "长度", "重量", "时间", "measurement", "length", "weight", "time", "volume"
    };

    private static readonly HashSet<string> NonMathKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "游戏", "电影", "音乐", "体育", "历史", "地理", "生物", "化学", "物理",
        "game", "movie", "music", "sports", "history", "geography", "biology", "chemistry", "physics",
        "文学", "艺术", "政治", "经济", "社会", "literature", "art", "politics", "economics", "social"
    };

    public ValidationResult ValidateInput(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return new ValidationResult
            {
                IsValid = false,
                ErrorMessage = "请输入数学知识点",
                Suggestions = new List<string> { "例如：加法运算", "分数的概念", "几何图形" }
            };
        }

        // 清理输入
        var cleanedInput = CleanInput(input);
        
        if (cleanedInput.Length > 200)
        {
            return new ValidationResult
            {
                IsValid = false,
                ErrorMessage = "输入内容过长，请控制在200字符以内",
                Suggestions = new List<string> { "请简化描述", "专注于核心概念" }
            };
        }

        // 检查是否包含数学相关内容
        if (!IsMathematicalContent(cleanedInput))
        {
            return new ValidationResult
            {
                IsValid = false,
                ErrorMessage = "请输入有效的数学概念",
                Suggestions = new List<string> 
                { 
                    "加法和减法", 
                    "几何图形认识", 
                    "分数的基本概念",
                    "时间的计算"
                }
            };
        }

        // 检查是否包含非数学内容
        if (ContainsNonMathContent(cleanedInput))
        {
            return new ValidationResult
            {
                IsValid = false,
                ErrorMessage = "检测到非数学相关内容，请提供数学概念",
                Suggestions = new List<string> 
                { 
                    "数字运算", 
                    "图形认识", 
                    "测量概念" 
                }
            };
        }

        return new ValidationResult
        {
            IsValid = true,
            ErrorMessage = string.Empty,
            Suggestions = new List<string>()
        };
    }

    public bool IsMathematicalContent(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return false;

        // 检查整个内容是否包含数学关键词
        return MathKeywords.Any(keyword => content.Contains(keyword, StringComparison.OrdinalIgnoreCase));
    }

    public MathConcept ParseMathConcept(string input)
    {
        var cleanedInput = CleanInput(input);
        
        return new MathConcept
        {
            Topic = cleanedInput,
            AgeGroup = AgeGroup.Elementary, // 默认值，可以后续调整
            Difficulty = DetermineDifficulty(cleanedInput),
            Keywords = ExtractKeywords(cleanedInput)
        };
    }

    public List<string> GetSuggestions(string invalidInput)
    {
        if (string.IsNullOrWhiteSpace(invalidInput))
        {
            return new List<string> { "例如：加法运算", "分数的概念", "几何图形" };
        }

        // 基于输入内容提供相关建议
        var suggestions = new List<string>();
        
        if (invalidInput.Contains("数字") || invalidInput.Contains("number"))
        {
            suggestions.AddRange(new[] { "数字的加减法", "数字的大小比较", "数字的认识" });
        }
        else if (invalidInput.Contains("图") || invalidInput.Contains("shape"))
        {
            suggestions.AddRange(new[] { "几何图形认识", "图形的面积计算", "图形的周长" });
        }
        else
        {
            suggestions.AddRange(new[] { "加法和减法", "乘法口诀", "分数概念", "时间计算" });
        }

        return suggestions;
    }

    private string CleanInput(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        // 移除多余的空白字符
        var cleaned = Regex.Replace(input.Trim(), @"\s+", " ");
        
        return cleaned;
    }

    private bool ContainsNonMathContent(string content)
    {
        // 检查整个内容是否包含非数学关键词
        return NonMathKeywords.Any(keyword => content.Contains(keyword, StringComparison.OrdinalIgnoreCase));
    }

    private DifficultyLevel DetermineDifficulty(string concept)
    {
        // 基于概念复杂度确定难度
        var complexityIndicators = new[]
        {
            "方程", "函数", "代数", "equation", "function", "algebra",
            "微积分", "calculus", "derivative", "integral"
        };

        var basicIndicators = new[]
        {
            "加法", "减法", "数字", "addition", "subtraction", "number",
            "计数", "counting", "基础", "basic"
        };

        if (complexityIndicators.Any(indicator => 
            concept.Contains(indicator, StringComparison.OrdinalIgnoreCase)))
        {
            return DifficultyLevel.Advanced;
        }

        if (basicIndicators.Any(indicator => 
            concept.Contains(indicator, StringComparison.OrdinalIgnoreCase)))
        {
            return DifficultyLevel.Beginner;
        }

        return DifficultyLevel.Elementary;
    }

    private List<string> ExtractKeywords(string concept)
    {
        var keywords = new List<string>();
        
        // 检查内容中包含的数学关键词
        foreach (var keyword in MathKeywords)
        {
            if (concept.Contains(keyword, StringComparison.OrdinalIgnoreCase) && !keywords.Contains(keyword))
            {
                keywords.Add(keyword);
            }
        }

        return keywords;
    }
}