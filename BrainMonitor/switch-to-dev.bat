@echo off
echo 切换到开发模式...
echo 注意：默认情况下软件会连接生产服务器
echo 只有运行此脚本后才会连接本地开发服务器
echo.
set ISDEVELOPMENT=true
echo 环境变量已设置: ISDEVELOPMENT=%ISDEVELOPMENT%
echo 请重启应用程序以使配置生效
echo.
echo 开发模式将使用: http://localhost:3000
pause
