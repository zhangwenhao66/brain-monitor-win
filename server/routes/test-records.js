const express = require('express');
const { query, transaction } = require('../config/database');
const { authenticateToken } = require('../middleware/auth');

const router = express.Router();

// 保存测试记录和测试者信息
router.post('/', authenticateToken, async (req, res) => {
    try {
        const { 
            testerId, 
            testerName, 
            medicalStaffId, 
            medicalStaffName, 
            institutionId, 
            testDate, 
            mocaScore, 
            mmseScore, 
            gripStrength, 
            testStatus,
            openEyesResultId,
            closedEyesResultId
        } = req.body;

        // 验证必填字段
        if (!testerId || !testerName || !medicalStaffId || !medicalStaffName || !institutionId) {
            return res.status(400).json({
                success: false,
                message: '测试者ID、姓名、医护人员ID、姓名和机构ID为必填字段'
            });
        }

        // 检查医护人员是否存在 - 使用数据库ID查询
        let [existingStaff] = await query(
            'SELECT id FROM medical_staff WHERE id = ? AND institution_id = ?',
            [medicalStaffId, institutionId]
        );

        if (!existingStaff) {
            console.log('医护人员不存在，创建新记录');
            // 如果医护人员不存在，创建新记录
            await query(
                'INSERT INTO medical_staff (staff_id, name, account, password, institution_id, created_at) VALUES (?, ?, ?, ?, ?, NOW())',
                [medicalStaffId.toString(), medicalStaffName, `staff_${medicalStaffId}`, '$2a$10$92IXUNpkjO0rOQ5byMi.Ye4oKoEa3Ro9llC/.og/at2uheWG/igi.', institutionId]
            );
        }

        // 获取医护人员的实际ID（可能是新插入的，也可能是已存在的）
        let [currentStaff] = await query(
            'SELECT id FROM medical_staff WHERE id = ? AND institution_id = ?',
            [medicalStaffId, institutionId]
        );

        // 首先检查测试者是否存在，如果不存在则创建
        let [existingTester] = await query(
            'SELECT id FROM testers WHERE tester_id = ? AND institution_id = ?',
            [testerId, institutionId]
        );

        if (!existingTester) {
            await query(
                'INSERT INTO testers (tester_id, name, institution_id, medical_staff_id, created_at) VALUES (?, ?, ?, ?, NOW())',
                [testerId, testerName, institutionId, currentStaff.id]
            );
        }


        
        // 生成报告时创建测试记录 - 包含评分数据和脑电波结果关联
        const testRecordResult = await query(
            `INSERT INTO test_records 
             (tester_id, medical_staff_id, institution_id, test_start_time, test_status, moca_score, mmse_score, grip_strength, open_eyes_result_id, closed_eyes_result_id, created_at) 
             VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, NOW())`,
            [testerId, currentStaff.id, institutionId, testDate, testStatus, mocaScore, mmseScore, gripStrength, openEyesResultId, closedEyesResultId]
        );

        const testRecordId = testRecordResult.insertId;

        // 计算AD风险值
        let adRiskValue = 0;
        if (closedEyesResultId) {
            try {
                // 查询闭眼测试结果数据
                const [closedEyesResult] = await query(
                    'SELECT theta_value, alpha_value, beta_value FROM test_results WHERE id = ?',
                    [closedEyesResultId]
                );

                if (closedEyesResult && closedEyesResult.theta_value !== null && 
                    closedEyesResult.alpha_value !== null && closedEyesResult.beta_value !== null) {
                    
                    // 计算脑电最终指标 = (Theta值/3 + Alpha值/3 + Beta值/3)
                    const brainwaveFinalIndex = (closedEyesResult.theta_value / 3.0) + 
                                              (closedEyesResult.alpha_value / 3.0) + 
                                              (closedEyesResult.beta_value / 3.0);
                    
                    // 计算量表分数 = (MMSE分数/30 * 100% + MoCA分数/30 * 100%) / 2
                    let scaleScore = 0;
                    let scaleCount = 0;
                    
                    if (mmseScore !== null && mmseScore !== undefined) {
                        scaleScore += (mmseScore / 30.0) * 100.0;
                        scaleCount++;
                    }
                    
                    if (mocaScore !== null && mocaScore !== undefined) {
                        scaleScore += (mocaScore / 30.0) * 100.0;
                        scaleCount++;
                    }
                    
                    const averageScaleScore = scaleCount > 0 ? scaleScore / scaleCount : 0;
                    
                    // 计算AD风险指数 = (脑电最终指标/2 + 量表分数/2)
                    adRiskValue = (brainwaveFinalIndex / 2.0) + (averageScaleScore / 2.0);
                }
            } catch (error) {
                console.error('计算AD风险值错误:', error);
            }
        }

        // 更新测试记录，保存AD风险值
        if (adRiskValue > 0) {
            await query(
                'UPDATE test_records SET ad_risk_value = ? WHERE id = ?',
                [adRiskValue, testRecordId]
            );
        }

        // 注意：睁眼和闭眼的测试结果会在文件上传时创建，并通过brainwave_data_id关联
        // 这里需要后续的逻辑来更新test_records表中的open_eyes_result_id和closed_eyes_result_id
        res.json({
            success: true,
            message: '测试记录保存成功',
            data: {
                testRecordId,
                testerId,
                testerName,
                medicalStaffId,
                medicalStaffName,
                institutionId,
                testDate,
                testStatus
            }
        });

    } catch (error) {
        console.error('保存测试记录错误:', error);
        res.status(500).json({
            success: false,
            message: '服务器内部错误'
        });
    }
});

// 获取测试者的测试记录历史
router.post('/history', authenticateToken, async (req, res) => {
    try {
        const { testerId, page = 1, pageSize = 20 } = req.body;
        
        if (!testerId) {
            return res.status(400).json({
                success: false,
                message: '测试者ID为必填字段'
            });
        }

        // 确保page和pageSize是数字类型
        const pageNum = Math.max(1, parseInt(page) || 1);
        const pageSizeNum = Math.max(1, parseInt(pageSize) || 20);
        
        // 计算分页偏移量
        const offset = (pageNum - 1) * pageSizeNum;
        
        // 首先根据tester_id获取testers表的id
        const [testerResult] = await query(
            'SELECT id FROM testers WHERE tester_id = ?',
            [testerId]
        );
        
        if (!testerResult) {
            return res.status(404).json({
                success: false,
                message: '测试者不存在'
            });
        }
        
        const testerDbId = testerResult.id;
        
        // 获取总记录数
        const [countResult] = await query(
            'SELECT COUNT(*) as total FROM test_records WHERE tester_id = ?',
            [testerDbId]
        );
        
        const totalCount = countResult.total;
        
        // 获取分页数据 - 使用字符串拼接避免 LIMIT/OFFSET 参数类型问题
        const records = await query(
            `SELECT 
                tr.id,
                tr.tester_id,
                tr.medical_staff_id,
                tr.institution_id,
                tr.test_start_time,
                tr.test_status,
                tr.moca_score,
                tr.mmse_score,
                tr.grip_strength,
                tr.ad_risk_value,
                tr.brain_age,
                tr.open_eyes_result_id,
                tr.closed_eyes_result_id,
                tr.created_at,
                ms.name as medical_staff_name,
                i.institution_name
             FROM test_records tr
             LEFT JOIN medical_staff ms ON tr.medical_staff_id = ms.id
             LEFT JOIN institutions i ON tr.institution_id = i.id
             WHERE tr.tester_id = ?
             ORDER BY tr.created_at DESC
             LIMIT ${pageSizeNum} OFFSET ${offset}`,
            [testerDbId]
        );
        
        res.json({
            success: true,
            message: '获取测试历史成功',
            data: {
                records: records,
                totalCount: totalCount,
                currentPage: pageNum,
                pageSize: pageSizeNum,
                totalPages: Math.ceil(totalCount / pageSizeNum)
            }
        });
        
    } catch (error) {
        console.error('获取测试历史错误:', error);
        res.status(500).json({
            success: false,
            message: '服务器内部错误'
        });
    }
});

// 关联测试记录和睁眼/闭眼测试结果
router.post('/link-results', authenticateToken, async (req, res) => {
    try {
        const { testRecordId, openEyesResultId, closedEyesResultId } = req.body;

        // 验证必填字段
        if (!testRecordId) {
            return res.status(400).json({
                success: false,
                message: '测试记录ID为必填字段'
            });
        }

        // 更新测试记录，关联睁眼和闭眼的测试结果
        const updateSql = `
            UPDATE test_records 
            SET open_eyes_result_id = ?, closed_eyes_result_id = ?, updated_at = NOW()
            WHERE id = ?
        `;

        await query(updateSql, [
            openEyesResultId || null,
            closedEyesResultId || null,
            testRecordId
        ]);

        console.log('测试记录关联更新成功');

        res.json({
            success: true,
            message: '测试记录关联成功',
            data: {
                testRecordId,
                openEyesResultId,
                closedEyesResultId
            }
        });

    } catch (error) {
        console.error('关联测试记录错误:', error);
        res.status(500).json({
            success: false,
            message: '服务器内部错误'
        });
    }
});

// 获取测试记录详细信息（用于生成报告）
router.get('/:recordId/report', authenticateToken, async (req, res) => {
    try {
        const { recordId } = req.params;
        
        if (!recordId) {
            return res.status(400).json({
                success: false,
                message: '测试记录ID为必填字段'
            });
        }

        // 获取测试记录基本信息
        const [testRecord] = await query(
            `SELECT 
                tr.id,
                tr.tester_id,
                tr.medical_staff_id,
                tr.institution_id,
                tr.test_start_time,
                tr.test_status,
                tr.moca_score,
                tr.mmse_score,
                tr.grip_strength,
                tr.ad_risk_value,
                tr.brain_age,
                tr.open_eyes_result_id,
                tr.closed_eyes_result_id,
                tr.created_at,
                ms.name as medical_staff_name,
                i.institution_name,
                t.name as tester_name,
                t.tester_id as tester_external_id
             FROM test_records tr
             LEFT JOIN medical_staff ms ON tr.medical_staff_id = ms.id
             LEFT JOIN institutions i ON tr.institution_id = i.id
             LEFT JOIN testers t ON tr.tester_id = t.id
             WHERE tr.id = ?`,
            [recordId]
        );

        if (!testRecord) {
            return res.status(404).json({
                success: false,
                message: '测试记录不存在'
            });
        }

        // 获取闭眼测试结果数据
        let closedEyesResult = null;
        if (testRecord.closed_eyes_result_id) {
            [closedEyesResult] = await query(
                'SELECT theta_value, alpha_value, beta_value, result, created_at FROM test_results WHERE id = ?',
                [testRecord.closed_eyes_result_id]
            );
        }

        // 获取睁眼测试结果数据
        let openEyesResult = null;
        if (testRecord.open_eyes_result_id) {
            [openEyesResult] = await query(
                'SELECT theta_value, alpha_value, beta_value, result, created_at FROM test_results WHERE id = ?',
                [testRecord.open_eyes_result_id]
            );
        }

        res.json({
            success: true,
            message: '获取测试记录详细信息成功',
            data: {
                testRecord,
                closedEyesResult,
                openEyesResult
            }
        });

    } catch (error) {
        console.error('获取测试记录详细信息错误:', error);
        res.status(500).json({
            success: false,
            message: '服务器内部错误'
        });
    }
});

// 获取单个测试结果数据
router.get('/result/:resultId', authenticateToken, async (req, res) => {
    try {
        const { resultId } = req.params;
        
        if (!resultId) {
            return res.status(400).json({
                success: false,
                message: '测试结果ID为必填字段'
            });
        }

        // 获取测试结果数据
        const [testResult] = await query(
            'SELECT theta_value, alpha_value, beta_value, result, created_at FROM test_results WHERE id = ?',
            [resultId]
        );

        if (!testResult) {
            return res.status(404).json({
                success: false,
                message: '测试结果不存在'
            });
        }

        res.json({
            success: true,
            message: '获取测试结果成功',
            data: testResult
        });

    } catch (error) {
        console.error('获取测试结果错误:', error);
        res.status(500).json({
            success: false,
            message: '服务器内部错误'
        });
    }
});

module.exports = router;

