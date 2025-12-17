namespace MathComicGenerator.Shared.Models;

public class GenerationOptions
{
    public int PanelCount { get; set; } = 4; // 默认4个面板，范围3-6
    public AgeGroup AgeGroup { get; set; } = AgeGroup.Preschool; // 默认5-6岁
    public VisualStyle VisualStyle { get; set; } = VisualStyle.Cartoon;
    public Language Language { get; set; } = Language.Chinese;
    public bool EnablePinyin { get; set; } = true; // 默认开启拼音
}