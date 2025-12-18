#!/usr/bin/env pwsh

Write-Host "=== 简单API测试 ===" -ForegroundColor Green

$apiUrl = "http://localhost:5082"

# 测试基本连接
Write-Host "`n🔍 测试API基本连接..."
try {
    $configResponse = Invoke-RestMethod -Uri "$apiUrl/api/comic/config-status" -Method GET
    Write-Host "✅ API基本连接成功" -ForegroundColor Green
    Write-Host "配置状态: $($configResponse | ConvertTo-Json)" -ForegroundColor Gray
} catch {
    Write-Host "❌ API基本连接失败: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# 测试提示词生成
Write-Host "`n🎯 测试提示词生成..."
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
Write-Host "请求数据: $requestJson" -ForegroundColor Yellow

try {
    $response = Invoke-RestMethod -Uri "$apiUrl/api/comic/generate-prompt" -Method POST -Body $requestJson -ContentType "application/json"
    Write-Host "✅ 提示词生成成功" -ForegroundColor Green
    Write-Host "响应: $($response | ConvertTo-Json -Depth 3)" -ForegroundColor White
} catch {
    Write-Host "❌ 提示词生成失败" -ForegroundColor Red
    Write-Host "错误信息: $($_.Exception.Message)" -ForegroundColor Red
    
    # 尝试获取详细错误信息
    if ($_.Exception.Response) {
        try {
            $errorStream = $_.Exception.Response.GetResponseStream()
            $reader = New-Object System.IO.StreamReader($errorStream)
            $errorBody = $reader.ReadToEnd()
            Write-Host "详细错误: $errorBody" -ForegroundColor Red
        } catch {
            Write-Host "无法读取详细错误信息" -ForegroundColor Red
        }
    }
}