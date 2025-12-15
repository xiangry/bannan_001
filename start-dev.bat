@echo off
echo 启动数学漫画生成器开发环境...

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
pause > nul