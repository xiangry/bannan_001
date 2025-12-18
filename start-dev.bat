@echo off
echo 启动数学漫画生成器开发环境...

echo.
echo 正在关闭现有服务...
echo 关闭所有 MathComicGenerator 相关进程...
taskkill /f /im "MathComicGenerator.Api.exe" 2>nul
taskkill /f /im "MathComicGenerator.Web.exe" 2>nul

echo 关闭可能占用端口的 dotnet 进程...
for /f "tokens=5" %%a in ('netstat -aon ^| findstr ":7109 "') do taskkill /f /pid %%a 2>nul
for /f "tokens=5" %%a in ('netstat -aon ^| findstr ":5001 "') do taskkill /f /pid %%a 2>nul

echo 等待进程完全关闭...
timeout /t 2 /nobreak > nul

echo.
echo 清理构建缓存...
dotnet clean > nul

echo.
echo 检查配置...
powershell -ExecutionPolicy Bypass -File check-config.ps1

echo.
echo 正在构建项目...
dotnet build

if %ERRORLEVEL% neq 0 (
    echo 构建失败！
    pause
    exit /b 1
)

echo.
echo 启动API服务器 (端口 7109)...
start "Math Comic Generator API" cmd /k "dotnet run --project MathComicGenerator.Api"

echo.
echo 等待API服务器启动...
timeout /t 5 /nobreak > nul

echo.
echo 启动Web服务器 (端口 5001)...
start "Math Comic Generator Web" cmd /k "dotnet run --project MathComicGenerator.Web"

echo.
echo 开发环境启动完成！
echo API: https://localhost:7109
echo Web: https://localhost:5001
echo.
echo 按任意键退出...
@REM pause > nul