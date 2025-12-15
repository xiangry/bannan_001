namespace MathComicGenerator.Shared.Models;

/// <summary>
/// 提示词生成请求
/// </summary>
public class PromptGenerationRequest
{
    public string MathConcept { get; set; } = string.Empty;
    public GenerationOptions Options { get; set; } = new();
}

/// <summary>
/// 提示词生成响应
/// </summary>
public class PromptGenerationResponse
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string MathConcept { get; set; } = string.Empty;
    public string GeneratedPrompt { get; set; } = string.Empty;
    public GenerationOptions Options { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<string> Suggestions { get; set; } = new();
}

/// <summary>
/// 漫画图片生成请求
/// </summary>
public class ComicImageGenerationRequest
{
    public string PromptId { get; set; } = string.Empty;
    public string EditedPrompt { get; set; } = string.Empty;
    public GenerationOptions Options { get; set; } = new();
}