# PowerShell helper: stage and commit local changes after an automated fix
# Usage: pwsh -File .\scripts\post_fix_commit.ps1 -Message "your commit message"

param(
    [string]$Message = ""
)

# Ensure we're inside a git repository
$git = (Get-Command git -ErrorAction SilentlyContinue)
if (-not $git) {
    Write-Host "git 未找到，无法提交变更。请安装 Git 并在 PATH 中可用。" -ForegroundColor Red
    exit 1
}

$repoRoot = git rev-parse --show-toplevel 2>$null
if ($LASTEXITCODE -ne 0) {
    Write-Host "当前目录不是 git 仓库，跳过提交。" -ForegroundColor Yellow
    exit 0
}

# Stage all changes (tracked/untracked)
git add -A

# Get list of changed files for the commit message
$changed = git ls-files -m -o --exclude-standard

if (-not $changed) {
    Write-Host "没有检测到需要提交的更改。" -ForegroundColor Yellow
    exit 0
}

if ([string]::IsNullOrWhiteSpace($Message)) {
    $timestamp = (Get-Date).ToString("yyyy-MM-dd HH:mm:ss")
    $shortList = ($changed -split "\n" | Select-Object -First 6) -join ", "
    $more = if (($changed -split "\n").Count -gt 6) { ", ..." } else { "" }
    $Message = "chore(agent): applied fix to $shortList$more — $timestamp"
}

# Commit
git commit -m "$Message" --no-verify
if ($LASTEXITCODE -eq 0) {
    Write-Host "已提交: $Message" -ForegroundColor Green
} else {
    Write-Host "提交失败（可能没有实际变更），git 返回代码: $LASTEXITCODE" -ForegroundColor Red
}
