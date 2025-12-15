#!/bin/bash

echo "启动数学漫画生成器开发环境..."

echo ""
echo "正在构建项目..."
dotnet build

if [ $? -ne 0 ]; then
    echo "构建失败！"
    exit 1
fi

echo ""
echo "启动API服务器 (端口 7109)..."
gnome-terminal --title="Math Comic Generator API" -- bash -c "dotnet run --project MathComicGenerator.Api; exec bash" &

echo ""
echo "等待API服务器启动..."
sleep 5

echo ""
echo "启动Web服务器 (端口 5001)..."
gnome-terminal --title="Math Comic Generator Web" -- bash -c "dotnet run --project MathComicGenerator.Web; exec bash" &

echo ""
echo "开发环境启动完成！"
echo "API: https://localhost:7109"
echo "Web: https://localhost:5001"
echo ""
echo "按Enter键退出..."
read