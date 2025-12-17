# 测试提示词生成API

Write-Host "=== 测试提示词生成API ===" -ForegroundColor Green

# 测试数据 - 使用枚举数值
$promptRequest = @{
    MathConcept = "加法运算"
    Options = @{
        PanelCount = 4
        AgeGroup = 1      # Elementary
        VisualStyle = 0   # Cartoon
        Language = 0      # Chinese
    }
} | ConvertTo-Json -Depth 3

Write-Host "请求数据:" -ForegroundColor Yellow
Write-Host $promptRequest -ForegroundColor White

try {
    Write-Host "`n发送请求到 https://localhost:7109/api/comic/generate-prompt" -ForegroundColor Cyan
    
    $response = Invoke-WebRequest -Uri "https://localhost:7109/api/comic/generate-prompt" -Method POST -Body $promptRequest -ContentType "application/json" -SkipCertificateCheck -TimeoutSec 30
    
    Write-Host "`n响应状态码: $($response.StatusCode)" -ForegroundColor Green
    Write-Host "响应内容:" -ForegroundColor Yellow
    
    $responseData = $response.Content | ConvertFrom-Json
    Write-Host ($responseData | ConvertTo-Json -Depth 5) -ForegroundColor White
    
} catch {
    Write-Host "`n❌ 请求失败:" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    
    if ($_.Exception.Response) {
        Write-Host "HTTP状态码: $($_.Exception.Response.StatusCode)" -ForegroundColor Red
        
        # 尝试获取详细错误信息
        try {
            $errorResponse = $_.Exception.Response
            $stream = $errorResponse.GetResponseStream()
            $reader = New-Object System.IO.StreamReader($stream)
            $errorText = $reader.ReadToEnd()
            Write-Host "错误详情: $errorText" -ForegroundColor Red
            $reader.Close()
            $stream.Close()
        } catch {
            Write-Host "无法读取错误详情: $($_.Exception.Message)" -ForegroundColor Red
        }
    }
    
    # 尝试使用PowerShell的错误记录
    if ($_.ErrorDetails) {
        Write-Host "PowerShell错误详情: $($_.ErrorDetails.Message)" -ForegroundColor Red
    }
}