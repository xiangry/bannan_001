@echo off
setlocal enabledelayedexpansion

rem Ensure console uses UTF-8 to avoid Chinese garbled output
chcp 65001 > nul
rem Prefer .NET CLI messages in Simplified Chinese (optional)
set "DOTNET_CLI_UI_LANGUAGE=zh-CN"

echo ========================================
echo 数学漫画生成器 - 开发环境启动脚本
echo ========================================

:: 检查 .NET 8 是否安装
echo 检查 .NET 8 环境...
dotnet --version >nul 2>&1
if %ERRORLEVEL% neq 0 (
    echo 错误：未找到 .NET SDK。请安装 .NET 8 SDK。
    pause
    exit /b 1
)

:: 显示 .NET 版本
for /f "tokens=*" %%i in ('dotnet --version') do set DOTNET_VERSION=%%i
echo .NET 版本: %DOTNET_VERSION%

:: 关闭现有服务
echo.
echo 正在关闭现有服务...
taskkill /f /im "dotnet.exe" 2>nul
taskkill /f /im "MathComicGenerator.Api.exe" 2>nul
taskkill /f /im "MathComicGenerator.Web.exe" 2>nul

:: 关闭占用端口的进程
echo 释放端口 7109 和 5001...
for /f "tokens=5" %%a in ('netstat -aon ^| findstr ":7109 "') do (
    echo 关闭进程 %%a (端口 7109)
    taskkill /f /pid %%a 2>nul
)
for /f "tokens=5" %%a in ('netstat -aon ^| findstr ":5001 "') do (
    echo 关闭进程 %%a (端口 5001)
    taskkill /f /pid %%a 2>nul
)

echo 等待进程完全关闭...
timeout /t 3 /nobreak > nul

:: 清理和还原
echo.
echo 清理构建缓存...
dotnet clean --verbosity quiet
if %ERRORLEVEL% neq 0 (
    echo 警告：清理过程中出现问题，继续执行...
)

echo.
echo 还原 NuGet 包...
dotnet restore
if %ERRORLEVEL% neq 0 (
    echo 错误：NuGet 包还原失败！
    pause
    exit /b 1
)

:: 构建项目
echo.
echo 构建项目...
dotnet build --no-restore
if %ERRORLEVEL% neq 0 (
    echo 错误：项目构建失败！请检查编译错误。
    echo.
    echo 常见问题解决方案：
    echo 1. 检查是否缺少 using 语句
    echo 2. 检查 nullable 引用类型配置
    echo 3. 运行 'dotnet build' 查看详细错误信息
    pause
    exit /b 1
)

:: 检查配置文件
echo.
echo 检查配置文件...
if exist "MathComicGenerator.Api\appsettings.json" (
    echo ✓ API 配置文件存在
) else (
    echo ✗ API 配置文件缺失
)

if exist "check-config.ps1" (
    echo 运行配置检查脚本...
    powershell -ExecutionPolicy Bypass -File check-config.ps1
)

:: 确保开发证书可信
echo.
echo 检查开发证书...
dotnet dev-certs https --check --trust >nul 2>&1
if %ERRORLEVEL% neq 0 (
    echo 警告：HTTPS 开发证书可能未配置。
    echo 如果遇到 SSL 错误，请运行：dotnet dev-certs https --trust
)

:: 启动服务
echo.
echo ========================================
echo 启动服务...
echo ========================================

echo.
echo 启动 API 服务器 (https://localhost:7109)...
start "Math Comic Generator API" cmd /k "title Math Comic Generator API && cd /d "%~dp0" && chcp 65001 > nul && echo 启动 API 服务器... && dotnet run --project MathComicGenerator.Api --urls https://localhost:7109"

echo 等待 API 服务器启动...
timeout /t 10 /nobreak > nul

echo.
echo 启动 Vue 开发服务器 (http://localhost:5173)...
start "Math Comic Generator Vue" cmd /k "title Math Comic Generator Vue && cd /d "%~dp0MathComicGenerator.Web.Vue" && chcp 65001 > nul && echo 启动 Vue 开发服务器... && npm install --no-audit --no-fund && npm run dev"

echo.
echo ========================================
echo 开发环境启动完成！
echo ========================================
echo.
echo 服务地址：
echo   API:  https://localhost:7109
echo   Web (Vite dev):  http://localhost:5173
echo.
echo 提示：
echo - 前端开发使用 Vite (http://localhost:5173)，API 请求由 Vite 代理到后端 /api
echo - 如果需要后端托管的静态站点，请运行 scripts\build_and_deploy_vue.ps1 并重启 API
echo - 如果遇到 SSL 证书问题，运行：dotnet dev-certs https --trust
echo - 查看各服务窗口的启动日志
echo - 按 Ctrl+C 在各服务窗口中停止服务
echo.
echo 按任意键关闭此窗口...
pause > nul