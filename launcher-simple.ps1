# 简化版启动器 - 更稳定可靠
param(
    [switch]$ApiOnly,
    [switch]$VueOnly
)

$ErrorActionPreference = "Continue"
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

function Write-Header {
    param([string]$Text)
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host $Text -ForegroundColor Cyan
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host ""
}

function Write-Success { param([string]$Msg) Write-Host "✅ $Msg" -ForegroundColor Green }
function Write-Error { param([string]$Msg) Write-Host "❌ $Msg" -ForegroundColor Red }
function Write-Info { param([string]$Msg) Write-Host "ℹ️  $Msg" -ForegroundColor Cyan }

Write-Header "数学漫画生成器 - 启动器"

# 检查环境
Write-Info "检查环境..."
try {
    $dotnetVer = dotnet --version 2>&1
    Write-Success ".NET 版本: $dotnetVer"
} catch {
    Write-Error "未找到 .NET SDK"
    exit 1
}

if (-not $ApiOnly) {
    try {
        $nodeVer = node --version 2>&1
        Write-Success "Node.js 版本: $nodeVer"
    } catch {
        Write-Error "未找到 Node.js"
        $VueOnly = $false
    }
}

# 清理端口
Write-Info "清理端口占用..."
$ports = @(7109, 5173)
foreach ($port in $ports) {
    $conns = Get-NetTCPConnection -LocalPort $port -ErrorAction SilentlyContinue
    foreach ($conn in $conns) {
        if ($conn.OwningProcess) {
            Stop-Process -Id $conn.OwningProcess -Force -ErrorAction SilentlyContinue
        }
    }
}
Start-Sleep -Seconds 2

# 启动API
if (-not $VueOnly) {
    Write-Header "启动 API 服务"
    Write-Info "启动中... (https://localhost:7109)"
    
    Start-Process powershell -ArgumentList @(
        "-NoExit",
        "-Command",
        "cd '$PSScriptRoot'; dotnet run --project MathComicGenerator.Api --urls https://localhost:7109"
    ) -WindowStyle Normal
    
    Write-Success "API 服务已启动（新窗口）"
    Write-Info "等待服务启动..."
    Start-Sleep -Seconds 10
    
    # 测试API
    try {
        $health = Invoke-RestMethod -Uri "https://localhost:7109/api/comic/health" `
            -Method GET -SkipCertificateCheck -TimeoutSec 5
        Write-Success "API 服务已就绪"
    } catch {
        Write-Info "API 服务可能还在启动中，请稍候..."
    }
}

# 启动Vue
if (-not $ApiOnly) {
    $vuePath = Join-Path $PSScriptRoot "MathComicGenerator.Web.Vue"
    if (Test-Path $vuePath) {
        Write-Header "启动 Vue 前端"
        Write-Info "启动中... (http://localhost:5173)"
        
        Start-Process powershell -ArgumentList @(
            "-NoExit",
            "-Command",
            "cd '$vuePath'; npm run dev"
        ) -WindowStyle Normal
        
        Write-Success "Vue 服务已启动（新窗口）"
    } else {
        Write-Error "Vue 项目目录不存在"
    }
}

Write-Header "启动完成"
Write-Success "所有服务已启动！"
Write-Host ""
Write-Host "服务地址：" -ForegroundColor Cyan
if (-not $VueOnly) {
    Write-Host "  API: https://localhost:7109" -ForegroundColor Green
}
if (-not $ApiOnly) {
    Write-Host "  Vue: http://localhost:5173" -ForegroundColor Green
}
Write-Host ""
Write-Host "提示：" -ForegroundColor Yellow
Write-Host "  - 服务运行在独立窗口中" -ForegroundColor White
Write-Host "  - 关闭窗口即可停止服务" -ForegroundColor White
Write-Host "  - 运行 quick-test.ps1 进行测试" -ForegroundColor White
Write-Host ""

