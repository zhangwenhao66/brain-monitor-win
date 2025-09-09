const express = require('express');
const { query, transaction } = require('../config/database');
const { authenticateToken, authenticateTesterAccess } = require('../middleware/auth');

const router = express.Router();

// 获取当前工作人员的测试者列表
router.get('/my-testers', authenticateToken, async (req, res) => {
    try {
        const { search } = req.query;
        const medicalStaffId = req.user.id;

        let sql = `
            SELECT t.id, t.tester_id, t.name, t.age, t.gender, t.phone, 
                   t.created_at, t.updated_at
            FROM testers t
            WHERE t.medical_staff_id = ?
        `;
        let params = [medicalStaffId];

        // 如果有搜索关键词，添加搜索条件
        if (search && search.trim()) {
            sql += ` AND (t.tester_id LIKE ? OR t.name LIKE ?)`;
            const searchTerm = `%${search.trim()}%`;
            params.push(searchTerm, searchTerm);
        }

        sql += ` ORDER BY t.created_at DESC`;

        const testers = await query(sql, params);

        // 转换性别值：英文转中文（用于返回给前端）
        const genderMapReverse = {
            'Male': '男',
            'Female': '女',
            'Other': '其他'
        };
        
        const testersWithChineseGender = testers.map(tester => ({
            ...tester,
            gender: genderMapReverse[tester.gender] || tester.gender
        }));

        res.json({
            success: true,
            data: testersWithChineseGender
        });

    } catch (error) {
        console.error('获取测试者列表错误:', error);
        res.status(500).json({
            success: false,
            message: '服务器内部错误'
        });
    }
});

// 添加新测试者
router.post('/', authenticateToken, async (req, res) => {
    try {
        const { testerId, name, age, gender, phone, medicalStaffId, institutionId } = req.body;
        const currentMedicalStaffId = req.user.id;
        const currentInstitutionId = req.user.institution_id;

        // 验证必填字段
        if (!testerId || !name) {
            return res.status(400).json({
                success: false,
                message: '测试者ID和姓名为必填字段'
            });
        }

        // 转换性别值：中文转英文
        let normalizedGender = null;
        if (gender) {
            const genderMap = {
                '男': 'Male',
                '女': 'Female',
                '其他': 'Other',
                'Male': 'Male',
                'Female': 'Female',
                'Other': 'Other'
            };
            normalizedGender = genderMap[gender] || null;
        }

        // 检查测试者ID是否在当前工作人员下已存在
        const [existingTester] = await query(
            'SELECT id, medical_staff_id, institution_id FROM testers WHERE tester_id = ? AND medical_staff_id = ?',
            [testerId, currentMedicalStaffId]
        );

        if (existingTester) {
            return res.status(400).json({
                success: false,
                message: `测试者ID ${testerId} 在当前工作人员下已存在`
            });
        }

        // 插入新测试者
        const result = await query(
            `INSERT INTO testers 
             (tester_id, name, age, gender, phone, medical_staff_id, institution_id) 
             VALUES (?, ?, ?, ?, ?, ?, ?)`,
            [testerId, name, age, normalizedGender, phone, currentMedicalStaffId, currentInstitutionId]
        );

        // 获取新插入的测试者信息
        const [newTester] = await query(
            'SELECT id, tester_id, name, age, gender, phone, medical_staff_id, institution_id, created_at FROM testers WHERE id = ?',
            [result.insertId]
        );

        // 转换性别值：英文转中文（用于返回给前端）
        const genderMapReverse = {
            'Male': '男',
            'Female': '女',
            'Other': '其他'
        };
        const displayGender = genderMapReverse[newTester.gender] || newTester.gender;

        res.status(201).json({
            success: true,
            message: '测试者添加成功',
            data: {
                id: newTester.id,
                testerId: newTester.tester_id,
                name: newTester.name,
                age: newTester.age,
                gender: displayGender,
                phone: newTester.phone,
                medicalStaffId: newTester.medical_staff_id,
                institutionId: newTester.institution_id,
                createdAt: newTester.created_at
            }
        });

    } catch (error) {
        console.error('添加测试者错误:', error);
        res.status(500).json({
            success: false,
            message: '服务器内部错误'
        });
    }
});

// 获取测试者列表（分页）
router.post('/list', authenticateToken, async (req, res) => {
    try {
        const { medicalStaffId, institutionId, page = 1, pageSize = 20 } = req.body;
        const currentMedicalStaffId = req.user.id;
        const currentInstitutionId = req.user.institution_id;

        // 确保所有参数都是正确的类型
        const medicalStaffIdInt = Number(medicalStaffId);
        const institutionIdInt = Number(institutionId);
        const pageInt = Number(page);
        const pageSizeInt = Number(pageSize);
        const offset = (pageInt - 1) * pageSizeInt;

        // 验证参数有效性
        if (isNaN(medicalStaffIdInt) || isNaN(institutionIdInt) || isNaN(pageInt) || isNaN(pageSizeInt)) {
            return res.status(400).json({
                success: false,
                message: '无效的参数类型'
            });
        }

        // 验证权限：只能查看自己机构的测试者
        if (currentInstitutionId !== institutionIdInt) {
            return res.status(403).json({
                success: false,
                message: '无权访问其他机构的测试者'
            });
        }

        // 获取测试者总数
        const [countResult] = await query(
            'SELECT COUNT(*) as total FROM testers WHERE medical_staff_id = ? AND institution_id = ?',
            [medicalStaffIdInt, institutionIdInt]
        );

        const total = countResult.total;

        // 获取分页的测试者列表
        
        
        // 先尝试不使用分页的简单查询
        const testers = await query(
            `SELECT id, tester_id, name, age, gender, phone, medical_staff_id, institution_id, created_at, updated_at
             FROM testers 
             WHERE medical_staff_id = ? AND institution_id = ?
             ORDER BY created_at DESC`,
            [medicalStaffIdInt, institutionIdInt]
        );

        // 转换性别值：英文转中文（用于返回给前端）
        const genderMapReverse = {
            'Male': '男',
            'Female': '女',
            'Other': '其他'
        };
        
        const testersWithChineseGender = testers.map(tester => ({
            ...tester,
            gender: genderMapReverse[tester.gender] || tester.gender
        }));

        res.json({
            success: true,
            data: {
                testers: testersWithChineseGender,
                totalCount: total,
                page: pageInt,
                pageSize: pageSizeInt,
                totalPages: Math.ceil(total / pageSizeInt)
            }
        });

    } catch (error) {
        console.error('获取测试者列表错误:', error);
        res.status(500).json({
            success: false,
            message: '服务器内部错误'
        });
    }
});

// 检查测试者ID是否已存在
router.get('/:testerId/exists', authenticateToken, async (req, res) => {
    try {
        const { testerId } = req.params;
        const currentInstitutionId = req.user.institution_id;

        // 检查测试者ID是否已存在
        const [existingTester] = await query(
            'SELECT id FROM testers WHERE tester_id = ? AND institution_id = ?',
            [testerId, currentInstitutionId]
        );

        res.json({
            success: true,
            data: {
                exists: !!existingTester,
                testerId: testerId
            }
        });

    } catch (error) {
        console.error('检查测试者ID存在错误:', error);
        res.status(500).json({
            success: false,
            message: '服务器内部错误'
        });
    }
});

module.exports = router;
