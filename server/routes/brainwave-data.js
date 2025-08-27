const express = require('express');
const { query, transaction } = require('../config/database');
const { authenticateToken } = require('../middleware/auth');
const multer = require('multer');
const path = require('path');
const fs = require('fs');

const router = express.Router();

// 配置文件上传 - 先上传到临时目录，然后在处理函数中移动到最终位置
const storage = multer.diskStorage({
    destination: function (req, file, cb) {
        // 先上传到临时目录
        const tempDir = path.join(__dirname, '..', 'temp');
        if (!fs.existsSync(tempDir)) {
            fs.mkdirSync(tempDir, { recursive: true });
        }
        cb(null, tempDir);
    },
    filename: function (req, file, cb) {
        // 使用时间戳和原始文件名
        const timestamp = Date.now();
        const originalName = file.originalname;
        cb(null, `${timestamp}_${originalName}`);
    }
});

const upload = multer({ 
    storage: storage,
    fileFilter: function (req, file, cb) {
        // 只允许CSV文件
        if (file.mimetype === 'text/csv' || path.extname(file.originalname).toLowerCase() === '.csv') {
            cb(null, true);
        } else {
            cb(new Error('只允许上传CSV文件'));
        }
    }
});

// 上传脑电波数据CSV文件
router.post('/upload', authenticateToken, upload.single('csvFile'), async (req, res) => {
    try {
                const { dataType, institutionId, staffName, testerName } = req.body;
        const csvFile = req.file;
        
        // 验证必填字段
        if (!dataType || !csvFile || !institutionId || !staffName || !testerName) {
            return res.status(400).json({
                success: false,
                message: '请提供数据类型、CSV文件和相关信息'
            });
        }
        
        // 确保字符串字段不为空
        if (typeof institutionId !== 'string' || typeof staffName !== 'string' || typeof testerName !== 'string') {
            return res.status(400).json({
                success: false,
                message: '机构ID、医护人员姓名和测试者姓名必须是字符串'
            });
        }

        // 验证数据类型
        if (!['睁眼', '闭眼'].includes(dataType)) {
            return res.status(400).json({
                success: false,
                message: '数据类型必须是"睁眼"或"闭眼"'
            });
        }

        // 文件上传权限验证 - 只需要验证用户身份

        // 构建最终文件路径
        const timestamp = new Date().toISOString().replace(/[:.]/g, '-').slice(0, 19);
        const fileName = `${timestamp}_${dataType}.csv`;
        const finalDir = path.join(__dirname, '..', 'data', institutionId, staffName, testerName);
        const finalPath = path.join(finalDir, fileName);
        const relativePath = path.join(institutionId, staffName, testerName, fileName);



        // 确保最终目录存在
        if (!fs.existsSync(finalDir)) {
            fs.mkdirSync(finalDir, { recursive: true });
        }

        // 将文件从临时位置移动到最终位置
        fs.copyFileSync(csvFile.path, finalPath);

        // 删除临时文件
        fs.unlinkSync(csvFile.path);

        // 首先插入脑电波数据记录
        const insertSql = `
            INSERT INTO brainwave_data 
            (data_type, timestamp, csv_file_path, channel, sampling_rate) 
            VALUES (?, ?, ?, ?, ?)
        `;

        const channel = 1; // 默认通道
        const samplingRate = 520; // 默认采样率

        const brainwaveResult = await query(insertSql, [
            dataType,
            timestamp,
            relativePath,
            channel,
            samplingRate
        ]);

        const brainwaveDataId = brainwaveResult.insertId;

        // 然后创建测试结果记录，引用脑电波数据
        const testResultSql = `
            INSERT INTO test_results 
            (result_type, analysis_result, brainwave_data_id, created_at) 
            VALUES (?, ?, ?, NOW())
        `;

        const analysisResult = `脑电波数据文件: ${relativePath}`;
        const testResultResult = await query(testResultSql, [dataType, analysisResult, brainwaveDataId]);
        const testResultId = testResultResult.insertId;

        res.json({
            success: true,
            message: '成功上传脑电波数据CSV文件',
            data: {
                testResultId,
                brainwaveDataId,
                dataType,
                csvFilePath: relativePath,
                fileName: csvFile.filename
            }
        });

    } catch (error) {
        console.error('上传脑电波数据错误:', error);
        
        // 如果是文件上传错误，返回相应的错误信息
        if (error.message === '只允许上传CSV文件') {
            return res.status(400).json({
                success: false,
                message: '只允许上传CSV文件'
            });
        }
        
        res.status(500).json({
            success: false,
            message: '服务器内部错误'
        });
    }
});

module.exports = router;
