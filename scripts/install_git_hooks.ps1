<#
Install git hooks from .githooks into .git/hooks for local development.
Usage: pwsh -File .\scripts\install_git_hooks.ps1
#>

$repoRoot = git rev-parse --show-toplevel 2>$null
if ($LASTEXITCODE -ne 0) {
    Write-Host "当前目录不是 git 仓库，无法安装钩子。" -ForegroundColor Red
    exit 1
}

$hooksDir = Join-Path $repoRoot '.git\hooks'
$githooks = Join-Path $repoRoot '.githooks'

if (-not (Test-Path $githooks)) {
    Write-Host "未找到 .githooks 目录，跳过钩子安装。" -ForegroundColor Yellow
    exit 0
}

Get-ChildItem -Path $githooks -File | ForEach-Object {
    $target = Join-Path $hooksDir $_.Name
    Copy-Item -Path $_.FullName -Destination $target -Force
    # Ensure executable on Windows Subsystem / Git Bash
    if (Test-Path $target) {
        # Quote the permission string so PowerShell doesn't try to evaluate (RX) as an expression
        icacls $target /grant "Everyone:(RX)" | Out-Null
    }
    Write-Host "Installed hook: $($_.Name)" -ForegroundColor Green
}

Write-Host "Git hooks installed. 注意：本地运行仍然不会 push 变更，提交保留在本地。" -ForegroundColor Cyan
