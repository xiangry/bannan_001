# 测试UI性能改进
Write-Host "=== 测试UI性能改进 ===" -ForegroundColor Green

# 测试DeepSeek API直接调用速度
Write-Host "`n1. 测试DeepSeek API直接调用速度..." -ForegroundColor Yellow

$startTime = Get-Date
try {
    $config = Get-Content "MathComicGenerator.Api/appsettings.json" -Raw | ConvertFrom-Json
    $deepSeekKey = $config.DeepSeekAPI.ApiKey
    $deepSeekUrl = $config.DeepSeekAPI.BaseUrl

    $headers = @{
        "Authorization" = "Bearer $deepSeekKey"
        "Content-Type" = "application/json"
    }

    $body = @{
        model = "deepseek-chat"
        messages = @(
            @{role = "system"; content = "你是一个专业的教育漫画提示词生成专家。"}
            @{role = "user"; content = "请为加法运算生成一个简单的漫画提示词。"}
        )
        max_tokens = 500
        temperature = 0.7
    } | ConvertTo-Json -Depth 4

    $response = Invoke-RestMethod -Uri "$deepSeekUrl/chat/completions" -Method POST -Headers $headers -Body $body -TimeoutSec 30
    $directApiTime = (Get-Date) - $startTime
    
    Write-Host "✅ DeepSeek API直接调用成功" -ForegroundColor Green
    Write-Host "⏱️  直接API调用时间: $($directApiTime.TotalMilliseconds)ms" -ForegroundColor Cyan
    Write-Host "📝 响应长度: $($response.choices[0].message.content.Length) 字符" -ForegroundColor Cyan
} catch {
    Write-Host "❌ DeepSeek API直接调用失败: $($_.Exception.Message)" -ForegroundColor Red
}

# 测试通过我们的API调用速度
Write-Host "`n2. 测试通过我们的API调用速度..." -ForegroundColor Yellow

$startTime = Get-Date
try {
    $requestData = @{
        MathConcept = "加法运算"
        Options = @{
            PanelCount = 4
            AgeGroup = 0
            VisualStyle = 0
            Language = 0
            EnablePinyin = $true
        }
    }

    $response = Invoke-RestMethod -Uri "http://localhost:5082/api/comic/generate-prompt" -Method POST -Body ($requestData | ConvertTo-Json -Depth 4) -ContentType "application/json"
    $ourApiTime = (Get-Date) - $startTime
    
    Write-Host "✅ 我们的API调用成功" -ForegroundColor Green
    Write-Host "⏱️  我们的API调用时间: $($ourApiTime.TotalMilliseconds)ms" -ForegroundColor Cyan
    Write-Host "📝 响应长度: $($response.data.generatedPrompt.Length) 字符" -ForegroundColor Cyan
    
    # 计算额外开销
    $overhead = $ourApiTime.TotalMilliseconds - $directApiTime.TotalMilliseconds
    Write-Host "📊 额外开销: ${overhead}ms" -ForegroundColor $(if ($overhead -lt 500) { "Green" } elseif ($overhead -lt 1000) { "Yellow" } else { "Red" })
    
} catch {
    Write-Host "❌ 我们的API调用失败: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`n3. 性能分析总结:" -ForegroundColor Yellow
Write-Host "- 如果直接API调用很快(< 1秒)，但我们的API很慢(> 5秒)，说明问题在我们的后端处理" -ForegroundColor White
Write-Host "- 如果两者都很快，但Web UI更新慢，说明问题在前端JavaScript日志记录" -ForegroundColor White
Write-Host "- 我们已经禁用了前端调试日志，应该能看到UI响应速度的改善" -ForegroundColor White

Write-Host "`n4. 建议测试步骤:" -ForegroundColor Yellow
Write-Host "1. 打开浏览器访问 https://localhost:5001" -ForegroundColor White
Write-Host "2. 输入一个简单的知识点，如'加法'" -ForegroundColor White
Write-Host "3. 点击'生成提示词'按钮" -ForegroundColor White
Write-Host "4. 观察从点击到UI更新的时间" -ForegroundColor White
Write-Host "5. 如果仍然很慢，可能需要进一步优化前端代码" -ForegroundColor White

Write-Host "`n=== 测试完成 ===" -ForegroundColor Green