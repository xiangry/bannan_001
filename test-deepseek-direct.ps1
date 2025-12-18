#!/usr/bin/env pwsh

# Test DeepSeek API directly
$headers = @{
    "Content-Type" = "application/json"
    "Authorization" = "Bearer sk-f3cd2cf87b194b02b0fa21d5a21e5373"
}

$body = @{
    model = "deepseek-chat"
    messages = @(
        @{
            role = "user"
            content = "Hello, can you respond with a simple greeting?"
        }
    )
    max_tokens = 100
    temperature = 0.7
} | ConvertTo-Json -Depth 10

Write-Host "Testing DeepSeek API directly..."
Write-Host "Request Body: $body"

try {
    $response = Invoke-RestMethod -Uri "https://api.deepseek.com/v1/chat/completions" -Method Post -Headers $headers -Body $body
    Write-Host "Success! Response:"
    $response | ConvertTo-Json -Depth 10
} catch {
    Write-Host "Error occurred:"
    Write-Host "Status Code: $($_.Exception.Response.StatusCode)"
    Write-Host "Status Description: $($_.Exception.Response.StatusDescription)"
    
    if ($_.Exception.Response) {
        $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
        $responseBody = $reader.ReadToEnd()
        Write-Host "Response Body: $responseBody"
    }
    
    Write-Host "Full Exception: $($_.Exception.Message)"
}