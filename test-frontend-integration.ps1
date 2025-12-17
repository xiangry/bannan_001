# 测试前端集成

Write-Host "=== 测试前端集成 ===" -ForegroundColor Green

# 测试Web服务是否运行
Write-Host "`n检查Web服务状态..." -ForegroundColor Cyan
try {
    $webResponse = Invoke-WebRequest -Uri "http://localhost:5000" -TimeoutSec 5
    if ($webResponse.StatusCode -eq 200) {
        Write-Host "✅ Web服务正常运行 (端口5000)" -ForegroundColor Green
    }
} catch {
    Write-Host "❌ Web服务无法访问: $($_.Exception.Message)" -ForegroundColor Red
}

# 测试API服务是否运行
Write-Host "`n检查API服务状态..." -ForegroundColor Cyan
try {
    $apiResponse = Invoke-WebRequest -Uri "https://localhost:7109/api/comic/config-status" -SkipCertificateCheck -TimeoutSec 5
    if ($apiResponse.StatusCode -eq 200) {
        Write-Host "✅ API服务正常运行 (端口7109)" -ForegroundColor Green
        
        $configData = ($apiResponse.Content | ConvertFrom-Json).data
        Write-Host "配置状态: $($configData.isValid ? '有效' : '无效')" -ForegroundColor White
        Write-Host "API密钥: $($configData.configuration.GeminiAPI.hasApiKey ? '已配置' : '未配置')" -ForegroundColor White
    }
} catch {
    Write-Host "❌ API服务无法访问: $($_.Exception.Message)" -ForegroundColor Red
}

# 测试提示词生成API
Write-Host "`n测试提示词生成API..." -ForegroundColor Cyan
$promptRequest = @{
    MathConcept = "除法运算"
    Options = @{
        PanelCount = 4
        AgeGroup = 1      # Elementary
        VisualStyle = 0   # Cartoon
        Language = 0      # Chinese
    }
} | ConvertTo-Json -Depth 3

try {
    $promptResponse = Invoke-WebRequest -Uri "https://localhost:7109/api/comic/generate-prompt" -Method POST -Body $promptRequest -ContentType "application/json" -SkipCertificateCheck -TimeoutSec 30
    
    if ($promptResponse.StatusCode -eq 200) {
        Write-Host "✅ 提示词生成API正常工作" -ForegroundColor Green
        
        $promptData = ($promptResponse.Content | ConvertFrom-Json).data
        Write-Host "生成的提示词ID: $($promptData.id)" -ForegroundColor White
        Write-Host "数学概念: $($promptData.mathConcept)" -ForegroundColor White
        Write-Host "提示词长度: $($promptData.generatedPrompt.Length) 字符" -ForegroundColor White
        
        # 测试从提示词生成漫画
        Write-Host "`n测试从提示词生成漫画..." -ForegroundColor Cyan
        
        $comicRequest = @{
            PromptId = $promptData.id
            EditedPrompt = $promptData.generatedPrompt + "`n`n[测试编辑] 请确保漫画内容生动有趣。"
            Options = $promptData.options
        } | ConvertTo-Json -Depth 3
        
        $comicResponse = Invoke-WebRequest -Uri "https://localhost:7109/api/comic/generate-from-prompt" -Method POST -Body $comicRequest -ContentType "application/json" -SkipCertificateCheck -TimeoutSec 30
        
        if ($comicResponse.StatusCode -eq 200) {
            Write-Host "✅ 漫画生成API正常工作" -ForegroundColor Green
            
            $comicData = ($comicResponse.Content | ConvertFrom-Json).data
            Write-Host "生成的漫画ID: $($comicData.id)" -ForegroundColor White
            Write-Host "漫画标题: $($comicData.title)" -ForegroundColor White
            Write-Host "面板数量: $($comicData.panels.Count)" -ForegroundColor White
        } else {
            Write-Host "❌ 漫画生成失败: $($comicResponse.StatusCode)" -ForegroundColor Red
        }
        
    } else {
        Write-Host "❌ 提示词生成失败: $($promptResponse.StatusCode)" -ForegroundColor Red
    }
} catch {
    Write-Host "❌ API测试失败: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`n=== 集成测试完成 ===" -ForegroundColor Green

Write-Host "`n🌐 访问地址:" -ForegroundColor Yellow
Write-Host "Web界面: http://localhost:5000" -ForegroundColor White
Write-Host "测试页面: 打开 test-frontend.html 文件" -ForegroundColor White

Write-Host "`n📝 使用说明:" -ForegroundColor Yellow
Write-Host "1. 访问 http://localhost:5000" -ForegroundColor White
Write-Host "2. 输入数学概念（如：分数概念）" -ForegroundColor White
Write-Host "3. 选择年龄组和其他选项" -ForegroundColor White
Write-Host "4. 点击'生成提示词'按钮" -ForegroundColor White
Write-Host "5. 等待页面切换到提示词编辑界面" -ForegroundColor White
Write-Host "6. 编辑提示词后点击'生成漫画图片'" -ForegroundColor White

Write-Host "`n🔧 故障排除:" -ForegroundColor Yellow
Write-Host "- 如果看不到提示词编辑器，请检查浏览器控制台是否有错误" -ForegroundColor White
Write-Host "- 如果按钮无响应，请检查网络连接和API服务状态" -ForegroundColor White
Write-Host "- 可以使用 test-frontend.html 作为备用测试界面" -ForegroundColor White