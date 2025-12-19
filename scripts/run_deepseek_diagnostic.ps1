Set-Location 'd:\DGit\bannan_001'
# Allow self-signed certificates for local dev
[System.Net.ServicePointManager]::ServerCertificateValidationCallback = { $true }

$payload = @'
{
  "MathConcept": "2+1=3",
  "Options": { "PanelCount": 4, "AgeGroup": 0, "VisualStyle": 0, "Language": 0, "EnablePinyin": true }
}
'@

$payload | Set-Content -Path .\payload.json -Encoding UTF8

# Use the local API port used by dotnet run (http://localhost:5000) for diagnostics.
$uri = 'http://localhost:5000/api/comic/generate-prompt'

try {
    $resp = Invoke-RestMethod -Uri $uri -Method Post -Body (Get-Content .\payload.json -Raw) -ContentType 'application/json' -TimeoutSec 180 -ErrorAction Stop
    $resp | ConvertTo-Json -Depth 10 | Set-Content -Path .\response.json -Encoding UTF8
    Write-Host 'HTTP request completed successfully.'
} catch {
    Write-Host "HTTP request failed: $($_.Exception.Message)"
    # Save the exception object to response.json for inspection
    $_ | ConvertTo-Json -Depth 5 | Set-Content -Path .\response.json -Encoding UTF8
}

Write-Host '--- HTTP RESPONSE ---'
Get-Content .\response.json -Raw

Write-Host '--- LATEST deepseek-response file ---'
$latest = Get-ChildItem -Path .\MathComicGenerator.Api\logs\deepseek-response-*.json -File -ErrorAction SilentlyContinue | Sort-Object LastWriteTime -Descending | Select-Object -First 1
if ($null -ne $latest) { Write-Host $latest.FullName; Get-Content $latest.FullName -Raw } else { Write-Host 'No deepseek-response file found' }

Write-Host '--- LATEST error log ---'
$l2 = Get-ChildItem -Path .\MathComicGenerator.Api\logs\errors\*.json -File -ErrorAction SilentlyContinue | Sort-Object LastWriteTime -Descending | Select-Object -First 1
if ($null -ne $l2) { Write-Host $l2.FullName; Get-Content $l2.FullName -Raw } else { Write-Host 'No error log found' }
