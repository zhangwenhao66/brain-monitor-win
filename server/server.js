const express = require('express');
const cors = require('cors');
const helmet = require('helmet');
const rateLimit = require('express-rate-limit');
require('dotenv').config();

const { testConnection } = require('./config/database');

// 导入路由
const authRoutes = require('./routes/auth');
const testerRoutes = require('./routes/testers');
const testRecordRoutes = require('./routes/test-records');
const brainwaveDataRoutes = require('./routes/brainwave-data');
const testResultRoutes = require('./routes/test-results');

const app = express();
const PORT = process.env.PORT || 3000;

// 安全中间件
app.use(helmet());

// CORS配置
app.use(cors({
    origin: process.env.NODE_ENV === 'production' 
        ? ['http://localhost:8080', 'https://yourdomain.com'] 
        : true,
    credentials: true
}));

// 速率限制
const limiter = rateLimit({
    windowMs: 15 * 60 * 1000, // 15分钟
    max: 100, // 限制每个IP 15分钟内最多100个请求
    message: {
        success: false,
        message: '请求过于频繁，请稍后再试'
    }
});
app.use('/api/', limiter);

// 解析JSON请求体
app.use(express.json({ limit: '10mb' }));
app.use(express.urlencoded({ extended: true, limit: '10mb' }));

// 请求日志中间件
app.use((req, res, next) => {
    console.log(`${new Date().toISOString()} - ${req.method} ${req.path}`);
    next();
});

// 健康检查端点
app.get('/health', (req, res) => {
    res.json({
        success: true,
        message: 'Brain Monitor Server is running',
        timestamp: new Date().toISOString(),
        version: '1.4.0'
    });
});

// API路由
app.use('/api/auth', authRoutes);
app.use('/api/testers', testerRoutes);
app.use('/api/test-records', testRecordRoutes);
app.use('/api/brainwave-data', brainwaveDataRoutes);
app.use('/api/test-results', testResultRoutes);

// 404处理
app.use('*', (req, res) => {
    res.status(404).json({
        success: false,
        message: '接口不存在'
    });
});

// 全局错误处理中间件
app.use((error, req, res, next) => {
    console.error('服务器错误:', error);
    
    // 数据库连接错误
    if (error.code === 'ECONNREFUSED' || error.code === 'PROTOCOL_CONNECTION_LOST') {
        return res.status(503).json({
            success: false,
            message: '数据库连接失败，请稍后重试'
        });
    }
    
    // JWT错误
    if (error.name === 'JsonWebTokenError') {
        return res.status(401).json({
            success: false,
            message: '无效的访问令牌'
        });
    }
    
    if (error.name === 'TokenExpiredError') {
        return res.status(401).json({
            success: false,
            message: '访问令牌已过期'
        });
    }
    
    // 默认错误响应
    res.status(500).json({
        success: false,
        message: process.env.NODE_ENV === 'development' 
            ? error.message 
            : '服务器内部错误'
    });
});

// 启动服务器
async function startServer() {
    try {
        // 测试数据库连接
        const dbConnected = await testConnection();
        if (!dbConnected) {
            console.error('❌ 无法连接到数据库，服务器启动失败');
            process.exit(1);
        }

        // 启动HTTP服务器
        app.listen(PORT, () => {
            console.log(`🚀 Brain Monitor Server 启动成功`);
            console.log(`📍 服务器地址: http://localhost:${PORT}`);
            console.log(`🌍 环境: ${process.env.NODE_ENV || 'development'}`);
            console.log(`📊 健康检查: http://localhost:${PORT}/health`);
            console.log(`⏰ 启动时间: ${new Date().toLocaleString('zh-CN')}`);
        });

    } catch (error) {
        console.error('❌ 服务器启动失败:', error);
        process.exit(1);
    }
}

// 优雅关闭
process.on('SIGINT', () => {
    console.log('\n🛑 收到中断信号，正在关闭服务器...');
    process.exit(0);
});

process.on('SIGTERM', () => {
    console.log('\n🛑 收到终止信号，正在关闭服务器...');
    process.exit(0);
});

// 启动服务器
startServer();

