# 测试新的两步生成功能

Write-Host "=== 测试数学漫画生成器新功能 ===" -ForegroundColor Green

# 启动API服务
Write-Host "`n启动API服务..." -ForegroundColor Yellow
$apiProcess = Start-Process -FilePath "dotnet" -ArgumentList "run --project MathComicGenerator.Api" -PassThru -WindowStyle Hidden

# 等待服务启动
Start-Sleep -Seconds 10

try {
    # 测试1: 生成提示词
    Write-Host "`n--- 测试提示词生成 ---" -ForegroundColor Cyan
    
    $promptRequest = @{
        MathConcept = "加法运算"
        Options = @{
            PanelCount = 4
            AgeGroup = "Elementary"
            VisualStyle = "Cartoon"
            Language = "Chinese"
        }
    } | ConvertTo-Json -Depth 3

    try {
        $response = Invoke-WebRequest -Uri "https://localhost:7109/api/comic/generate-prompt" -Method POST -Body $promptRequest -ContentType "application/json" -SkipCertificateCheck -TimeoutSec 15
        
        if ($response.StatusCode -eq 200) {
            Write-Host "✅ 提示词生成API正常工作" -ForegroundColor Green
            $promptData = $response.Content | ConvertFrom-Json
            Write-Host "生成的提示词ID: $($promptData.Id)" -ForegroundColor White
        } else {
            Write-Host "❌ 提示词生成API响应异常: $($response.StatusCode)" -ForegroundColor Red
        }
    } catch {
        Write-Host "❌ 提示词生成API调用失败: $($_.Exception.Message)" -ForegroundColor Red
    }

    # 测试2: 验证提示词
    Write-Host "`n--- 测试提示词验证 ---" -ForegroundColor Cyan
    
    $validateRequest = @{
        Prompt = "创建一个关于加法的4格漫画，包含可爱的角色和清晰的数学概念解释"
    } | ConvertTo-Json

    try {
        $response = Invoke-WebRequest -Uri "https://localhost:7109/api/comic/validate-prompt" -Method POST -Body $validateRequest -ContentType "application/json" -SkipCertificateCheck -TimeoutSec 10
        
        if ($response.StatusCode -eq 200) {
            Write-Host "✅ 提示词验证API正常工作" -ForegroundColor Green
            $validationData = $response.Content | ConvertFrom-Json
            Write-Host "验证结果: $($validationData.IsValid)" -ForegroundColor White
        } else {
            Write-Host "❌ 提示词验证API响应异常: $($response.StatusCode)" -ForegroundColor Red
        }
    } catch {
        Write-Host "❌ 提示词验证API调用失败: $($_.Exception.Message)" -ForegroundColor Red
    }

    # 测试3: 从提示词生成漫画
    Write-Host "`n--- 测试从提示词生成漫画 ---" -ForegroundColor Cyan
    
    $comicRequest = @{
        PromptId = "test-prompt-id"
        EditedPrompt = "创建一个关于加法运算的教育漫画，包含4个面板。第一个面板显示两个小朋友遇到加法问题，第二个面板展示他们开始思考解决方法，第三个面板显示计算过程，第四个面板展示正确答案和庆祝。使用卡通风格，色彩鲜艳，适合小学生理解。"
        Options = @{
            PanelCount = 4
            AgeGroup = "Elementary"
            VisualStyle = "Cartoon"
            Language = "Chinese"
        }
    } | ConvertTo-Json -Depth 3

    try {
        $response = Invoke-WebRequest -Uri "https://localhost:7109/api/comic/generate-from-prompt" -Method POST -Body $comicRequest -ContentType "application/json" -SkipCertificateCheck -TimeoutSec 20
        
        if ($response.StatusCode -eq 200) {
            Write-Host "✅ 从提示词生成漫画API正常工作" -ForegroundColor Green
            $comicData = $response.Content | ConvertFrom-Json
            Write-Host "生成的漫画ID: $($comicData.Id)" -ForegroundColor White
            Write-Host "漫画标题: $($comicData.Title)" -ForegroundColor White
            Write-Host "面板数量: $($comicData.Panels.Count)" -ForegroundColor White
        } else {
            Write-Host "❌ 从提示词生成漫画API响应异常: $($response.StatusCode)" -ForegroundColor Red
        }
    } catch {
        Write-Host "❌ 从提示词生成漫画API调用失败: $($_.Exception.Message)" -ForegroundColor Red
    }

    # 测试4: 检查配置状态
    Write-Host "`n--- 检查系统配置 ---" -ForegroundColor Cyan
    
    try {
        $response = Invoke-WebRequest -Uri "https://localhost:7109/api/comic/config-status" -SkipCertificateCheck -TimeoutSec 5
        
        if ($response.StatusCode -eq 200) {
            Write-Host "✅ 配置状态API正常工作" -ForegroundColor Green
            $configData = $response.Content | ConvertFrom-Json
            Write-Host "配置有效性: $($configData.isValid)" -ForegroundColor White
            Write-Host "API密钥配置: $($configData.configuration.GeminiAPI.HasApiKey)" -ForegroundColor White
        } else {
            Write-Host "❌ 配置状态API响应异常: $($response.StatusCode)" -ForegroundColor Red
        }
    } catch {
        Write-Host "❌ 配置状态API调用失败: $($_.Exception.Message)" -ForegroundColor Red
    }

} finally {
    # 清理进程
    Write-Host "`n--- 清理测试环境 ---" -ForegroundColor Cyan
    if ($apiProcess -and !$apiProcess.HasExited) {
        Stop-Process -Id $apiProcess.Id -Force
        Write-Host "✅ API进程已停止" -ForegroundColor Green
    }
}

Write-Host "`n=== 新功能测试完成 ===" -ForegroundColor Green
Write-Host "`n📋 功能总结:" -ForegroundColor Yellow
Write-Host "1. ✨ 两步生成流程: 先生成提示词，再生成漫画" -ForegroundColor White
Write-Host "2. ✏️  提示词编辑: 用户可以编辑AI生成的提示词" -ForegroundColor White
Write-Host "3. ✅ 提示词验证: 自动验证提示词的有效性" -ForegroundColor White
Write-Host "4. 🔧 提示词优化: 提供优化建议和自动优化功能" -ForegroundColor White
Write-Host "5. 🎨 灵活控制: 用户对最终生成内容有更多控制权" -ForegroundColor White

Write-Host "`n🚀 使用方法:" -ForegroundColor Yellow
Write-Host "1. 输入数学概念和选项" -ForegroundColor White
Write-Host "2. 点击'生成提示词'按钮" -ForegroundColor White
Write-Host "3. 在提示词编辑器中查看和编辑提示词" -ForegroundColor White
Write-Host "4. 点击'生成漫画图片'按钮生成最终漫画" -ForegroundColor White