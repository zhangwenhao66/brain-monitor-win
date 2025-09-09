@echo off
chcp 65001 >nul
echo 🚀 脑镜BrainMirror 机构登录功能快速启动脚本
echo.

echo 📋 检查Node.js环境...
node --version >nul 2>&1
if errorlevel 1 (
    echo ❌ 未检测到Node.js，请先安装Node.js
    pause
    exit /b 1
)
echo ✅ Node.js 环境正常

echo.
echo 📋 检查MySQL服务...
net start | findstr "MySQL" >nul
if errorlevel 1 (
    echo ⚠️  未检测到MySQL服务，请确保MySQL已启动
    echo    可以通过服务管理器启动MySQL服务
    echo.
)

echo.
echo 📦 安装依赖包...
call npm install
if errorlevel 1 (
    echo ❌ 依赖安装失败
    pause
    exit /b 1
)
echo ✅ 依赖安装完成

echo.
echo 🗄️  初始化数据库...
call npm run init-db
if errorlevel 1 (
    echo ❌ 数据库初始化失败
    echo 请检查MySQL连接配置
    pause
    exit /b 1
)
echo ✅ 数据库初始化完成

echo.
echo 🌐 启动后端服务...
echo 服务将在 http://localhost:3000/api 启动
echo 按 Ctrl+C 停止服务
echo.
call npm start

pause
