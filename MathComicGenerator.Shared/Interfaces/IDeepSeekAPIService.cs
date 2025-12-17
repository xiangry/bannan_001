using MathComicGenerator.Shared.Models;

namespace MathComicGenerator.Shared.Interfaces;

/// <summary>
/// DeepSeek API服务接口
/// </summary>
public interface IDeepSeekAPIService
{
    /// <summary>
    /// 生成提示词内容
    /// </summary>
    /// <param name="systemPrompt">系统提示词</param>
    /// <param name="userPrompt">用户提示词</param>
    /// <returns>生成的提示词内容</returns>
    Task<string> GeneratePromptAsync(string systemPrompt, string userPrompt);

    /// <summary>
    /// 优化提示词
    /// </summary>
    /// <param name="originalPrompt">原始提示词</param>
    /// <param name="optimizationInstructions">优化指令</param>
    /// <returns>优化后的提示词</returns>
    Task<string> OptimizePromptAsync(string originalPrompt, string optimizationInstructions);

    /// <summary>
    /// 处理API错误
    /// </summary>
    /// <param name="error">API错误</param>
    /// <returns>错误响应</returns>
    Task<ErrorResponse> HandleAPIErrorAsync(APIError error);
}