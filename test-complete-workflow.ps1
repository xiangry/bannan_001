#!/usr/bin/env pwsh

# Test script to verify the complete workflow

$apiUrl = "https://localhost:7110"

Write-Host "=== Testing Complete Workflow ==="

# Test 1: Generate prompt for a physics concept
Write-Host "`n1. Testing prompt generation for physics concept..."
$promptData = @{
    MathConcept = "光的折射"
    Options = @{
        AgeGroup = 1        # Elementary
        VisualStyle = 0     # Cartoon
        PanelCount = 4
        Language = 0        # Chinese
        EnablePinyin = $false
    }
}

$promptJson = $promptData | ConvertTo-Json -Depth 3

try {
    $promptResponse = Invoke-RestMethod -Uri "$apiUrl/api/comic/generate-prompt" -Method POST -Body $promptJson -ContentType "application/json" -SkipCertificateCheck
    Write-Host "✅ Prompt generation successful!"
    Write-Host "Generated prompt length: $($promptResponse.data.generatedPrompt.Length) characters"
    
    # Test 2: Generate comic from the prompt
    Write-Host "`n2. Testing comic generation from prompt..."
    $comicData = @{
        PromptId = $promptResponse.data.id
        EditedPrompt = $promptResponse.data.generatedPrompt
        Options = @{
            AgeGroup = 1        # Elementary
            VisualStyle = 0     # Cartoon
            PanelCount = 4
            Language = 0        # Chinese
            EnablePinyin = $false
        }
    }
    
    $comicJson = $comicData | ConvertTo-Json -Depth 3
    
    $comicResponse = Invoke-RestMethod -Uri "$apiUrl/api/comic/generate-from-prompt" -Method POST -Body $comicJson -ContentType "application/json" -SkipCertificateCheck
    Write-Host "✅ Comic generation successful!"
    Write-Host "Comic ID: $($comicResponse.data.id)"
    Write-Host "Comic Title: $($comicResponse.data.title)"
    Write-Host "Panel Count: $($comicResponse.data.panels.Count)"
    
    # Test 3: Save the comic
    Write-Host "`n3. Testing comic save..."
    $saveResponse = Invoke-RestMethod -Uri "$apiUrl/api/comic/save" -Method POST -Body ($comicResponse.data | ConvertTo-Json -Depth 5) -ContentType "application/json" -SkipCertificateCheck
    Write-Host "✅ Comic save successful!"
    Write-Host "Save message: $($saveResponse.data.message)"
    
    Write-Host "`n🎉 Complete workflow test PASSED!"
    Write-Host "The system successfully:"
    Write-Host "  - Accepted non-math educational content (physics)"
    Write-Host "  - Generated appropriate prompts using DeepSeek/fallback"
    Write-Host "  - Created multi-panel comic content"
    Write-Host "  - Saved the comic to storage"
    
}
catch {
    Write-Host "❌ Workflow test FAILED!"
    Write-Host "Error: $($_.Exception.Message)"
    if ($_.ErrorDetails) {
        Write-Host "Details: $($_.ErrorDetails.Message)"
    }
}

# Test 4: Test with different subjects
Write-Host "`n4. Testing with different educational subjects..."

$subjects = @(
    "古代埃及文明",
    "植物的光合作用", 
    "英语过去时态",
    "中国古诗词欣赏"
)

foreach ($subject in $subjects) {
    Write-Host "`nTesting subject: $subject"
    
    $testData = @{
        MathConcept = $subject
        Options = @{
            AgeGroup = 1
            VisualStyle = 0
            PanelCount = 4
            Language = 0
            EnablePinyin = $false
        }
    }
    
    try {
        $response = Invoke-RestMethod -Uri "$apiUrl/api/comic/generate-prompt" -Method POST -Body ($testData | ConvertTo-Json -Depth 3) -ContentType "application/json" -SkipCertificateCheck
        Write-Host "  ✅ $subject - Prompt generated successfully"
    }
    catch {
        Write-Host "  ❌ $subject - Failed: $($_.Exception.Message)"
    }
}

Write-Host "`n=== Workflow Test Complete ==="