using MathComicGenerator.Shared.Models;

namespace MathComicGenerator.Shared.Interfaces;

public interface IImageGenerationService
{
    /// <summary>
    /// 为单个面板生成图片
    /// </summary>
    /// <param name="panelContent">面板内容</param>
    /// <param name="options">生成选项</param>
    /// <param name="panelNumber">面板编号</param>
    /// <returns>生成的图片文件名</returns>
    Task<string> GeneratePanelImageAsync(PanelContent panelContent, GenerationOptions options, int panelNumber);

    /// <summary>
    /// 为所有面板生成图片
    /// </summary>
    /// <param name="panels">面板内容列表</param>
    /// <param name="options">生成选项</param>
    /// <returns>生成的图片文件名列表</returns>
    Task<List<string>> GenerateAllPanelImagesAsync(List<PanelContent> panels, GenerationOptions options);

    /// <summary>
    /// 获取图片URL
    /// </summary>
    /// <param name="fileName">文件名</param>
    /// <returns>图片URL</returns>
    string GetImageUrl(string fileName);

    /// <summary>
    /// 获取图片本地路径
    /// </summary>
    /// <param name="fileName">文件名</param>
    /// <returns>图片本地路径</returns>
    string GetImagePath(string fileName);
}