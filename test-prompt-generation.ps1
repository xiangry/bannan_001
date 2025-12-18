# 测试提示词生成功能
Write-Host "=== 测试提示词生成功能 ===" -ForegroundColor Green

# 测试数据
$requestBody = @{
    mathConcept = "加法"
    options = @{
        ageGroup = 1
        panelCount = 4
        visualStyle = 0
        language = 0
        enablePinyin = $false
    }
} | ConvertTo-Json -Depth 3

Write-Host "请求体:" -ForegroundColor Cyan
Write-Host $requestBody -ForegroundColor Gray

Write-Host "`n发送请求到提示词生成端点..." -ForegroundColor Yellow

try {
    $response = Invoke-RestMethod -Uri "http://localhost:5082/api/comic/generate-prompt" -Method POST -Body $requestBody -ContentType "application/json" -TimeoutSec 60
    
    Write-Host "`n✅ 提示词生成成功!" -ForegroundColor Green
    Write-Host "生成的提示词长度: $($response.generatedPrompt.Length)" -ForegroundColor Cyan
    Write-Host "建议数量: $($response.suggestions.Count)" -ForegroundColor Cyan
    
    Write-Host "`n生成的提示词:" -ForegroundColor Cyan
    Write-Host $response.generatedPrompt -ForegroundColor White
    
    Write-Host "`n改进建议:" -ForegroundColor Cyan
    foreach ($suggestion in $response.suggestions) {
        Write-Host "- $suggestion" -ForegroundColor Yellow
    }
    
} catch {
    Write-Host "`n❌ 提示词生成失败" -ForegroundColor Red
    Write-Host "错误信息: $($_.Exception.Message)" -ForegroundColor Red
    
    if ($_.Exception.Response) {
        $statusCode = $_.Exception.Response.StatusCode
        Write-Host "状态码: $statusCode" -ForegroundColor Red
        
        try {
            $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
            $errorBody = $reader.ReadToEnd()
            Write-Host "错误详情: $errorBody" -ForegroundColor Red
        } catch {
            Write-Host "无法读取错误详情" -ForegroundColor Red
        }
    }
}