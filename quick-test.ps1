# 快速测试脚本
param(
    [int]$ApiPort = 7109
)

$ErrorActionPreference = "Continue"

Write-Host "=== 快速API测试 ===" -ForegroundColor Green
Write-Host ""

# 测试1: 健康检查
Write-Host "1. 测试健康检查..." -ForegroundColor Cyan
try {
    $health = Invoke-RestMethod -Uri "https://localhost:$ApiPort/api/comic/health" `
        -Method GET -SkipCertificateCheck -TimeoutSec 5
    Write-Host "✅ 健康检查通过" -ForegroundColor Green
    Write-Host ($health | ConvertTo-Json -Depth 2) -ForegroundColor Gray
} catch {
    Write-Host "❌ 健康检查失败: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Write-Host ""

# 测试2: 配置状态
Write-Host "2. 测试配置状态..." -ForegroundColor Cyan
try {
    $config = Invoke-RestMethod -Uri "https://localhost:$ApiPort/api/comic/config-status" `
        -Method GET -SkipCertificateCheck -TimeoutSec 5
    Write-Host "✅ 配置状态检查通过" -ForegroundColor Green
    $deepSeekStatus = if ($config.configuration.DeepSeekAPI.HasApiKey) { "已配置" } else { "未配置" }
    $deepSeekColor = if ($config.configuration.DeepSeekAPI.HasApiKey) { "Green" } else { "Yellow" }
    Write-Host "DeepSeek API 配置: $deepSeekStatus" -ForegroundColor $deepSeekColor
    
    $geminiStatus = if ($config.configuration.GeminiAPI.HasApiKey) { "已配置" } else { "未配置" }
    $geminiColor = if ($config.configuration.GeminiAPI.HasApiKey) { "Green" } else { "Yellow" }
    Write-Host "Gemini API 配置: $geminiStatus" -ForegroundColor $geminiColor
} catch {
    Write-Host "❌ 配置状态检查失败: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""

# 测试3: 提示词生成
Write-Host "3. 测试提示词生成..." -ForegroundColor Cyan
$testRequest = @{
    MathConcept = "加法运算"
    Options = @{
        PanelCount = 4
        AgeGroup = 1
        VisualStyle = 0
        Language = 0
    }
} | ConvertTo-Json -Depth 3

try {
    Write-Host "发送请求..." -ForegroundColor Gray
    $response = Invoke-RestMethod -Uri "https://localhost:$ApiPort/api/comic/generate-prompt" `
        -Method POST -Body $testRequest -ContentType "application/json" `
        -SkipCertificateCheck -TimeoutSec 120
    
    if ($response.Success) {
        Write-Host "✅ 提示词生成成功" -ForegroundColor Green
        Write-Host "处理时间: $($response.ProcessingTime)" -ForegroundColor Cyan
        Write-Host "提示词长度: $($response.Data.GeneratedPrompt.Length) 字符" -ForegroundColor Cyan
        Write-Host ""
        Write-Host "生成的提示词预览:" -ForegroundColor Yellow
        $preview = $response.Data.GeneratedPrompt
        if ($preview.Length -gt 200) {
            $preview = $preview.Substring(0, 200) + "..."
        }
        Write-Host $preview -ForegroundColor White
        
        if ($response.Data.Suggestions -and $response.Data.Suggestions.Count -gt 0) {
            Write-Host ""
            Write-Host "改进建议:" -ForegroundColor Yellow
            foreach ($suggestion in $response.Data.Suggestions) {
                Write-Host "  - $suggestion" -ForegroundColor Gray
            }
        }
    } else {
        Write-Host "⚠️  提示词生成返回失败状态" -ForegroundColor Yellow
    }
} catch {
    Write-Host "❌ 提示词生成失败" -ForegroundColor Red
    Write-Host "错误: $($_.Exception.Message)" -ForegroundColor Red
    
    if ($_.Exception.Response) {
        try {
            $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
            $errorBody = $reader.ReadToEnd()
            $errorObj = $errorBody | ConvertFrom-Json -ErrorAction SilentlyContinue
            if ($errorObj) {
                Write-Host "错误详情: $($errorObj.error)" -ForegroundColor Red
                if ($errorObj.details) {
                    Write-Host "解决方案:" -ForegroundColor Yellow
                    foreach ($detail in $errorObj.details) {
                        Write-Host "  - $detail" -ForegroundColor Gray
                    }
                }
            } else {
                Write-Host "错误响应: $errorBody" -ForegroundColor Red
            }
        } catch {
            Write-Host "无法解析错误详情" -ForegroundColor Red
        }
    }
}

Write-Host ""
Write-Host "=== 测试完成 ===" -ForegroundColor Green

