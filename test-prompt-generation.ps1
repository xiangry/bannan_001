#!/usr/bin/env pwsh

# Test script to reproduce the prompt generation issue

$apiUrl = "https://localhost:7110"

# Test data - using integer values for enums
$testData = @{
    MathConcept = "光的折射"
    Options = @{
        AgeGroup = 1        # Elementary = 1
        VisualStyle = 0     # Cartoon = 0
        PanelCount = 4
        Language = 0        # Chinese = 0
        EnablePinyin = $false
    }
}

$jsonBody = $testData | ConvertTo-Json -Depth 3
Write-Host "Testing prompt generation with data:"
Write-Host $jsonBody

try {
    # Test the generate-prompt endpoint
    $response = Invoke-RestMethod -Uri "$apiUrl/api/comic/generate-prompt" -Method POST -Body $jsonBody -ContentType "application/json" -SkipCertificateCheck
    Write-Host "SUCCESS: Prompt generation worked!"
    Write-Host "Response: $($response | ConvertTo-Json -Depth 3)"
}
catch {
    Write-Host "ERROR: Prompt generation failed!"
    Write-Host "Status Code: $($_.Exception.Response.StatusCode)"
    Write-Host "Error Message: $($_.Exception.Message)"
    
    # Get the response content using WebException
    if ($_.Exception -is [System.Net.WebException]) {
        $responseStream = $_.Exception.Response.GetResponseStream()
        $reader = New-Object System.IO.StreamReader($responseStream)
        $responseBody = $reader.ReadToEnd()
        Write-Host "Response Body: $responseBody"
    }
    elseif ($_.ErrorDetails) {
        Write-Host "Error Details: $($_.ErrorDetails.Message)"
    }
}

# Also test validation endpoint
Write-Host "`n--- Testing validation endpoint ---"
$validationData = @{
    Concept = "光的折射"
}

$validationJson = $validationData | ConvertTo-Json
Write-Host "Testing validation with: $validationJson"

try {
    $validationResponse = Invoke-RestMethod -Uri "$apiUrl/api/comic/validate" -Method POST -Body $validationJson -ContentType "application/json" -SkipCertificateCheck
    Write-Host "Validation Response: $($validationResponse | ConvertTo-Json -Depth 3)"
}
catch {
    Write-Host "Validation ERROR: $($_.Exception.Message)"
    if ($_.Exception.Response) {
        $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
        $responseBody = $reader.ReadToEnd()
        Write-Host "Validation Response Body: $responseBody"
    }
}