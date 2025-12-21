# 数学漫画生成器 - 完整启动器
# 功能：启动服务、监控日志、自动测试

param(
    [switch]$SkipBuild,
    [switch]$SkipTest,
    [switch]$ApiOnly,
    [switch]$VueOnly,
    [int]$ApiPort = 7109,
    [int]$VuePort = 5173
)

$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"

# 设置控制台编码为UTF-8
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
chcp 65001 | Out-Null

# 颜色输出函数
function Write-ColorOutput {
    param(
        [string]$Message,
        [string]$Color = "White"
    )
    Write-Host $Message -ForegroundColor $Color
}

function Write-Section {
    param([string]$Title)
    Write-Host ""
    Write-ColorOutput "========================================" "Cyan"
    Write-ColorOutput $Title "Cyan"
    Write-ColorOutput "========================================" "Cyan"
    Write-Host ""
}

function Write-Success { param([string]$Msg) Write-ColorOutput "✅ $Msg" "Green" }
function Write-Error { param([string]$Msg) Write-ColorOutput "❌ $Msg" "Red" }
function Write-Warning { param([string]$Msg) Write-ColorOutput "⚠️  $Msg" "Yellow" }
function Write-Info { param([string]$Msg) Write-ColorOutput "ℹ️  $Msg" "Cyan" }

# 全局变量
$script:ApiProcess = $null
$script:VueProcess = $null
$script:ApiLogFile = "logs\api-$(Get-Date -Format 'yyyyMMdd-HHmmss').log"
$script:VueLogFile = "logs\vue-$(Get-Date -Format 'yyyyMMdd-HHmmss').log"
$script:RootPath = $PSScriptRoot

# 确保日志目录存在
New-Item -ItemType Directory -Force -Path "logs" | Out-Null

Write-Section "数学漫画生成器 - 完整启动器"

# 检查.NET环境
Write-Info "检查 .NET 8 环境..."
try {
    $dotnetVersion = dotnet --version 2>&1
    Write-Success ".NET 版本: $dotnetVersion"
} catch {
    Write-Error "未找到 .NET SDK。请安装 .NET 8 SDK。"
    exit 1
}

# 检查Node.js环境（如果需要启动Vue）
if (-not $ApiOnly) {
    Write-Info "检查 Node.js 环境..."
    try {
        $nodeVersion = node --version 2>&1
        Write-Success "Node.js 版本: $nodeVersion"
    } catch {
        Write-Warning "未找到 Node.js。Vue 前端将无法启动。"
        $VueOnly = $false
    }
}

# 清理现有进程
Write-Section "清理现有服务"
Write-Info "关闭现有服务进程..."

$processesToKill = @("dotnet.exe", "node.exe")
foreach ($procName in $processesToKill) {
    Get-Process -Name $procName -ErrorAction SilentlyContinue | 
        Where-Object { $_.MainWindowTitle -like "*Math Comic*" -or $_.CommandLine -like "*MathComicGenerator*" } |
        Stop-Process -Force -ErrorAction SilentlyContinue
}

# 释放端口
Write-Info "释放端口 $ApiPort 和 $VuePort..."
$ports = @($ApiPort, $VuePort)
foreach ($port in $ports) {
    $connections = Get-NetTCPConnection -LocalPort $port -ErrorAction SilentlyContinue
    foreach ($conn in $connections) {
        if ($conn.OwningProcess) {
            Stop-Process -Id $conn.OwningProcess -Force -ErrorAction SilentlyContinue
            Write-Info "已关闭占用端口 $port 的进程"
        }
    }
}

Start-Sleep -Seconds 2

# 构建项目
if (-not $SkipBuild) {
    Write-Section "构建项目"
    
    Write-Info "清理构建缓存..."
    dotnet clean --verbosity quiet 2>&1 | Out-Null
    
    Write-Info "还原 NuGet 包..."
    dotnet restore 2>&1 | Out-Null
    if ($LASTEXITCODE -ne 0) {
        Write-Error "NuGet 包还原失败！"
        exit 1
    }
    
    Write-Info "构建项目..."
    dotnet build --no-restore 2>&1 | Tee-Object -Variable buildOutput
    if ($LASTEXITCODE -ne 0) {
        Write-Error "项目构建失败！"
        Write-Host $buildOutput
        exit 1
    }
    Write-Success "项目构建成功"
}

# 检查配置文件
Write-Section "检查配置"
$configFile = "MathComicGenerator.Api\appsettings.json"
if (Test-Path $configFile) {
    Write-Success "API 配置文件存在"
    
    # 检查API密钥配置
    $config = Get-Content $configFile | ConvertFrom-Json
    if ($config.DeepSeekAPI.ApiKey -and $config.DeepSeekAPI.ApiKey -ne "") {
        Write-Success "DeepSeek API 密钥已配置"
    } else {
        Write-Warning "DeepSeek API 密钥未配置"
    }
    
    if ($config.GeminiAPI.ApiKey -and $config.GeminiAPI.ApiKey -ne "") {
        Write-Success "Gemini API 密钥已配置"
    } else {
        Write-Warning "Gemini API 密钥未配置"
    }
} else {
    Write-Error "API 配置文件不存在: $configFile"
    exit 1
}

# 检查HTTPS证书
Write-Info "检查 HTTPS 开发证书..."
$certCheck = dotnet dev-certs https --check 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Warning "HTTPS 开发证书可能未配置"
    Write-Info "运行 'dotnet dev-certs https --trust' 来配置证书"
}

# 启动API服务
if (-not $VueOnly) {
    Write-Section "启动 API 服务"
    
    Write-Info "启动 API 服务器 (https://localhost:$ApiPort)..."
    
    $apiProject = "MathComicGenerator.Api"
    $apiStartInfo = New-Object System.Diagnostics.ProcessStartInfo
    $apiStartInfo.FileName = "dotnet"
    $apiStartInfo.Arguments = "run --project $apiProject --urls https://localhost:$ApiPort"
    $apiStartInfo.WorkingDirectory = $script:RootPath
    $apiStartInfo.UseShellExecute = $false
    $apiStartInfo.RedirectStandardOutput = $true
    $apiStartInfo.RedirectStandardError = $true
    $apiStartInfo.CreateNoWindow = $false
    $apiStartInfo.StandardOutputEncoding = [System.Text.Encoding]::UTF8
    $apiStartInfo.StandardErrorEncoding = [System.Text.Encoding]::UTF8
    
    $script:ApiProcess = New-Object System.Diagnostics.Process
    $script:ApiProcess.StartInfo = $apiStartInfo
    
    # 设置输出重定向
    $apiOutputHandler = {
        $line = $EventArgs.Data
        if ($line) {
            $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
            "$timestamp [API] $line" | Add-Content -Path $script:ApiLogFile
            Write-Host "[API] $line" -ForegroundColor Green
        }
    }
    
    $apiErrorHandler = {
        $line = $EventArgs.Data
        if ($line) {
            $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
            "$timestamp [API ERROR] $line" | Add-Content -Path $script:ApiLogFile
            Write-Host "[API ERROR] $line" -ForegroundColor Red
        }
    }
    
    Register-ObjectEvent -InputObject $script:ApiProcess -EventName "OutputDataReceived" -Action $apiOutputHandler | Out-Null
    Register-ObjectEvent -InputObject $script:ApiProcess -EventName "ErrorDataReceived" -Action $apiErrorHandler | Out-Null
    
    $script:ApiProcess.Start() | Out-Null
    $script:ApiProcess.BeginOutputReadLine()
    $script:ApiProcess.BeginErrorReadLine()
    
    Write-Success "API 服务已启动 (PID: $($script:ApiProcess.Id))"
    Write-Info "日志文件: $script:ApiLogFile"
    
    # 等待API启动
    Write-Info "等待 API 服务启动..."
    $maxWait = 30
    $waited = 0
    $apiReady = $false
    
    while ($waited -lt $maxWait -and -not $apiReady) {
        Start-Sleep -Seconds 1
        $waited++
        
        try {
            $response = Invoke-WebRequest -Uri "https://localhost:$ApiPort/api/comic/health" `
                -Method GET -SkipCertificateCheck -TimeoutSec 2 -ErrorAction Stop
            if ($response.StatusCode -eq 200) {
                $apiReady = $true
                Write-Success "API 服务已就绪！"
            }
        } catch {
            # 继续等待
            if ($waited % 5 -eq 0) {
                Write-Info "等待中... ($waited/$maxWait 秒)"
            }
        }
    }
    
    if (-not $apiReady) {
        Write-Warning "API 服务启动超时，但将继续运行"
    }
}

# 启动Vue前端
if (-not $ApiOnly) {
    Write-Section "启动 Vue 前端"
    
    $vuePath = "MathComicGenerator.Web.Vue"
    if (-not (Test-Path $vuePath)) {
        Write-Warning "Vue 项目目录不存在，跳过前端启动"
    } else {
        Write-Info "检查 Vue 依赖..."
        $packageJson = Join-Path $vuePath "package.json"
        $nodeModules = Join-Path $vuePath "node_modules"
        
        if (-not (Test-Path $nodeModules)) {
            Write-Info "安装 Vue 依赖..."
            Push-Location $vuePath
            npm install --no-audit --no-fund 2>&1 | Out-Null
            Pop-Location
        }
        
        Write-Info "启动 Vue 开发服务器 (http://localhost:$VuePort)..."
        
        $vueStartInfo = New-Object System.Diagnostics.ProcessStartInfo
        $vueStartInfo.FileName = "npm"
        $vueStartInfo.Arguments = "run dev"
        $vueStartInfo.WorkingDirectory = Join-Path $script:RootPath $vuePath
        $vueStartInfo.UseShellExecute = $false
        $vueStartInfo.RedirectStandardOutput = $true
        $vueStartInfo.RedirectStandardError = $true
        $vueStartInfo.CreateNoWindow = $false
        $vueStartInfo.StandardOutputEncoding = [System.Text.Encoding]::UTF8
        $vueStartInfo.StandardErrorEncoding = [System.Text.Encoding]::UTF8
        
        $script:VueProcess = New-Object System.Diagnostics.Process
        $script:VueProcess.StartInfo = $vueStartInfo
        
        # 设置输出重定向
        $vueOutputHandler = {
            $line = $EventArgs.Data
            if ($line) {
                $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
                "$timestamp [VUE] $line" | Add-Content -Path $script:VueLogFile
                Write-Host "[VUE] $line" -ForegroundColor Magenta
            }
        }
        
        $vueErrorHandler = {
            $line = $EventArgs.Data
            if ($line) {
                $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
                "$timestamp [VUE ERROR] $line" | Add-Content -Path $script:VueLogFile
                Write-Host "[VUE ERROR] $line" -ForegroundColor Red
            }
        }
        
        Register-ObjectEvent -InputObject $script:VueProcess -EventName "OutputDataReceived" -Action $vueOutputHandler | Out-Null
        Register-ObjectEvent -InputObject $script:VueProcess -EventName "ErrorDataReceived" -Action $vueErrorHandler | Out-Null
        
        $script:VueProcess.Start() | Out-Null
        $script:VueProcess.BeginOutputReadLine()
        $script:VueProcess.BeginErrorReadLine()
        
        Write-Success "Vue 服务已启动 (PID: $($script:VueProcess.Id))"
        Write-Info "日志文件: $script:VueLogFile"
        
        Start-Sleep -Seconds 3
    }
}

# 自动测试
if (-not $SkipTest -and $apiReady) {
    Write-Section "自动测试"
    
    Write-Info "测试 API 健康检查..."
    try {
        $healthResponse = Invoke-RestMethod -Uri "https://localhost:$ApiPort/api/comic/health" `
            -Method GET -SkipCertificateCheck -TimeoutSec 5
        Write-Success "健康检查通过"
        Write-Host ($healthResponse | ConvertTo-Json -Depth 2) -ForegroundColor Gray
    } catch {
        Write-Warning "健康检查失败: $($_.Exception.Message)"
    }
    
    Write-Info "测试提示词生成..."
    $testRequest = @{
        MathConcept = "加法运算"
        Options = @{
            PanelCount = 4
            AgeGroup = 1
            VisualStyle = 0
            Language = 0
        }
    } | ConvertTo-Json -Depth 3
    
    try {
        $testResponse = Invoke-RestMethod -Uri "https://localhost:$ApiPort/api/comic/generate-prompt" `
            -Method POST -Body $testRequest -ContentType "application/json" `
            -SkipCertificateCheck -TimeoutSec 60
        
        if ($testResponse.Success -and $testResponse.Data.GeneratedPrompt) {
            Write-Success "提示词生成测试通过"
            Write-Info "生成的提示词长度: $($testResponse.Data.GeneratedPrompt.Length) 字符"
            Write-Info "处理时间: $($testResponse.ProcessingTime)"
        } else {
            Write-Warning "提示词生成返回了意外的响应格式"
        }
    } catch {
        Write-Warning "提示词生成测试失败: $($_.Exception.Message)"
        if ($_.Exception.Response) {
            try {
                $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
                $errorBody = $reader.ReadToEnd()
                Write-Host "错误详情: $errorBody" -ForegroundColor Red
            } catch {
                # 忽略
            }
        }
    }
}

# 显示启动信息
Write-Section "启动完成"
Write-Success "所有服务已启动！"
Write-Host ""
Write-ColorOutput "服务地址：" "Cyan"
if (-not $VueOnly) {
    Write-ColorOutput "  API:  https://localhost:$ApiPort" "Green"
    Write-ColorOutput "  API 健康检查: https://localhost:$ApiPort/api/comic/health" "Gray"
    Write-ColorOutput "  API 配置状态: https://localhost:$ApiPort/api/comic/config-status" "Gray"
}
if (-not $ApiOnly) {
    Write-ColorOutput "  Vue 前端: http://localhost:$VuePort" "Green"
}
Write-Host ""
Write-ColorOutput "日志文件：" "Cyan"
if ($script:ApiLogFile) {
    Write-ColorOutput "  API: $script:ApiLogFile" "Gray"
}
if ($script:VueLogFile) {
    Write-ColorOutput "  Vue: $script:VueLogFile" "Gray"
}
Write-Host ""
Write-ColorOutput "提示：" "Yellow"
Write-ColorOutput "  - 按 Ctrl+C 停止所有服务" "White"
Write-ColorOutput "  - 查看日志文件获取详细信息" "White"
Write-ColorOutput "  - 如果遇到 SSL 证书错误，运行: dotnet dev-certs https --trust" "White"
Write-Host ""

# 等待用户中断
try {
    Write-ColorOutput "服务正在运行中... (按 Ctrl+C 停止)" "Cyan"
    
    # 监控进程状态
    while ($true) {
        Start-Sleep -Seconds 5
        
        # 检查API进程
        if ($script:ApiProcess -and $script:ApiProcess.HasExited) {
            Write-Warning "API 服务进程已退出 (退出代码: $($script:ApiProcess.ExitCode))"
            break
        }
        
        # 检查Vue进程
        if ($script:VueProcess -and $script:VueProcess.HasExited) {
            Write-Warning "Vue 服务进程已退出 (退出代码: $($script:VueProcess.ExitCode))"
            break
        }
    }
} catch {
    Write-Host ""
    Write-Info "正在停止服务..."
} finally {
    # 清理资源
    if ($script:ApiProcess -and -not $script:ApiProcess.HasExited) {
        Write-Info "停止 API 服务..."
        $script:ApiProcess.Kill()
        $script:ApiProcess.WaitForExit(5000)
    }
    
    if ($script:VueProcess -and -not $script:VueProcess.HasExited) {
        Write-Info "停止 Vue 服务..."
        $script:VueProcess.Kill()
        $script:VueProcess.WaitForExit(5000)
    }
    
    Write-Success "所有服务已停止"
}

