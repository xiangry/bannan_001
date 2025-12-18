# 调试DeepSeek API响应格式
Write-Host "=== 调试DeepSeek API响应格式 ===" -ForegroundColor Green

$config = Get-Content "MathComicGenerator.Api/appsettings.json" -Raw | ConvertFrom-Json
$deepSeekKey = $config.DeepSeekAPI.ApiKey
$deepSeekUrl = $config.DeepSeekAPI.BaseUrl

$headers = @{
    "Authorization" = "Bearer $deepSeekKey"
    "Content-Type" = "application/json"
}

$systemPrompt = @"
你是一个专业的教育漫画提示词生成专家。你的任务是根据知识点生成详细的漫画创作提示词。

生成的提示词应该包含以下要素：
1. 漫画整体结构和面板布局
2. 主要角色设计和特征
3. 每个面板的具体场景描述
4. 对话内容和教育要点
5. 视觉风格和色彩方案

目标年龄组: 小学及以上 (6岁以上) - 适合基础知识概念学习，可以包含更多细节
视觉风格: 卡通风格 - 可爱、夸张的角色设计
面板数量: 4个面板
语言: 中文

请确保生成的提示词：
- 适合目标年龄组的理解水平
- 包含准确的知识概念
- 具有教育价值和趣味性
- 描述清晰，便于图像生成

请按以下格式返回：
提示词: [详细的漫画创作提示词]

改进建议:
- [建议1]
- [建议2]
- [建议3]
"@

$userPrompt = @"
请为以下知识点生成漫画创作提示词：

知识点: 加法

请生成一个详细的漫画创作提示词，包含完整的故事情节、角色设计和视觉描述。
同时提供3-5个改进建议，帮助用户优化提示词。
"@

$body = @{
    model = "deepseek-chat"
    messages = @(
        @{role = "system"; content = $systemPrompt}
        @{role = "user"; content = $userPrompt}
    )
    max_tokens = 1000
    temperature = 0.7
} | ConvertTo-Json -Depth 4

Write-Host "发送请求到DeepSeek API..." -ForegroundColor Yellow

try {
    $response = Invoke-RestMethod -Uri "$deepSeekUrl/chat/completions" -Method POST -Headers $headers -Body $body -TimeoutSec 30
    
    Write-Host "`n✅ DeepSeek API调用成功" -ForegroundColor Green
    $content = $response.choices[0].message.content
    Write-Host "响应内容长度: $($content.Length)" -ForegroundColor Cyan
    
    Write-Host "`n=== DeepSeek 原始响应 ===" -ForegroundColor Cyan
    Write-Host $content -ForegroundColor White
    
    Write-Host "`n=== 分析响应结构 ===" -ForegroundColor Cyan
    $lines = $content.Split("`n", [StringSplitOptions]::RemoveEmptyEntries)
    Write-Host "总行数: $($lines.Count)" -ForegroundColor Yellow
    
    for ($i = 0; $i -lt [Math]::Min(10, $lines.Count); $i++) {
        $line = $lines[$i].Trim()
        Write-Host "行 $($i+1): $line" -ForegroundColor Gray
    }
    
} catch {
    Write-Host "`n❌ DeepSeek API调用失败" -ForegroundColor Red
    Write-Host "错误信息: $($_.Exception.Message)" -ForegroundColor Red
}