using MathComicGenerator.Shared.Models;

namespace MathComicGenerator.Shared.Interfaces;

/// <summary>
/// 提示词生成服务接口
/// </summary>
public interface IPromptGenerationService
{
    /// <summary>
    /// 根据数学概念和选项生成提示词
    /// </summary>
    /// <param name="mathConcept">数学概念</param>
    /// <param name="options">生成选项</param>
    /// <returns>生成的提示词</returns>
    Task<PromptGenerationResponse> GeneratePromptAsync(MathConcept mathConcept, GenerationOptions options);

    /// <summary>
    /// 验证提示词的有效性
    /// </summary>
    /// <param name="prompt">提示词</param>
    /// <returns>验证结果</returns>
    ValidationResult ValidatePrompt(string prompt);

    /// <summary>
    /// 优化提示词
    /// </summary>
    /// <param name="prompt">原始提示词</param>
    /// <param name="options">生成选项</param>
    /// <returns>优化后的提示词</returns>
    Task<string> OptimizePromptAsync(string prompt, GenerationOptions options);
}