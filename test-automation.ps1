# 数学漫画生成器自动化测试脚本

Write-Host "=== 数学漫画生成器自动化测试 ===" -ForegroundColor Green

# 测试结果记录
$TestResults = @()

function Add-TestResult {
    param($TestName, $Status, $Message)
    $TestResults += [PSCustomObject]@{
        Test = $TestName
        Status = $Status
        Message = $Message
        Time = Get-Date
    }
    
    $color = if ($Status -eq "PASS") { "Green" } elseif ($Status -eq "FAIL") { "Red" } else { "Yellow" }
    Write-Host "[$Status] $TestName - $Message" -ForegroundColor $color
}

# 测试1: 基础环境检查
Write-Host "`n--- 基础环境检查 ---" -ForegroundColor Cyan

try {
    $dotnetVersion = dotnet --version
    Add-TestResult "DotNet版本检查" "PASS" "版本: $dotnetVersion"
} catch {
    Add-TestResult "DotNet版本检查" "FAIL" "未安装.NET SDK"
}

# 测试2: 项目构建
Write-Host "`n--- 项目构建测试 ---" -ForegroundColor Cyan

try {
    $buildResult = dotnet build --verbosity quiet 2>&1
    if ($LASTEXITCODE -eq 0) {
        Add-TestResult "项目构建" "PASS" "构建成功"
    } else {
        Add-TestResult "项目构建" "FAIL" "构建失败: $buildResult"
    }
} catch {
    Add-TestResult "项目构建" "FAIL" "构建异常: $($_.Exception.Message)"
}

# 测试3: 配置文件检查
Write-Host "`n--- 配置文件检查 ---" -ForegroundColor Cyan

$configFiles = @(
    "MathComicGenerator.Api/appsettings.json",
    "MathComicGenerator.Web/appsettings.json"
)

foreach ($configFile in $configFiles) {
    if (Test-Path $configFile) {
        Add-TestResult "配置文件存在" "PASS" $configFile
        
        # 检查API密钥配置
        if ($configFile -like "*Api*") {
            $content = Get-Content $configFile -Raw | ConvertFrom-Json
            if ($content.GeminiAPI.ApiKey -eq "YOUR_GEMINI_API_KEY_HERE") {
                Add-TestResult "API密钥配置" "WARN" "使用默认占位符，需要配置真实API密钥"
            } else {
                Add-TestResult "API密钥配置" "PASS" "已配置API密钥"
            }
        }
    } else {
        Add-TestResult "配置文件存在" "FAIL" "缺失: $configFile"
    }
}

# 测试4: 端口占用检查
Write-Host "`n--- 端口占用检查 ---" -ForegroundColor Cyan

$ports = @(5001, 7109)
foreach ($port in $ports) {
    try {
        $connection = Test-NetConnection -ComputerName localhost -Port $port -WarningAction SilentlyContinue
        if ($connection.TcpTestSucceeded) {
            Add-TestResult "端口检查" "WARN" "端口 $port 已被占用"
        } else {
            Add-TestResult "端口检查" "PASS" "端口 $port 可用"
        }
    } catch {
        Add-TestResult "端口检查" "PASS" "端口 $port 可用"
    }
}

# 测试5: 数据目录检查
Write-Host "`n--- 数据目录检查 ---" -ForegroundColor Cyan

$dataDir = "./data"
if (Test-Path $dataDir) {
    Add-TestResult "数据目录" "PASS" "数据目录存在"
} else {
    try {
        New-Item -ItemType Directory -Path $dataDir -Force | Out-Null
        Add-TestResult "数据目录" "PASS" "已创建数据目录"
    } catch {
        Add-TestResult "数据目录" "FAIL" "无法创建数据目录: $($_.Exception.Message)"
    }
}

# 测试6: 服务启动测试
Write-Host "`n--- 服务启动测试 ---" -ForegroundColor Cyan

# 启动API服务
Write-Host "启动API服务..." -ForegroundColor Yellow
$apiProcess = Start-Process -FilePath "dotnet" -ArgumentList "run --project MathComicGenerator.Api" -PassThru -WindowStyle Hidden

Start-Sleep -Seconds 10

# 检查API服务
try {
    $response = Invoke-WebRequest -Uri "https://localhost:7109/api/comic/health" -SkipCertificateCheck -TimeoutSec 5
    if ($response.StatusCode -eq 200) {
        Add-TestResult "API服务启动" "PASS" "API服务正常响应"
    } else {
        Add-TestResult "API服务启动" "FAIL" "API服务响应异常: $($response.StatusCode)"
    }
} catch {
    Add-TestResult "API服务启动" "FAIL" "API服务无法访问: $($_.Exception.Message)"
}

# 启动Web服务
Write-Host "启动Web服务..." -ForegroundColor Yellow
$webProcess = Start-Process -FilePath "dotnet" -ArgumentList "run --project MathComicGenerator.Web" -PassThru -WindowStyle Hidden

Start-Sleep -Seconds 10

# 检查Web服务
try {
    $response = Invoke-WebRequest -Uri "https://localhost:5001/" -SkipCertificateCheck -TimeoutSec 5
    if ($response.StatusCode -eq 200) {
        Add-TestResult "Web服务启动" "PASS" "Web服务正常响应"
    } else {
        Add-TestResult "Web服务启动" "FAIL" "Web服务响应异常: $($response.StatusCode)"
    }
} catch {
    Add-TestResult "Web服务启动" "FAIL" "Web服务无法访问: $($_.Exception.Message)"
}

# 测试7: API功能测试
Write-Host "`n--- API功能测试 ---" -ForegroundColor Cyan

try {
    # 测试概念验证API
    $validatePayload = @{
        Concept = "加法运算"
    } | ConvertTo-Json

    $response = Invoke-WebRequest -Uri "https://localhost:7109/api/comic/validate" -Method POST -Body $validatePayload -ContentType "application/json" -SkipCertificateCheck -TimeoutSec 10
    
    if ($response.StatusCode -eq 200) {
        Add-TestResult "概念验证API" "PASS" "验证API正常工作"
    } else {
        Add-TestResult "概念验证API" "FAIL" "验证API响应异常"
    }
} catch {
    Add-TestResult "概念验证API" "FAIL" "验证API调用失败: $($_.Exception.Message)"
}

# 清理进程
Write-Host "`n--- 清理测试环境 ---" -ForegroundColor Cyan
if ($apiProcess -and !$apiProcess.HasExited) {
    Stop-Process -Id $apiProcess.Id -Force
    Add-TestResult "进程清理" "PASS" "API进程已停止"
}

if ($webProcess -and !$webProcess.HasExited) {
    Stop-Process -Id $webProcess.Id -Force
    Add-TestResult "进程清理" "PASS" "Web进程已停止"
}

# 生成测试报告
Write-Host "`n=== 测试报告 ===" -ForegroundColor Green

$passCount = ($TestResults | Where-Object { $_.Status -eq "PASS" }).Count
$failCount = ($TestResults | Where-Object { $_.Status -eq "FAIL" }).Count
$warnCount = ($TestResults | Where-Object { $_.Status -eq "WARN" }).Count

Write-Host "总测试数: $($TestResults.Count)" -ForegroundColor White
Write-Host "通过: $passCount" -ForegroundColor Green
Write-Host "失败: $failCount" -ForegroundColor Red
Write-Host "警告: $warnCount" -ForegroundColor Yellow

# 输出详细结果
Write-Host "`n--- 详细结果 ---" -ForegroundColor Cyan
$TestResults | Format-Table -AutoSize

# 保存测试报告
$reportPath = "test-report-$(Get-Date -Format 'yyyyMMdd-HHmmss').json"
$TestResults | ConvertTo-Json -Depth 3 | Out-File -FilePath $reportPath -Encoding UTF8
Write-Host "测试报告已保存到: $reportPath" -ForegroundColor Green

# 返回退出码
if ($failCount -gt 0) {
    exit 1
} else {
    exit 0
}