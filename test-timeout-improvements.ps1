#!/usr/bin/env pwsh

Write-Host "=== 测试超时配置改进 ===" -ForegroundColor Green

# 检查配置文件中的超时设置
Write-Host "`n1. 检查配置文件中的超时设置..." -ForegroundColor Yellow
$configPath = "MathComicGenerator.Api/appsettings.json"
if (Test-Path $configPath) {
    $config = Get-Content $configPath | ConvertFrom-Json
    Write-Host "Gemini API 超时设置: $($config.GeminiAPI.TimeoutSeconds) 秒" -ForegroundColor Cyan
    Write-Host "DeepSeek API 超时设置: $($config.DeepSeekAPI.TimeoutSeconds) 秒" -ForegroundColor Cyan
    Write-Host "资源管理超时设置: $($config.ResourceManagement.RequestTimeoutMs) 毫秒" -ForegroundColor Cyan
} else {
    Write-Host "配置文件不存在: $configPath" -ForegroundColor Red
}

# 构建项目
Write-Host "`n2. 构建项目..." -ForegroundColor Yellow
dotnet build --configuration Debug
if ($LASTEXITCODE -ne 0) {
    Write-Host "构建失败!" -ForegroundColor Red
    exit 1
}

# 启动API服务器（后台）
Write-Host "`n3. 启动API服务器..." -ForegroundColor Yellow
$apiProcess = Start-Process -FilePath "dotnet" -ArgumentList "run --project MathComicGenerator.Api" -PassThru -WindowStyle Hidden

# 等待服务器启动
Write-Host "等待API服务器启动..." -ForegroundColor Yellow
Start-Sleep -Seconds 10

try {
    # 测试API连接
    Write-Host "`n4. 测试API连接..." -ForegroundColor Yellow
    $healthUrl = "https://localhost:7109/health"
    try {
        $response = Invoke-RestMethod -Uri $healthUrl -Method GET -SkipCertificateCheck
        Write-Host "API服务器健康检查通过" -ForegroundColor Green
    } catch {
        Write-Host "API服务器健康检查失败: $($_.Exception.Message)" -ForegroundColor Red
    }

    # 测试漫画生成（带超时监控）
    Write-Host "`n5. 测试漫画生成（监控超时）..." -ForegroundColor Yellow
    $generateUrl = "https://localhost:7109/api/comic/generate"
    $requestBody = @{
        mathConcept = @{
            topic = "加法运算"
            difficulty = "Elementary"
            ageGroup = "Age6To8"
            keywords = @("加法", "数字", "计算")
        }
        options = @{
            panelCount = 4
            ageGroup = "Age6To8"
            visualStyle = "Cartoon"
            language = "Chinese"
        }
    } | ConvertTo-Json -Depth 3

    $startTime = Get-Date
    Write-Host "开始时间: $($startTime.ToString('HH:mm:ss.fff'))" -ForegroundColor Cyan
    
    try {
        $response = Invoke-RestMethod -Uri $generateUrl -Method POST -Body $requestBody -ContentType "application/json" -SkipCertificateCheck -TimeoutSec 150
        $endTime = Get-Date
        $duration = ($endTime - $startTime).TotalMilliseconds
        
        Write-Host "请求成功完成!" -ForegroundColor Green
        Write-Host "结束时间: $($endTime.ToString('HH:mm:ss.fff'))" -ForegroundColor Cyan
        Write-Host "总耗时: $([math]::Round($duration, 2)) 毫秒" -ForegroundColor Cyan
        
        if ($response.success) {
            Write-Host "漫画生成成功，标题: $($response.data.title)" -ForegroundColor Green
            Write-Host "面板数量: $($response.data.panels.Count)" -ForegroundColor Green
        } else {
            Write-Host "漫画生成失败: $($response.message)" -ForegroundColor Red
            if ($response.error) {
                Write-Host "错误详情: $($response.error)" -ForegroundColor Red
            }
        }
    } catch {
        $endTime = Get-Date
        $duration = ($endTime - $startTime).TotalMilliseconds
        
        Write-Host "请求失败!" -ForegroundColor Red
        Write-Host "结束时间: $($endTime.ToString('HH:mm:ss.fff'))" -ForegroundColor Cyan
        Write-Host "失败耗时: $([math]::Round($duration, 2)) 毫秒" -ForegroundColor Cyan
        Write-Host "错误信息: $($_.Exception.Message)" -ForegroundColor Red
        
        # 检查是否是超时错误
        if ($_.Exception.Message -like "*timeout*" -or $_.Exception.Message -like "*超时*") {
            Write-Host "检测到超时错误，这可能表明配置生效" -ForegroundColor Yellow
        }
    }

    # 检查日志文件
    Write-Host "`n6. 检查日志文件..." -ForegroundColor Yellow
    $logPath = "MathComicGenerator.Api/logs"
    if (Test-Path $logPath) {
        $logFiles = Get-ChildItem $logPath -Filter "*.log" | Sort-Object LastWriteTime -Descending | Select-Object -First 1
        if ($logFiles) {
            Write-Host "最新日志文件: $($logFiles.Name)" -ForegroundColor Cyan
            $logContent = Get-Content $logFiles.FullName -Tail 20
            Write-Host "最近20行日志:" -ForegroundColor Cyan
            $logContent | ForEach-Object { Write-Host "  $_" -ForegroundColor Gray }
        }
    }

} finally {
    # 清理：停止API服务器
    Write-Host "`n7. 清理资源..." -ForegroundColor Yellow
    if ($apiProcess -and !$apiProcess.HasExited) {
        Write-Host "停止API服务器..." -ForegroundColor Yellow
        $apiProcess.Kill()
        $apiProcess.WaitForExit(5000)
    }
}

Write-Host "`n=== 测试完成 ===" -ForegroundColor Green