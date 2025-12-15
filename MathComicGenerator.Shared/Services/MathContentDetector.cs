using MathComicGenerator.Shared.Interfaces;
using System.Text.RegularExpressions;

namespace MathComicGenerator.Shared.Services;

public class MathContentDetector : IMathContentDetector
{
    private static readonly Dictionary<string, double> MathConceptWeights = new()
    {
        // 基础运算 - 高权重
        { "加法", 1.0 }, { "减法", 1.0 }, { "乘法", 1.0 }, { "除法", 1.0 },
        { "addition", 1.0 }, { "subtraction", 1.0 }, { "multiplication", 1.0 }, { "division", 1.0 },
        
        // 数字和计算 - 高权重
        { "数字", 0.9 }, { "计算", 0.9 }, { "运算", 0.9 },
        { "number", 0.9 }, { "calculation", 0.9 }, { "arithmetic", 0.9 },
        
        // 几何概念 - 中高权重
        { "几何", 0.8 }, { "图形", 0.8 }, { "三角形", 0.8 }, { "正方形", 0.8 }, { "圆形", 0.8 },
        { "geometry", 0.8 }, { "shape", 0.8 }, { "triangle", 0.8 }, { "square", 0.8 }, { "circle", 0.8 },
        
        // 代数概念 - 中权重
        { "代数", 0.7 }, { "方程", 0.7 }, { "变量", 0.7 },
        { "algebra", 0.7 }, { "equation", 0.7 }, { "variable", 0.7 },
        
        // 分数和小数 - 中权重
        { "分数", 0.7 }, { "小数", 0.7 }, { "百分比", 0.7 },
        { "fraction", 0.7 }, { "decimal", 0.7 }, { "percentage", 0.7 },
        
        // 测量概念 - 中低权重
        { "测量", 0.6 }, { "长度", 0.6 }, { "重量", 0.6 }, { "时间", 0.5 },
        { "measurement", 0.6 }, { "length", 0.6 }, { "weight", 0.6 }, { "time", 0.5 }
    };

    private static readonly HashSet<string> NonMathIndicators = new(StringComparer.OrdinalIgnoreCase)
    {
        "故事", "小说", "电影", "游戏", "音乐", "体育", "历史", "地理",
        "story", "novel", "movie", "game", "music", "sports", "history", "geography",
        "生物", "化学", "物理", "文学", "艺术", "政治", "经济",
        "biology", "chemistry", "physics", "literature", "art", "politics", "economics"
    };

    public bool ContainsMathematicalConcepts(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return false;

        var mathScore = CalculateMathRelevanceScore(content);
        
        return mathScore > 0.3;
    }

    public double CalculateMathRelevanceScore(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return 0.0;

        var totalScore = 0.0;
        var matchCount = 0;

        // 检查数学关键词
        foreach (var kvp in MathConceptWeights)
        {
            if (content.Contains(kvp.Key, StringComparison.OrdinalIgnoreCase))
            {
                totalScore += kvp.Value;
                matchCount++;
            }
        }

        // 检查数字模式
        var numberMatches = Regex.Matches(content, @"\d+");
        if (numberMatches.Count > 0)
        {
            totalScore += numberMatches.Count * 0.3;
            matchCount++;
        }

        // 检查数学符号
        var mathSymbols = new[] { "+", "-", "×", "÷", "=", "%", "°" };
        foreach (var symbol in mathSymbols)
        {
            if (content.Contains(symbol))
            {
                totalScore += 0.5;
                matchCount++;
            }
        }

        // 计算相对分数
        var words = content.Split(new char[] { ' ', '，', ',', '。', '.' }, StringSplitOptions.RemoveEmptyEntries);
        return matchCount > 0 ? totalScore / Math.Max(words.Length, 1) : 0.0;
    }

    public List<string> ExtractMathKeywords(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return new List<string>();

        var keywords = new List<string>();
        
        // 检查内容中包含的数学关键词
        foreach (var keyword in MathConceptWeights.Keys)
        {
            if (content.Contains(keyword, StringComparison.OrdinalIgnoreCase) && !keywords.Contains(keyword))
            {
                keywords.Add(keyword);
            }
        }

        return keywords;
    }

    public bool IsEducationallyAppropriate(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return false;

        // 检查不当内容关键词
        var inappropriateKeywords = new[]
        {
            "暴力", "恐怖", "血腥", "violence", "horror", "bloody",
            "成人", "色情", "adult", "pornographic",
            "危险", "伤害", "dangerous", "harmful"
        };

        var lowerContent = content.ToLower();
        return !inappropriateKeywords.Any(keyword => lowerContent.Contains(keyword));
    }

    private string CleanContent(string content)
    {
        // 转换为小写并移除多余空格
        var cleaned = Regex.Replace(content.ToLower().Trim(), @"\s+", " ");
        
        // 移除标点符号但保留中文字符
        cleaned = Regex.Replace(cleaned, @"[^\u4e00-\u9fa5a-z0-9\s]", " ");
        
        return cleaned;
    }
}