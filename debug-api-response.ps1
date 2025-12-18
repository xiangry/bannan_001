# 调试API响应格式
Write-Host "=== 调试Gemini API响应格式 ===" -ForegroundColor Green

# 读取配置
$config = Get-Content "MathComicGenerator.Api/appsettings.json" -Raw | ConvertFrom-Json
$apiKey = $config.GeminiAPI.ApiKey
$baseUrl = $config.GeminiAPI.BaseUrl

Write-Host "API基础URL: $baseUrl" -ForegroundColor Cyan
Write-Host "API密钥长度: $($apiKey.Length)" -ForegroundColor Cyan

# 构造请求
$headers = @{
    "Authorization" = "Bearer $apiKey"
    "Content-Type" = "application/json"
}

$body = @{
    model = "gpt-3.5-turbo"
    messages = @(
        @{
            role = "user"
            content = "请生成一个简单的数学漫画提示词，主题是加法，适合6-8岁儿童。"
        }
    )
    max_tokens = 500
    temperature = 0.7
} | ConvertTo-Json -Depth 3

Write-Host "`n发送请求到: $baseUrl/chat/completions" -ForegroundColor Yellow
Write-Host "请求体: $body" -ForegroundColor Gray

try {
    $response = Invoke-RestMethod -Uri "$baseUrl/chat/completions" -Method POST -Headers $headers -Body $body -TimeoutSec 30
    Write-Host "`n✅ API调用成功" -ForegroundColor Green
    Write-Host "响应内容:" -ForegroundColor Cyan
    $response | ConvertTo-Json -Depth 5
} catch {
    Write-Host "`n❌ API调用失败" -ForegroundColor Red
    Write-Host "错误信息: $($_.Exception.Message)" -ForegroundColor Red
    
    if ($_.Exception.Response) {
        $statusCode = $_.Exception.Response.StatusCode
        Write-Host "状态码: $statusCode" -ForegroundColor Red
        
        try {
            $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
            $errorBody = $reader.ReadToEnd()
            Write-Host "错误响应体: $errorBody" -ForegroundColor Red
        } catch {
            Write-Host "无法读取错误响应体" -ForegroundColor Red
        }
    }
}