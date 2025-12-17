using MathComicGenerator.Shared.Interfaces;
using MathComicGenerator.Shared.Models;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text;

namespace MathComicGenerator.Api.Services;

public class ImageGenerationService : IImageGenerationService
{
    private readonly ILogger<ImageGenerationService> _logger;
    private readonly string _imagesBasePath;

    public ImageGenerationService(ILogger<ImageGenerationService> logger, IConfiguration configuration)
    {
        _logger = logger;
        var storageBasePath = configuration["Storage:BasePath"] ?? 
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MathComicGenerator");
        _imagesBasePath = Path.Combine(storageBasePath, "images");
        
        Directory.CreateDirectory(_imagesBasePath);
    }

    public async Task<string> GeneratePanelImageAsync(PanelContent panelContent, GenerationOptions options, int panelNumber)
    {
        try
        {
            _logger.LogInformation("Generating image for panel {PanelNumber}", panelNumber);

            // 创建图片文件名
            var fileName = $"panel_{panelNumber}_{Guid.NewGuid():N}.png";
            var filePath = Path.Combine(_imagesBasePath, fileName);

            // 生成简单的漫画面板图片
            await CreateComicPanelImageAsync(panelContent, options, filePath);

            _logger.LogInformation("Panel image generated successfully: {FileName}", fileName);
            return fileName;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating panel image for panel {PanelNumber}", panelNumber);
            throw;
        }
    }

    public async Task<List<string>> GenerateAllPanelImagesAsync(List<PanelContent> panels, GenerationOptions options)
    {
        var imageFiles = new List<string>();

        for (int i = 0; i < panels.Count; i++)
        {
            var imageFile = await GeneratePanelImageAsync(panels[i], options, i + 1);
            imageFiles.Add(imageFile);
        }

        return imageFiles;
    }

    public string GetImageUrl(string fileName)
    {
        return $"/api/images/{fileName}";
    }

    public string GetImagePath(string fileName)
    {
        return Path.Combine(_imagesBasePath, fileName);
    }

    private async Task CreateComicPanelImageAsync(PanelContent panelContent, GenerationOptions options, string filePath)
    {
        // 创建一个简单的漫画面板图片
        const int width = 400;
        const int height = 300;

        using var bitmap = new Bitmap(width, height);
        using var graphics = Graphics.FromImage(bitmap);

        // 设置背景色
        var backgroundColor = GetBackgroundColor(options.VisualStyle);
        graphics.Clear(backgroundColor);

        // 绘制边框
        using var borderPen = new Pen(Color.Black, 3);
        graphics.DrawRectangle(borderPen, 0, 0, width - 1, height - 1);

        // 绘制场景描述区域
        var sceneRect = new Rectangle(10, 10, width - 20, height - 100);
        DrawSceneDescription(graphics, panelContent.ImageDescription, sceneRect, options);

        // 绘制对话区域
        if (panelContent.Dialogue.Any())
        {
            var dialogueRect = new Rectangle(10, height - 90, width - 20, 80);
            DrawDialogue(graphics, panelContent.Dialogue, dialogueRect, options);
        }

        // 保存图片
        bitmap.Save(filePath, System.Drawing.Imaging.ImageFormat.Png);
    }

    private Color GetBackgroundColor(VisualStyle style)
    {
        return style switch
        {
            VisualStyle.Cartoon => Color.LightBlue,
            VisualStyle.Colorful => Color.LightYellow,
            VisualStyle.Realistic => Color.White,
            VisualStyle.Minimalist => Color.WhiteSmoke,
            _ => Color.White
        };
    }

    private void DrawSceneDescription(Graphics graphics, string description, Rectangle rect, GenerationOptions options)
    {
        // 绘制场景背景
        using var sceneBrush = new SolidBrush(Color.FromArgb(240, 248, 255));
        graphics.FillRectangle(sceneBrush, rect);

        // 绘制场景边框
        using var scenePen = new Pen(Color.Gray, 1);
        graphics.DrawRectangle(scenePen, rect);

        // 绘制简单的场景元素
        DrawSimpleScene(graphics, description, rect, options);

        // 添加场景描述文字
        using var font = new Font("Microsoft YaHei", 10, FontStyle.Regular);
        using var textBrush = new SolidBrush(Color.DarkBlue);
        
        var textRect = new Rectangle(rect.X + 5, rect.Y + rect.Height - 25, rect.Width - 10, 20);
        var stringFormat = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center
        };
        
        graphics.DrawString($"场景: {TruncateText(description, 30)}", font, textBrush, textRect, stringFormat);
    }

    private void DrawSimpleScene(Graphics graphics, string description, Rectangle rect, GenerationOptions options)
    {
        // 根据描述绘制简单的场景元素
        var centerX = rect.X + rect.Width / 2;
        var centerY = rect.Y + rect.Height / 2;

        // 绘制简单的角色或物体
        if (description.Contains("老师") || description.Contains("teacher"))
        {
            DrawTeacher(graphics, centerX - 30, centerY - 20);
        }
        else if (description.Contains("学生") || description.Contains("小朋友") || description.Contains("child"))
        {
            DrawStudent(graphics, centerX - 20, centerY - 15);
        }
        else if (description.Contains("苹果") || description.Contains("apple"))
        {
            DrawApple(graphics, centerX - 15, centerY - 15);
        }
        else if (description.Contains("黑板") || description.Contains("board"))
        {
            DrawBlackboard(graphics, centerX - 40, centerY - 30);
        }
        else
        {
            // 默认绘制一个简单的图形
            DrawDefaultShape(graphics, centerX - 25, centerY - 25);
        }
    }

    private void DrawTeacher(Graphics graphics, int x, int y)
    {
        // 绘制简单的老师图形
        using var brush = new SolidBrush(Color.Brown);
        using var pen = new Pen(Color.Black, 2);
        
        // 头部
        graphics.FillEllipse(brush, x + 15, y, 30, 30);
        graphics.DrawEllipse(pen, x + 15, y, 30, 30);
        
        // 身体
        graphics.FillRectangle(brush, x + 20, y + 30, 20, 40);
        graphics.DrawRectangle(pen, x + 20, y + 30, 20, 40);
        
        // 手臂
        graphics.DrawLine(pen, x + 10, y + 40, x + 20, y + 35);
        graphics.DrawLine(pen, x + 40, y + 35, x + 50, y + 40);
    }

    private void DrawStudent(Graphics graphics, int x, int y)
    {
        // 绘制简单的学生图形
        using var brush = new SolidBrush(Color.Pink);
        using var pen = new Pen(Color.Black, 2);
        
        // 头部
        graphics.FillEllipse(brush, x + 10, y, 20, 20);
        graphics.DrawEllipse(pen, x + 10, y, 20, 20);
        
        // 身体
        graphics.FillRectangle(brush, x + 12, y + 20, 16, 25);
        graphics.DrawRectangle(pen, x + 12, y + 20, 16, 25);
    }

    private void DrawApple(Graphics graphics, int x, int y)
    {
        // 绘制苹果
        using var brush = new SolidBrush(Color.Red);
        using var pen = new Pen(Color.DarkRed, 2);
        
        graphics.FillEllipse(brush, x, y, 30, 30);
        graphics.DrawEllipse(pen, x, y, 30, 30);
        
        // 苹果柄
        using var stemPen = new Pen(Color.Brown, 3);
        graphics.DrawLine(stemPen, x + 15, y, x + 15, y - 5);
    }

    private void DrawBlackboard(Graphics graphics, int x, int y)
    {
        // 绘制黑板
        using var brush = new SolidBrush(Color.DarkGreen);
        using var pen = new Pen(Color.Black, 2);
        
        graphics.FillRectangle(brush, x, y, 80, 60);
        graphics.DrawRectangle(pen, x, y, 80, 60);
        
        // 黑板上的数学公式
        using var font = new Font("Arial", 12, FontStyle.Bold);
        using var textBrush = new SolidBrush(Color.White);
        graphics.DrawString("1+1=2", font, textBrush, x + 20, y + 20);
    }

    private void DrawDefaultShape(Graphics graphics, int x, int y)
    {
        // 绘制默认的几何图形
        using var brush = new SolidBrush(Color.LightBlue);
        using var pen = new Pen(Color.Blue, 2);
        
        graphics.FillEllipse(brush, x, y, 50, 50);
        graphics.DrawEllipse(pen, x, y, 50, 50);
        
        // 添加一个笑脸
        using var facePen = new Pen(Color.DarkBlue, 2);
        // 眼睛
        graphics.DrawEllipse(facePen, x + 15, y + 15, 5, 5);
        graphics.DrawEllipse(facePen, x + 30, y + 15, 5, 5);
        // 嘴巴
        graphics.DrawArc(facePen, x + 15, y + 25, 20, 15, 0, 180);
    }

    private void DrawDialogue(Graphics graphics, List<string> dialogue, Rectangle rect, GenerationOptions options)
    {
        // 绘制对话框背景
        using var dialogueBrush = new SolidBrush(Color.White);
        graphics.FillRectangle(dialogueBrush, rect);

        // 绘制对话框边框
        using var dialoguePen = new Pen(Color.Black, 2);
        graphics.DrawRectangle(dialoguePen, rect);

        // 绘制对话内容
        using var font = new Font("Microsoft YaHei", 9, FontStyle.Regular);
        using var textBrush = new SolidBrush(Color.Black);
        
        var combinedDialogue = string.Join(" ", dialogue);
        var truncatedDialogue = TruncateText(combinedDialogue, 50);
        
        var stringFormat = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center
        };
        
        graphics.DrawString(truncatedDialogue, font, textBrush, rect, stringFormat);
    }

    private string TruncateText(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
            return text;
        
        return text.Substring(0, maxLength - 3) + "...";
    }
}