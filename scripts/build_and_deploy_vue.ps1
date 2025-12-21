# Build Vue app and copy built assets into MathComicGenerator.Web/wwwroot
# Usage: pwsh -NoProfile -File .\scripts\build_and_deploy_vue.ps1

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Definition
$repoRoot = Resolve-Path (Join-Path $scriptDir "..")
$vueDir = Join-Path $repoRoot "MathComicGenerator.Web.Vue"
$wwwroot = Join-Path $repoRoot "MathComicGenerator.Web\wwwroot"

Write-Host "Vue dir: $vueDir"
Write-Host "Target wwwroot: $wwwroot"

if (-not (Test-Path $vueDir)) { Write-Error "Vue project not found at $vueDir"; exit 1 }

Push-Location $vueDir

Write-Host "Installing npm packages..."
# Run npm install
npm install
if ($LASTEXITCODE -ne 0) { Write-Error "npm install failed"; Exit 2 }

Write-Host "Building Vue app (npm run build)..."
npm run build
if ($LASTEXITCODE -ne 0) { Write-Error "npm run build failed"; Exit 3 }

$distPath = Join-Path $vueDir "dist"
if (-not (Test-Path $distPath)) { Write-Error "dist folder not found after build: $distPath"; Exit 4 }

# Ensure target wwwroot exists
if (-not (Test-Path $wwwroot)) {
    New-Item -ItemType Directory -Path $wwwroot -Force | Out-Null
}

Write-Host "Cleaning target wwwroot (preserving logs folder if present)..."
Get-ChildItem -Path $wwwroot -Force | Where-Object { $_.Name -ne 'logs' } | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue

Write-Host "Copying built files to wwwroot..."
Copy-Item -Path (Join-Path $distPath '*') -Destination $wwwroot -Recurse -Force

Write-Host "Done. Built assets copied to $wwwroot"

Pop-Location

Write-Host "Next steps:"
Write-Host "1) Restart the ASP.NET host that serves the Web site (MathComicGenerator.Web). e.g.:"
Write-Host "   cd MathComicGenerator.Web; dotnet run"
Write-Host "2) Open http://localhost:5000 or https://localhost:5001 depending on your launch settings. If using HTTPS, accept the dev certificate if prompted."
