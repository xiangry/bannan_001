@echo off
chcp 65001 > nul
echo ========================================
echo 数学漫画生成器 - 启动器
echo ========================================
echo.
echo 正在启动 PowerShell 启动器...
echo.

powershell -ExecutionPolicy Bypass -File "%~dp0launcher.ps1" %*

if %ERRORLEVEL% neq 0 (
    echo.
    echo 启动失败，错误代码: %ERRORLEVEL%
    pause
)

