using MathComicGenerator.Shared.Interfaces;
using MathComicGenerator.Shared.Models;

namespace MathComicGenerator.Shared.Services;

public class GenerationOptionsProcessor : IGenerationOptionsProcessor
{
    public ValidationResult ValidateOptions(GenerationOptions options)
    {
        if (options == null)
        {
            return new ValidationResult
            {
                IsValid = false,
                ErrorMessage = "生成选项不能为空",
                Suggestions = new List<string> { "请提供有效的生成选项" }
            };
        }

        // 验证面板数量
        if (options.PanelCount < 3 || options.PanelCount > 6)
        {
            return new ValidationResult
            {
                IsValid = false,
                ErrorMessage = "面板数量必须在3-6之间",
                Suggestions = new List<string> { "建议使用4个面板", "3-6个面板适合儿童阅读" }
            };
        }

        // 验证年龄组
        if (!Enum.IsDefined(typeof(AgeGroup), options.AgeGroup))
        {
            return new ValidationResult
            {
                IsValid = false,
                ErrorMessage = "无效的年龄组设置",
                Suggestions = new List<string> { "请选择有效的年龄组" }
            };
        }

        // 验证视觉风格
        if (!Enum.IsDefined(typeof(VisualStyle), options.VisualStyle))
        {
            return new ValidationResult
            {
                IsValid = false,
                ErrorMessage = "无效的视觉风格设置",
                Suggestions = new List<string> { "请选择有效的视觉风格" }
            };
        }

        // 验证语言设置
        if (!Enum.IsDefined(typeof(Language), options.Language))
        {
            return new ValidationResult
            {
                IsValid = false,
                ErrorMessage = "无效的语言设置",
                Suggestions = new List<string> { "请选择有效的语言" }
            };
        }

        return new ValidationResult
        {
            IsValid = true,
            ErrorMessage = string.Empty,
            Suggestions = new List<string>()
        };
    }

    public GenerationOptions ApplyDefaults(GenerationOptions? options)
    {
        if (options == null)
        {
            return new GenerationOptions
            {
                PanelCount = 4,
                AgeGroup = AgeGroup.Elementary,
                VisualStyle = VisualStyle.Cartoon,
                Language = Language.Chinese
            };
        }

        // 应用默认值到无效的属性
        var result = new GenerationOptions
        {
            PanelCount = options.PanelCount >= 3 && options.PanelCount <= 6 ? options.PanelCount : 4,
            AgeGroup = Enum.IsDefined(typeof(AgeGroup), options.AgeGroup) ? options.AgeGroup : AgeGroup.Elementary,
            VisualStyle = Enum.IsDefined(typeof(VisualStyle), options.VisualStyle) ? options.VisualStyle : VisualStyle.Cartoon,
            Language = Enum.IsDefined(typeof(Language), options.Language) ? options.Language : Language.Chinese
        };

        return result;
    }

    public GenerationOptions AdjustForAgeGroup(GenerationOptions options, AgeGroup ageGroup)
    {
        var adjusted = new GenerationOptions
        {
            PanelCount = options.PanelCount,
            AgeGroup = ageGroup,
            VisualStyle = options.VisualStyle,
            Language = options.Language
        };

        // 根据年龄组调整面板数量
        adjusted.PanelCount = ageGroup switch
        {
            AgeGroup.Preschool => Math.Min(options.PanelCount, 4), // 学龄前儿童最多4个面板
            AgeGroup.Elementary => options.PanelCount, // 小学生可以处理标准数量
            AgeGroup.MiddleSchool => options.PanelCount, // 中学生可以处理标准数量
            AgeGroup.HighSchool => Math.Max(options.PanelCount, 4), // 高中生至少4个面板
            _ => options.PanelCount
        };

        // 根据年龄组调整视觉风格
        if (ageGroup == AgeGroup.Preschool && options.VisualStyle == VisualStyle.Realistic)
        {
            adjusted.VisualStyle = VisualStyle.Cartoon; // 学龄前儿童更适合卡通风格
        }

        return adjusted;
    }

    public bool AreOptionsConsistent(GenerationOptions options)
    {
        if (options == null)
            return false;

        // 检查年龄组和面板数量的一致性
        var isConsistent = options.AgeGroup switch
        {
            AgeGroup.Preschool => options.PanelCount <= 4, // 学龄前儿童不应超过4个面板
            AgeGroup.Elementary => options.PanelCount >= 3 && options.PanelCount <= 6,
            AgeGroup.MiddleSchool => options.PanelCount >= 3 && options.PanelCount <= 6,
            AgeGroup.HighSchool => options.PanelCount >= 4 && options.PanelCount <= 6,
            _ => false
        };

        // 检查年龄组和视觉风格的一致性
        if (options.AgeGroup == AgeGroup.Preschool && options.VisualStyle == VisualStyle.Realistic)
        {
            isConsistent = false; // 学龄前儿童不适合现实主义风格
        }

        return isConsistent;
    }
}