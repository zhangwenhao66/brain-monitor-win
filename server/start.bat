@echo off
chcp 65001 >nul
echo ========================================
echo    脑镜BrainMirror Server 启动脚本
echo ========================================
echo.

echo 正在检查Node.js环境...
node --version >nul 2>&1
if errorlevel 1 (
    echo ❌ 错误: 未找到Node.js，请先安装Node.js
    echo 下载地址: https://nodejs.org/
    pause
    exit /b 1
)

echo ✅ Node.js环境检查通过
echo.

echo 正在检查依赖包...
if not exist "node_modules" (
    echo 📦 正在安装依赖包...
    npm install
    if errorlevel 1 (
        echo ❌ 依赖包安装失败
        pause
        exit /b 1
    )
    echo ✅ 依赖包安装完成
) else (
    echo ✅ 依赖包已存在
)

echo.
echo 正在检查环境配置文件...
if not exist ".env" (
    echo ⚠️  警告: 未找到.env配置文件
    echo 📝 正在复制环境配置模板...
    copy env.example .env
    echo.
    echo 请编辑.env文件，配置数据库连接信息：
    echo   - DB_HOST: 数据库主机地址
    echo   - DB_PORT: 数据库端口
    echo   - DB_USER: 数据库用户名
    echo   - DB_PASSWORD: 数据库密码
    echo   - DB_NAME: 数据库名称
    echo   - JWT_SECRET: JWT密钥
    echo.
    echo 配置完成后，按任意键继续...
    pause
)

echo.
echo 🚀 正在启动脑镜BrainMirror Server...
echo 📍 服务器地址: http://localhost:3000/api
echo 📊 健康检查: http://localhost:3000/api/health
echo.
echo 按 Ctrl+C 停止服务器
echo ========================================
echo.

npm start

