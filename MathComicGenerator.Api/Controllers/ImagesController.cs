using MathComicGenerator.Shared.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace MathComicGenerator.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ImagesController : ControllerBase
{
    private readonly IImageGenerationService _imageGenerationService;
    private readonly ILogger<ImagesController> _logger;

    public ImagesController(
        IImageGenerationService imageGenerationService,
        ILogger<ImagesController> logger)
    {
        _imageGenerationService = imageGenerationService;
        _logger = logger;
    }

    /// <summary>
    /// 获取图片文件
    /// </summary>
    /// <param name="fileName">图片文件名</param>
    /// <returns>图片文件</returns>
    [HttpGet("{fileName}")]
    public ActionResult GetImage(string fileName)
    {
        try
        {
            // 验证文件名安全性
            if (string.IsNullOrEmpty(fileName) || 
                fileName.Contains("..") || 
                fileName.Contains("/") || 
                fileName.Contains("\\"))
            {
                return BadRequest("Invalid file name");
            }

            var imagePath = _imageGenerationService.GetImagePath(fileName);
            
            if (!System.IO.File.Exists(imagePath))
            {
                _logger.LogWarning("Image not found: {FileName}", fileName);
                return NotFound();
            }

            var imageBytes = System.IO.File.ReadAllBytes(imagePath);
            var contentType = GetContentType(fileName);
            
            return File(imageBytes, contentType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error serving image: {FileName}", fileName);
            return StatusCode(500, "Error serving image");
        }
    }

    private string GetContentType(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension switch
        {
            ".png" => "image/png",
            ".jpg" => "image/jpeg",
            ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".bmp" => "image/bmp",
            ".webp" => "image/webp",
            _ => "application/octet-stream"
        };
    }
}