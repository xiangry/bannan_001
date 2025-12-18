# 测试API密钥有效性
Write-Host "=== 测试API密钥有效性 ===" -ForegroundColor Green

# 读取配置
$config = Get-Content "MathComicGenerator.Api/appsettings.json" -Raw | ConvertFrom-Json
$apiKey = $config.GeminiAPI.ApiKey
$baseUrl = $config.GeminiAPI.BaseUrl

Write-Host "测试API: $baseUrl" -ForegroundColor Cyan

# 测试不同的端点
$endpoints = @(
    "/models",
    "/chat/completions"
)

foreach ($endpoint in $endpoints) {
    Write-Host "`n测试端点: $endpoint" -ForegroundColor Yellow
    
    $headers = @{
        "Authorization" = "Bearer $apiKey"
        "Content-Type" = "application/json"
    }
    
    try {
        if ($endpoint -eq "/models") {
            $response = Invoke-RestMethod -Uri "$baseUrl$endpoint" -Method GET -Headers $headers -TimeoutSec 10
            Write-Host "✅ GET $endpoint 成功" -ForegroundColor Green
            Write-Host "可用模型数量: $($response.data.Count)" -ForegroundColor Cyan
        } else {
            # 简单的测试请求
            $body = @{
                model = "gpt-3.5-turbo"
                messages = @(@{role = "user"; content = "Hello"})
                max_tokens = 10
            } | ConvertTo-Json -Depth 3
            
            $response = Invoke-RestMethod -Uri "$baseUrl$endpoint" -Method POST -Headers $headers -Body $body -TimeoutSec 10
            Write-Host "✅ POST $endpoint 成功" -ForegroundColor Green
        }
    } catch {
        Write-Host "❌ $endpoint 失败: $($_.Exception.Message)" -ForegroundColor Red
        
        if ($_.Exception.Response) {
            $statusCode = $_.Exception.Response.StatusCode
            Write-Host "   状态码: $statusCode" -ForegroundColor Red
        }
    }
}

# 测试DeepSeek API
Write-Host "`n=== 测试DeepSeek API ===" -ForegroundColor Green
$deepSeekKey = $config.DeepSeekAPI.ApiKey
$deepSeekUrl = $config.DeepSeekAPI.BaseUrl

$headers = @{
    "Authorization" = "Bearer $deepSeekKey"
    "Content-Type" = "application/json"
}

try {
    $body = @{
        model = "deepseek-chat"
        messages = @(@{role = "user"; content = "Hello"})
        max_tokens = 10
    } | ConvertTo-Json -Depth 3
    
    $response = Invoke-RestMethod -Uri "$deepSeekUrl/chat/completions" -Method POST -Headers $headers -Body $body -TimeoutSec 10
    Write-Host "✅ DeepSeek API 可用" -ForegroundColor Green
} catch {
    Write-Host "❌ DeepSeek API 失败: $($_.Exception.Message)" -ForegroundColor Red
}