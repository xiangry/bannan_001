@echo off
chcp 65001 > nul
echo ========================================
echo 数学漫画生成器 - 快速启动
echo ========================================
echo.

powershell -ExecutionPolicy Bypass -File "%~dp0launcher-simple.ps1"

pause

