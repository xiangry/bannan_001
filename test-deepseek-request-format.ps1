#!/usr/bin/env pwsh

Write-Host "=== 测试DeepSeek请求格式 ===" -ForegroundColor Green

$apiUrl = "http://localhost:5082"

# 测试提示词生成并查看控制台输出
Write-Host "`n🎯 发送测试请求..."
$requestData = @{
    MathConcept = "加法运算"
    Options = @{
        PanelCount = 4
        AgeGroup = 1
        VisualStyle = 0
        Language = 0
        EnablePinyin = $true
    }
}

$requestJson = $requestData | ConvertTo-Json -Depth 3
Write-Host "前端请求数据: $requestJson" -ForegroundColor Yellow

try {
    Write-Host "`n📤 发送请求到API..."
    $response = Invoke-RestMethod -Uri "$apiUrl/api/comic/generate-prompt" -Method POST -Body $requestJson -ContentType "application/json" -TimeoutSec 120
    
    Write-Host "✅ 请求成功" -ForegroundColor Green
    Write-Host "请查看API控制台输出，确认发送给DeepSeek的数据格式" -ForegroundColor Cyan
    
} catch {
    Write-Host "❌ 请求失败: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`n💡 请检查API服务的控制台输出，查看'=== DeepSeek API Request ==='部分" -ForegroundColor Yellow