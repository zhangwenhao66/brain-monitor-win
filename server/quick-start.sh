#!/bin/bash

echo "🚀 Brain Monitor 机构登录功能快速启动脚本"
echo

# 检查Node.js环境
echo "📋 检查Node.js环境..."
if ! command -v node &> /dev/null; then
    echo "❌ 未检测到Node.js，请先安装Node.js"
    exit 1
fi
echo "✅ Node.js 环境正常"

# 检查MySQL服务
echo
echo "📋 检查MySQL服务..."
if ! pgrep -x "mysqld" > /dev/null; then
    echo "⚠️  未检测到MySQL服务，请确保MySQL已启动"
    echo "   可以通过以下命令启动MySQL服务："
    echo "   sudo systemctl start mysql"
    echo "   或"
    echo "   sudo service mysql start"
    echo
fi

# 安装依赖
echo
echo "📦 安装依赖包..."
if ! npm install; then
    echo "❌ 依赖安装失败"
    exit 1
fi
echo "✅ 依赖安装完成"

# 初始化数据库
echo
echo "🗄️  初始化数据库..."
if ! npm run init-db; then
    echo "❌ 数据库初始化失败"
    echo "请检查MySQL连接配置"
    exit 1
fi
echo "✅ 数据库初始化完成"

# 启动服务
echo
echo "🌐 启动后端服务..."
echo "服务将在 http://localhost:3000 启动"
echo "按 Ctrl+C 停止服务"
echo
npm start
