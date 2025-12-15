namespace MathComicGenerator.Shared.Models;

public class GenerationOptions
{
    public int PanelCount { get; set; } = 4; // 默认4个面板，范围3-6
    public AgeGroup AgeGroup { get; set; } = AgeGroup.Elementary;
    public VisualStyle VisualStyle { get; set; } = VisualStyle.Cartoon;
    public Language Language { get; set; } = Language.Chinese;
}