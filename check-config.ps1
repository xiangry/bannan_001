# 配置检查脚本

Write-Host "=== 数学漫画生成器配置检查 ===" -ForegroundColor Green

# 检查配置文件
$configFile = "MathComicGenerator.Api/appsettings.json"
if (Test-Path $configFile) {
    Write-Host "✅ 配置文件存在: $configFile" -ForegroundColor Green
    
    try {
        $config = Get-Content $configFile -Raw | ConvertFrom-Json
        
        # 检查API密钥
        $apiKey = $config.GeminiAPI.ApiKey
        if ($apiKey -eq "YOUR_GEMINI_API_KEY_HERE") {
            Write-Host "❌ API密钥未配置 - 仍使用默认占位符" -ForegroundColor Red
            Write-Host "   请按照快速配置指南.md设置真实的API密钥" -ForegroundColor Yellow
        } elseif ([string]::IsNullOrEmpty($apiKey)) {
            Write-Host "❌ API密钥为空" -ForegroundColor Red
        } else {
            Write-Host "✅ API密钥已配置" -ForegroundColor Green
            Write-Host "   密钥长度: $($apiKey.Length) 字符" -ForegroundColor Cyan
        }
        
        # 检查其他配置
        Write-Host "`n--- 其他配置检查 ---" -ForegroundColor Cyan
        Write-Host "API基础URL: $($config.GeminiAPI.BaseUrl)" -ForegroundColor White
        Write-Host "存储路径: $($config.Storage.BasePath)" -ForegroundColor White
        Write-Host "最大并发请求: $($config.ResourceManagement.MaxConcurrentRequests)" -ForegroundColor White
        
    } catch {
        Write-Host "❌ 配置文件格式错误: $($_.Exception.Message)" -ForegroundColor Red
    }
} else {
    Write-Host "❌ 配置文件不存在: $configFile" -ForegroundColor Red
}

# 检查数据目录
$dataDir = "data"
if (Test-Path $dataDir) {
    Write-Host "✅ 数据目录存在: $dataDir" -ForegroundColor Green
} else {
    Write-Host "⚠️  数据目录不存在，将在首次运行时创建" -ForegroundColor Yellow
}

# 检查端口占用
Write-Host "`n--- 端口检查 ---" -ForegroundColor Cyan
$ports = @(
    @{Port=5001; Service="Web界面"},
    @{Port=7109; Service="API服务"}
)

foreach ($portInfo in $ports) {
    try {
        $connection = Test-NetConnection -ComputerName localhost -Port $portInfo.Port -WarningAction SilentlyContinue -InformationLevel Quiet
        if ($connection.TcpTestSucceeded) {
            Write-Host "⚠️  端口 $($portInfo.Port) 已被占用 ($($portInfo.Service))" -ForegroundColor Yellow
        } else {
            Write-Host "✅ 端口 $($portInfo.Port) 可用 ($($portInfo.Service))" -ForegroundColor Green
        }
    } catch {
        Write-Host "✅ 端口 $($portInfo.Port) 可用 ($($portInfo.Service))" -ForegroundColor Green
    }
}

Write-Host "`n--- 建议操作 ---" -ForegroundColor Cyan
Write-Host "1. 如果API密钥未配置，请查看: 快速配置指南.md" -ForegroundColor White
Write-Host "2. 配置完成后运行: start-dev.bat" -ForegroundColor White
Write-Host "3. 访问应用: https://localhost:5001" -ForegroundColor White
Write-Host "4. 检查配置状态: https://localhost:7109/api/comic/config-status" -ForegroundColor White

Write-Host "`n=== 配置检查完成 ===" -ForegroundColor Green