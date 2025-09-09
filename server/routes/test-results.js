const express = require('express');
const { query, transaction } = require('../config/database');
const { authenticateToken } = require('../middleware/auth');

const router = express.Router();

// 保存测试结果
router.post('/', authenticateToken, async (req, res) => {
    try {
        const { 
            testRecordId, 
            resultType, 
            alphaPower, 
            betaPower, 
            thetaPower, 
            deltaPower, 
            gammaPower, 
            totalPower, 
            dominantFrequency, 
            coherenceScore, 
            attentionScore, 
            relaxationScore, 
            analysisResult 
        } = req.body;

        // 验证必填字段
        if (!testRecordId || !resultType) {
            return res.status(400).json({
                success: false,
                message: '测试记录ID和结果类型为必填字段'
            });
        }

        // 验证结果类型
        if (!['睁眼', '闭眼', '综合'].includes(resultType)) {
            return res.status(400).json({
                success: false,
                message: '结果类型必须是"睁眼"、"闭眼"或"综合"'
            });
        }

        // 验证测试记录是否存在且属于当前工作人员
        const [testRecord] = await query(
            'SELECT id, medical_staff_id, test_status FROM test_records WHERE id = ?',
            [testRecordId]
        );

        if (!testRecord) {
            return res.status(404).json({
                success: false,
                message: '测试记录不存在'
            });
        }

        if (testRecord.medical_staff_id !== req.user.id) {
            return res.status(403).json({
                success: false,
                message: '无权操作该测试记录'
            });
        }

        // 检查是否已存在相同类型的结果
        const [existingResult] = await query(
            'SELECT id FROM test_results WHERE test_record_id = ? AND result_type = ?',
            [testRecordId, resultType]
        );

        if (existingResult) {
            // 更新现有结果
            await query(
                `UPDATE test_results SET 
                 alpha_power = ?, beta_power = ?, theta_power = ?, delta_power = ?, 
                 gamma_power = ?, total_power = ?, dominant_frequency = ?, 
                 coherence_score = ?, attention_score = ?, relaxation_score = ?, 
                 analysis_result = ?
                 WHERE test_record_id = ? AND result_type = ?`,
                [
                    alphaPower, betaPower, thetaPower, deltaPower, gammaPower,
                    totalPower, dominantFrequency, coherenceScore, attentionScore,
                    relaxationScore, analysisResult, testRecordId, resultType
                ]
            );

            res.json({
                success: true,
                message: '测试结果更新成功',
                data: { testRecordId, resultType }
            });
        } else {
            // 插入新结果
            const result = await query(
                `INSERT INTO test_results 
                 (test_record_id, result_type, alpha_power, beta_power, theta_power, 
                  delta_power, gamma_power, total_power, dominant_frequency, 
                  coherence_score, attention_score, relaxation_score, analysis_result) 
                 VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)`,
                [
                    testRecordId, resultType, alphaPower, betaPower, thetaPower,
                    deltaPower, gammaPower, totalPower, dominantFrequency,
                    coherenceScore, attentionScore, relaxationScore, analysisResult
                ]
            );

            res.status(201).json({
                success: true,
                message: '测试结果保存成功',
                data: { 
                    id: result.insertId,
                    testRecordId, 
                    resultType 
                }
            });
        }

    } catch (error) {
        console.error('保存测试结果错误:', error);
        res.status(500).json({
            success: false,
            message: '服务器内部错误'
        });
    }
});

// 获取测试记录的所有结果
router.get('/test-record/:recordId', authenticateToken, async (req, res) => {
    try {
        const { recordId } = req.params;

        // 验证测试记录是否存在且属于当前工作人员
        const [testRecord] = await query(
            'SELECT id, medical_staff_id FROM test_records WHERE id = ?',
            [recordId]
        );

        if (!testRecord) {
            return res.status(404).json({
                success: false,
                message: '测试记录不存在'
            });
        }

        if (testRecord.medical_staff_id !== req.user.id) {
            return res.status(403).json({
                success: false,
                message: '无权访问该测试记录'
            });
        }

        // 获取所有测试结果
        const testResults = await query(
            `SELECT result_type, alpha_power, beta_power, theta_power, delta_power, 
                    gamma_power, total_power, dominant_frequency, coherence_score,
                    attention_score, relaxation_score, analysis_result, created_at
             FROM test_results
             WHERE test_record_id = ?
             ORDER BY result_type`,
            [recordId]
        );

        res.json({
            success: true,
            data: {
                testRecordId: recordId,
                testResults
            }
        });

    } catch (error) {
        console.error('获取测试结果错误:', error);
        res.status(500).json({
            success: false,
            message: '服务器内部错误'
        });
    }
});

// 获取特定类型的测试结果
router.get('/test-record/:recordId/:resultType', authenticateToken, async (req, res) => {
    try {
        const { recordId, resultType } = req.params;

        // 验证结果类型
        if (!['睁眼', '闭眼', '综合'].includes(resultType)) {
            return res.status(400).json({
                success: false,
                message: '无效的结果类型'
            });
        }

        // 验证测试记录是否存在且属于当前工作人员
        const [testRecord] = await query(
            'SELECT id, medical_staff_id FROM test_records WHERE id = ?',
            [recordId]
        );

        if (!testRecord) {
            return res.status(404).json({
                success: false,
                message: '测试记录不存在'
            });
        }

        if (testRecord.medical_staff_id !== req.user.id) {
            return res.status(403).json({
                success: false,
                message: '无权访问该测试记录'
            });
        }

        // 获取特定类型的测试结果
        const [testResult] = await query(
            `SELECT result_type, alpha_power, beta_power, theta_power, delta_power, 
                    gamma_power, total_power, dominant_frequency, coherence_score,
                    attention_score, relaxation_score, analysis_result, created_at
             FROM test_results
             WHERE test_record_id = ? AND result_type = ?`,
            [recordId, resultType]
        );

        if (!testResult) {
            return res.status(404).json({
                success: false,
                message: '该类型的测试结果不存在'
            });
        }

        res.json({
            success: true,
            data: testResult
        });

    } catch (error) {
        console.error('获取特定测试结果错误:', error);
        res.status(500).json({
            success: false,
            message: '服务器内部错误'
        });
    }
});

// 删除测试结果
router.delete('/test-record/:recordId/:resultType', authenticateToken, async (req, res) => {
    try {
        const { recordId, resultType } = req.params;

        // 验证结果类型
        if (!['睁眼', '闭眼', '综合'].includes(resultType)) {
            return res.status(400).json({
                success: false,
                message: '无效的结果类型'
            });
        }

        // 验证测试记录是否存在且属于当前工作人员
        const [testRecord] = await query(
            'SELECT id, medical_staff_id, test_status FROM test_records WHERE id = ?',
            [recordId]
        );

        if (!testRecord) {
            return res.status(404).json({
                success: false,
                message: '测试记录不存在'
            });
        }

        if (testRecord.medical_staff_id !== req.user.id) {
            return res.status(403).json({
                success: false,
                message: '无权操作该测试记录'
            });
        }

        // 只允许删除未完成的测试记录的结果
        if (testRecord.test_status === '已完成') {
            return res.status(400).json({
                success: false,
                message: '测试已完成，无法删除结果'
            });
        }

        // 删除特定类型的测试结果
        const result = await query(
            'DELETE FROM test_results WHERE test_record_id = ? AND result_type = ?',
            [recordId, resultType]
        );

        res.json({
            success: true,
            message: '测试结果删除成功',
            data: {
                testRecordId: recordId,
                resultType,
                deletedCount: result.affectedRows
            }
        });

    } catch (error) {
        console.error('删除测试结果错误:', error);
        res.status(500).json({
            success: false,
            message: '服务器内部错误'
        });
    }
});

module.exports = router;

