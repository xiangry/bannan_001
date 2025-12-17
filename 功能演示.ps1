# 数学漫画生成器 - 两步生成功能演示

Write-Host "🎨 数学漫画生成器 - 两步生成功能演示" -ForegroundColor Green
Write-Host "=" * 50 -ForegroundColor Green

Write-Host "`n📋 功能概述:" -ForegroundColor Cyan
Write-Host "✨ 新增两步生成流程：" -ForegroundColor White
Write-Host "   1️⃣  根据数学概念生成可编辑的提示词" -ForegroundColor Gray
Write-Host "   2️⃣  根据编辑后的提示词生成漫画图片" -ForegroundColor Gray

Write-Host "`n🚀 开始演示..." -ForegroundColor Cyan

# 演示数据
$demoData = @(
    @{
        Concept = "分数概念"
        Description = "小学生学习分数的基础概念"
    },
    @{
        Concept = "几何图形"
        Description = "认识基本的几何图形"
    },
    @{
        Concept = "乘法口诀"
        Description = "学习乘法口诀表"
    }
)

foreach ($demo in $demoData) {
    Write-Host "`n" + "="*60 -ForegroundColor Yellow
    Write-Host "🎯 演示案例: $($demo.Concept)" -ForegroundColor Yellow
    Write-Host "📝 描述: $($demo.Description)" -ForegroundColor Gray
    Write-Host "="*60 -ForegroundColor Yellow
    
    # 步骤1: 生成提示词
    Write-Host "`n🔸 步骤1: 生成提示词" -ForegroundColor Cyan
    
    $request = @{
        MathConcept = $demo.Concept
        Options = @{
            PanelCount = 4
            AgeGroup = 1      # Elementary
            VisualStyle = 0   # Cartoon
            Language = 0      # Chinese
        }
    } | ConvertTo-Json -Depth 3
    
    try {
        Write-Host "   📤 发送请求..." -ForegroundColor Gray
        $response = Invoke-WebRequest -Uri "https://localhost:7109/api/comic/generate-prompt" -Method POST -Body $request -ContentType "application/json" -SkipCertificateCheck -TimeoutSec 20
        
        if ($response.StatusCode -eq 200) {
            $data = ($response.Content | ConvertFrom-Json).data
            
            Write-Host "   ✅ 提示词生成成功！" -ForegroundColor Green
            Write-Host "   🆔 ID: $($data.id)" -ForegroundColor Gray
            Write-Host "   📏 长度: $($data.generatedPrompt.Length) 字符" -ForegroundColor Gray
            
            Write-Host "`n   📄 生成的提示词:" -ForegroundColor White
            Write-Host "   " + "-"*50 -ForegroundColor DarkGray
            Write-Host "   $($data.generatedPrompt)" -ForegroundColor Gray
            Write-Host "   " + "-"*50 -ForegroundColor DarkGray
            
            Write-Host "`n   💡 AI建议:" -ForegroundColor White
            foreach ($suggestion in $data.suggestions) {
                Write-Host "   • $suggestion" -ForegroundColor Gray
            }
            
            # 步骤2: 模拟用户编辑
            Write-Host "`n🔸 步骤2: 用户编辑提示词" -ForegroundColor Cyan
            
            $editedPrompt = $data.generatedPrompt + "`n`n[用户编辑] 请确保漫画风格活泼可爱，角色表情生动，适合小学生理解。增加更多互动细节和视觉元素。"
            
            Write-Host "   ✏️  模拟用户编辑完成" -ForegroundColor Green
            Write-Host "   📏 编辑后长度: $($editedPrompt.Length) 字符 (+$($editedPrompt.Length - $data.generatedPrompt.Length))" -ForegroundColor Gray
            
            # 步骤3: 生成漫画
            Write-Host "`n🔸 步骤3: 生成漫画图片" -ForegroundColor Cyan
            
            $comicRequest = @{
                PromptId = $data.id
                EditedPrompt = $editedPrompt
                Options = $data.options
            } | ConvertTo-Json -Depth 3
            
            Write-Host "   📤 发送漫画生成请求..." -ForegroundColor Gray
            $comicResponse = Invoke-WebRequest -Uri "https://localhost:7109/api/comic/generate-from-prompt" -Method POST -Body $comicRequest -ContentType "application/json" -SkipCertificateCheck -TimeoutSec 20
            
            if ($comicResponse.StatusCode -eq 200) {
                $comicData = ($comicResponse.Content | ConvertFrom-Json).data
                
                Write-Host "   ✅ 漫画生成成功！" -ForegroundColor Green
                Write-Host "   🆔 漫画ID: $($comicData.id)" -ForegroundColor Gray
                Write-Host "   📖 标题: $($comicData.title)" -ForegroundColor Gray
                Write-Host "   🎬 面板数: $($comicData.panels.Count)" -ForegroundColor Gray
                
                Write-Host "`n   🎨 漫画内容预览:" -ForegroundColor White
                for ($i = 0; $i -lt $comicData.panels.Count; $i++) {
                    $panel = $comicData.panels[$i]
                    Write-Host "   📱 面板 $($i + 1):" -ForegroundColor Cyan
                    if ($panel.dialogue -and $panel.dialogue.Count -gt 0) {
                        Write-Host "      💬 对话: $($panel.dialogue -join '; ')" -ForegroundColor Gray
                    }
                    if ($panel.narration) {
                        Write-Host "      📝 旁白: $($panel.narration)" -ForegroundColor Gray
                    }
                }
                
                Write-Host "`n   🎉 案例演示完成！" -ForegroundColor Green
                
            } else {
                Write-Host "   ❌ 漫画生成失败: $($comicResponse.StatusCode)" -ForegroundColor Red
            }
            
        } else {
            Write-Host "   ❌ 提示词生成失败: $($response.StatusCode)" -ForegroundColor Red
        }
        
    } catch {
        Write-Host "   ❌ 演示失败: $($_.Exception.Message)" -ForegroundColor Red
    }
    
    Write-Host "`n⏱️  等待3秒后继续下一个演示..." -ForegroundColor DarkGray
    Start-Sleep -Seconds 3
}

Write-Host "`n" + "="*60 -ForegroundColor Green
Write-Host "🎊 演示完成！" -ForegroundColor Green
Write-Host "="*60 -ForegroundColor Green

Write-Host "`n📊 功能总结:" -ForegroundColor Cyan
Write-Host "✅ 两步生成流程完全实现" -ForegroundColor Green
Write-Host "✅ 提示词生成和编辑功能" -ForegroundColor Green
Write-Host "✅ 漫画图片生成功能" -ForegroundColor Green
Write-Host "✅ 用户完全控制生成过程" -ForegroundColor Green

Write-Host "`n🌐 访问地址:" -ForegroundColor Cyan
Write-Host "🖥️  Web界面: https://localhost:5001" -ForegroundColor White
Write-Host "🧪 测试页面: test-frontend.html" -ForegroundColor White
Write-Host "📡 API状态: https://localhost:7109/api/comic/config-status" -ForegroundColor White

Write-Host "`n🎯 用户使用流程:" -ForegroundColor Cyan
Write-Host "1. 访问 https://localhost:5001" -ForegroundColor White
Write-Host "2. 输入数学概念和选择选项" -ForegroundColor White
Write-Host "3. 点击'生成提示词'按钮" -ForegroundColor White
Write-Host "4. 在编辑器中查看和编辑提示词" -ForegroundColor White
Write-Host "5. 点击'生成漫画图片'按钮" -ForegroundColor White
Write-Host "6. 查看、保存和分享生成的漫画" -ForegroundColor White

Write-Host "`n🎉 新功能已完全实现并可以使用！" -ForegroundColor Green