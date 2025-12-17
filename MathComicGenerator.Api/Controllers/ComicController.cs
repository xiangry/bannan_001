using MathComicGenerator.Api.Services;
using MathComicGenerator.Shared.Interfaces;
using MathComicGenerator.Shared.Models;
using MathComicGenerator.Shared.Services;
using Microsoft.AspNetCore.Mvc;

namespace MathComicGenerator.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ComicController : ControllerBase
{
    private readonly IComicGenerationService _comicGenerationService;
    private readonly IPromptGenerationService _promptGenerationService;
    private readonly IStorageService _storageService;
    private readonly ResourceManagementService _resourceManagement;
    private readonly ErrorLoggingService _errorLogging;
    private readonly ILogger<ComicController> _logger;

    public ComicController(
        IComicGenerationService comicGenerationService,
        IPromptGenerationService promptGenerationService,
        IStorageService storageService,
        ResourceManagementService resourceManagement,
        ErrorLoggingService errorLogging,
        ILogger<ComicController> logger)
    {
        _comicGenerationService = comicGenerationService;
        _promptGenerationService = promptGenerationService;
        _storageService = storageService;
        _resourceManagement = resourceManagement;
        _errorLogging = errorLogging;
        _logger = logger;
    }

    /// <summary>
    /// 生成数学漫画
    /// </summary>
    /// <param name="request">漫画生成请求</param>
    /// <returns>生成的漫画</returns>
    [HttpPost("generate")]
    public async Task<ActionResult<MultiPanelComic>> GenerateComic([FromBody] ComicGenerationRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            // 检查资源可用性
            await _resourceManagement.TryAcquireResourceAsync();

            _logger.LogInformation("Generating comic for concept: {Concept}", request.MathConcept);

            // 验证输入
            var validationResult = _comicGenerationService.ValidateConcept(request.MathConcept);
            if (!validationResult.IsValid)
            {
                return BadRequest(new { error = validationResult.ErrorMessage, suggestions = validationResult.Suggestions });
            }

            // 创建数学概念对象
            var validator = new MathConceptValidator();
            var mathConcept = validator.ParseMathConcept(request.MathConcept);

            // 生成漫画
            var comic = await _comicGenerationService.GenerateComicAsync(mathConcept, request.Options);

            // 保存漫画
            var comicId = await _storageService.SaveComicAsync(comic);

            _logger.LogInformation("Comic generated and saved successfully: {ComicId}", comicId);

            return Ok(comic);
        }
        catch (ResourceLimitException ex)
        {
            _logger.LogWarning(ex, "Resource limit exceeded for comic generation");
            return StatusCode(429, new { error = "系统繁忙，请稍后重试", canRetry = true });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid input for comic generation");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            await _errorLogging.LogErrorAsync(ex, "Comic generation failed", new Dictionary<string, object>
            {
                { "concept", request.MathConcept },
                { "options", request.Options }
            });
            
            _logger.LogError(ex, "Unexpected error during comic generation");
            return StatusCode(500, new { error = "生成漫画时发生错误，请稍后重试" });
        }
        finally
        {
            _resourceManagement.ReleaseResource();
        }
    }

    /// <summary>
    /// 获取漫画详情
    /// </summary>
    /// <param name="id">漫画ID</param>
    /// <returns>漫画详情</returns>
    [HttpGet("{id}")]
    public async Task<ActionResult<MultiPanelComic>> GetComic(string id)
    {
        try
        {
            _logger.LogInformation("Retrieving comic: {ComicId}", id);

            var comic = await _storageService.LoadComicAsync(id);
            if (comic == null)
            {
                return NotFound(new { error = "漫画不存在" });
            }

            return Ok(comic);
        }
        catch (Exception ex)
        {
            await _errorLogging.LogErrorAsync(ex, "Failed to retrieve comic", new Dictionary<string, object>
            {
                { "comicId", id }
            });
            
            _logger.LogError(ex, "Error retrieving comic: {ComicId}", id);
            return StatusCode(500, new { error = "获取漫画时发生错误" });
        }
    }

    /// <summary>
    /// 获取漫画列表
    /// </summary>
    /// <returns>漫画元数据列表</returns>
    [HttpGet]
    public async Task<ActionResult<List<ComicMetadata>>> GetComics()
    {
        try
        {
            _logger.LogInformation("Retrieving comics list");

            var comics = await _storageService.ListComicsAsync();
            return Ok(comics);
        }
        catch (Exception ex)
        {
            await _errorLogging.LogErrorAsync(ex, "Failed to retrieve comics list");
            
            _logger.LogError(ex, "Error retrieving comics list");
            return StatusCode(500, new { error = "获取漫画列表时发生错误" });
        }
    }

    /// <summary>
    /// 删除漫画
    /// </summary>
    /// <param name="id">漫画ID</param>
    /// <returns>删除结果</returns>
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteComic(string id)
    {
        try
        {
            _logger.LogInformation("Deleting comic: {ComicId}", id);

            var deleted = await _storageService.DeleteComicAsync(id);
            if (!deleted)
            {
                return NotFound(new { error = "漫画不存在" });
            }

            return Ok(new { message = "漫画删除成功" });
        }
        catch (Exception ex)
        {
            await _errorLogging.LogErrorAsync(ex, "Failed to delete comic", new Dictionary<string, object>
            {
                { "comicId", id }
            });
            
            _logger.LogError(ex, "Error deleting comic: {ComicId}", id);
            return StatusCode(500, new { error = "删除漫画时发生错误" });
        }
    }

    /// <summary>
    /// 导出漫画
    /// </summary>
    /// <param name="id">漫画ID</param>
    /// <param name="format">导出格式</param>
    /// <returns>导出的文件</returns>
    [HttpGet("{id}/export")]
    public async Task<ActionResult> ExportComic(string id, [FromQuery] ExportFormat format = ExportFormat.JSON)
    {
        try
        {
            _logger.LogInformation("Exporting comic: {ComicId} in format: {Format}", id, format);

            var exportData = await _storageService.ExportComicAsync(id, format);
            
            var contentType = format switch
            {
                ExportFormat.JSON => "application/json",
                ExportFormat.PDF => "application/pdf",
                ExportFormat.ZIP => "application/zip",
                _ => "application/octet-stream"
            };

            var fileName = $"comic_{id}.{format.ToString().ToLower()}";
            
            return File(exportData, contentType, fileName);
        }
        catch (StorageException ex) when (ex.Message.Contains("not found"))
        {
            return NotFound(new { error = "漫画不存在" });
        }
        catch (Exception ex)
        {
            await _errorLogging.LogErrorAsync(ex, "Failed to export comic", new Dictionary<string, object>
            {
                { "comicId", id },
                { "format", format.ToString() }
            });
            
            _logger.LogError(ex, "Error exporting comic: {ComicId}", id);
            return StatusCode(500, new { error = "导出漫画时发生错误" });
        }
    }

    /// <summary>
    /// 获取系统健康状态
    /// </summary>
    /// <returns>系统健康状态</returns>
    [HttpGet("health")]
    public ActionResult<SystemHealthStatus> GetHealth()
    {
        try
        {
            var health = _resourceManagement.GetSystemHealth();
            return Ok(health);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting system health");
            return StatusCode(500, new { error = "获取系统状态时发生错误" });
        }
    }

    /// <summary>
    /// 获取配置状态
    /// </summary>
    /// <returns>配置状态信息</returns>
    [HttpGet("config-status")]
    public ActionResult GetConfigStatus([FromServices] ConfigurationValidationService configValidator)
    {
        try
        {
            var isValid = configValidator.ValidateConfiguration();
            var summary = configValidator.GetConfigurationSummary();
            
            return Ok(new
            {
                isValid = isValid,
                configuration = summary,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting configuration status");
            return StatusCode(500, new { error = "获取配置状态时发生错误" });
        }
    }

    /// <summary>
    /// 获取统计信息
    /// </summary>
    /// <returns>系统统计信息</returns>
    [HttpGet("statistics")]
    public async Task<ActionResult<ComicStatistics>> GetStatistics()
    {
        try
        {
            _logger.LogInformation("Retrieving system statistics");

            var statistics = await _storageService.GetStatisticsAsync();
            return Ok(statistics);
        }
        catch (Exception ex)
        {
            await _errorLogging.LogErrorAsync(ex, "Failed to retrieve statistics");
            
            _logger.LogError(ex, "Error retrieving statistics");
            return StatusCode(500, new { error = "获取统计信息时发生错误" });
        }
    }

    /// <summary>
    /// 生成提示词
    /// </summary>
    /// <param name="request">提示词生成请求</param>
    /// <returns>生成的提示词</returns>
    [HttpPost("generate-prompt")]
    public async Task<ActionResult<PromptGenerationResponse>> GeneratePrompt([FromBody] PromptGenerationRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            // 检查资源可用性
            await _resourceManagement.TryAcquireResourceAsync();

            _logger.LogInformation("Generating prompt for concept: {Concept}", request.MathConcept);

            // 验证输入
            var validationResult = _comicGenerationService.ValidateConcept(request.MathConcept);
            if (!validationResult.IsValid)
            {
                return BadRequest(new { error = validationResult.ErrorMessage, suggestions = validationResult.Suggestions });
            }

            // 创建数学概念对象
            var validator = new MathConceptValidator();
            var mathConcept = validator.ParseMathConcept(request.MathConcept);

            // 生成提示词
            var promptResponse = await _promptGenerationService.GeneratePromptAsync(mathConcept, request.Options);

            _logger.LogInformation("Prompt generated successfully: {PromptId}", promptResponse.Id);

            return Ok(promptResponse);
        }
        catch (ResourceLimitException ex)
        {
            _logger.LogWarning(ex, "Resource limit exceeded for prompt generation");
            return StatusCode(429, new { error = "系统繁忙，请稍后重试", canRetry = true });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid input for prompt generation");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            await _errorLogging.LogErrorAsync(ex, "Prompt generation failed", new Dictionary<string, object>
            {
                { "concept", request.MathConcept },
                { "options", request.Options }
            });
            
            _logger.LogError(ex, "Unexpected error during prompt generation");
            return StatusCode(500, new { error = "生成提示词时发生错误，请稍后重试" });
        }
        finally
        {
            _resourceManagement.ReleaseResource();
        }
    }

    /// <summary>
    /// 根据提示词生成漫画图片
    /// </summary>
    /// <param name="request">漫画图片生成请求</param>
    /// <returns>生成的漫画</returns>
    [HttpPost("generate-from-prompt")]
    public async Task<ActionResult<MultiPanelComic>> GenerateComicFromPrompt([FromBody] ComicImageGenerationRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            // 检查资源可用性
            await _resourceManagement.TryAcquireResourceAsync();

            _logger.LogInformation("Generating comic from prompt: {PromptId}", request.PromptId);

            // 验证提示词
            var promptValidation = _promptGenerationService.ValidatePrompt(request.EditedPrompt);
            if (!promptValidation.IsValid)
            {
                return BadRequest(new { error = promptValidation.ErrorMessage, suggestions = promptValidation.Suggestions });
            }

            // 使用提示词生成漫画
            var comic = await _comicGenerationService.GenerateComicFromPromptAsync(request.EditedPrompt, request.Options);

            // 保存漫画
            var comicId = await _storageService.SaveComicAsync(comic);

            _logger.LogInformation("Comic generated from prompt and saved successfully: {ComicId}", comicId);

            return Ok(comic);
        }
        catch (ResourceLimitException ex)
        {
            _logger.LogWarning(ex, "Resource limit exceeded for comic generation from prompt");
            return StatusCode(429, new { error = "系统繁忙，请稍后重试", canRetry = true });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid prompt for comic generation");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            await _errorLogging.LogErrorAsync(ex, "Comic generation from prompt failed", new Dictionary<string, object>
            {
                { "promptId", request.PromptId },
                { "prompt", request.EditedPrompt },
                { "options", request.Options }
            });
            
            _logger.LogError(ex, "Unexpected error during comic generation from prompt");
            return StatusCode(500, new { error = "根据提示词生成漫画时发生错误，请稍后重试" });
        }
        finally
        {
            _resourceManagement.ReleaseResource();
        }
    }

    /// <summary>
    /// 验证提示词
    /// </summary>
    /// <param name="request">提示词验证请求</param>
    /// <returns>验证结果</returns>
    [HttpPost("validate-prompt")]
    public ActionResult<ValidationResult> ValidatePrompt([FromBody] PromptValidationRequest request)
    {
        try
        {
            _logger.LogInformation("Validating prompt");

            var result = _promptGenerationService.ValidatePrompt(request.Prompt);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating prompt");
            return StatusCode(500, new { error = "验证提示词时发生错误" });
        }
    }

    /// <summary>
    /// 优化提示词
    /// </summary>
    /// <param name="request">提示词优化请求</param>
    /// <returns>优化后的提示词</returns>
    [HttpPost("optimize-prompt")]
    public async Task<ActionResult<string>> OptimizePrompt([FromBody] PromptOptimizationRequest request)
    {
        try
        {
            _logger.LogInformation("Optimizing prompt");

            var optimizedPrompt = await _promptGenerationService.OptimizePromptAsync(request.Prompt, request.Options);
            return Ok(new { optimizedPrompt });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error optimizing prompt");
            return StatusCode(500, new { error = "优化提示词时发生错误" });
        }
    }

    /// <summary>
    /// 保存漫画
    /// </summary>
    /// <param name="comic">要保存的漫画</param>
    /// <returns>保存结果</returns>
    [HttpPost("save")]
    public async Task<ActionResult<string>> SaveComic([FromBody] MultiPanelComic comic)
    {
        try
        {
            _logger.LogInformation("Saving comic: {ComicId}", comic.Id);

            var comicId = await _storageService.SaveComicAsync(comic);
            
            return Ok(new { 
                message = "漫画保存成功", 
                comicId = comicId,
                success = true 
            });
        }
        catch (Exception ex)
        {
            await _errorLogging.LogErrorAsync(ex, "Failed to save comic", new Dictionary<string, object>
            {
                { "comicId", comic.Id },
                { "title", comic.Title }
            });
            
            _logger.LogError(ex, "Error saving comic: {ComicId}", comic.Id);
            return StatusCode(500, new { error = "保存漫画时发生错误" });
        }
    }

    /// <summary>
    /// 验证数学概念
    /// </summary>
    /// <param name="request">验证请求</param>
    /// <returns>验证结果</returns>
    [HttpPost("validate")]
    public ActionResult<ValidationResult> ValidateConcept([FromBody] ConceptValidationRequest request)
    {
        try
        {
            _logger.LogInformation("Validating concept: {Concept}", request.Concept);

            var result = _comicGenerationService.ValidateConcept(request.Concept);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating concept");
            return StatusCode(500, new { error = "验证概念时发生错误" });
        }
    }

    /// <summary>
    /// 触发系统恢复
    /// </summary>
    /// <returns>恢复结果</returns>
    [HttpPost("system/recovery")]
    public async Task<ActionResult> TriggerSystemRecovery()
    {
        try
        {
            _logger.LogInformation("Manual system recovery triggered");

            var recoveryResult = await _resourceManagement.AttemptSystemRecoveryAsync();
            
            if (recoveryResult)
            {
                return Ok(new { 
                    message = "系统恢复成功", 
                    success = true,
                    timestamp = DateTime.UtcNow 
                });
            }
            else
            {
                return StatusCode(503, new { 
                    message = "系统恢复失败，请稍后重试", 
                    success = false,
                    timestamp = DateTime.UtcNow 
                });
            }
        }
        catch (Exception ex)
        {
            await _errorLogging.LogErrorAsync(ex, "Manual system recovery failed");
            
            _logger.LogError(ex, "Error during manual system recovery");
            return StatusCode(500, new { error = "系统恢复过程中发生错误" });
        }
    }

    /// <summary>
    /// 获取错误日志统计
    /// </summary>
    /// <param name="hours">统计时间范围（小时）</param>
    /// <returns>错误统计信息</returns>
    [HttpGet("system/errors")]
    public async Task<ActionResult<ErrorStatistics>> GetErrorStatistics([FromQuery] int hours = 24)
    {
        try
        {
            _logger.LogInformation("Retrieving error statistics for last {Hours} hours", hours);

            var period = TimeSpan.FromHours(hours);
            var statistics = await _errorLogging.GetErrorStatisticsAsync(period);
            
            return Ok(statistics);
        }
        catch (Exception ex)
        {
            await _errorLogging.LogErrorAsync(ex, "Failed to retrieve error statistics");
            
            _logger.LogError(ex, "Error retrieving error statistics");
            return StatusCode(500, new { error = "获取错误统计时发生错误" });
        }
    }
}

// 请求模型
public class ComicGenerationRequest
{
    public string MathConcept { get; set; } = string.Empty;
    public GenerationOptions Options { get; set; } = new();
}

public class ConceptValidationRequest
{
    public string Concept { get; set; } = string.Empty;
}

public class PromptValidationRequest
{
    public string Prompt { get; set; } = string.Empty;
}

public class PromptOptimizationRequest
{
    public string Prompt { get; set; } = string.Empty;
    public GenerationOptions Options { get; set; } = new();
}