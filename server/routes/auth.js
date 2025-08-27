const express = require('express');
const bcrypt = require('bcryptjs');
const jwt = require('jsonwebtoken');
const { query, transaction } = require('../config/database');
const { authenticateToken } = require('../middleware/auth');
const { JWT_SECRET, JWT_EXPIRES_IN } = require('../config/jwt');

const router = express.Router();

// 机构登录
router.post('/institution/login', async (req, res) => {
    try {
        const { institutionId, password } = req.body;

        // 验证必填字段
        if (!institutionId || !password) {
            return res.status(400).json({
                success: false,
                message: '请填写机构ID和密码'
            });
        }

        // 查询机构信息
        const [institution] = await query(
            'SELECT id, institution_id, institution_name, password FROM institutions WHERE institution_id = ?',
            [institutionId]
        );

        if (!institution) {
            return res.status(404).json({
                success: false,
                message: '机构不存在'
            });
        }

        // 验证密码
        if (institution.password) {
            const isValidPassword = await bcrypt.compare(password, institution.password);
            if (!isValidPassword) {
                return res.status(401).json({
                    success: false,
                    message: '机构密码错误'
                });
            }
        } else {
            // 如果机构没有设置密码，拒绝登录
            return res.status(401).json({
                success: false,
                message: '该机构未设置密码，无法登录'
            });
        }

        // 登录成功
        return res.json({
            success: true,
            message: '机构登录成功',
            data: {
                institutionId: institution.institution_id,
                institutionName: institution.institution_name,
                institutionDbId: institution.id
            }
        });

    } catch (error) {
        console.error('机构登录错误:', error);
        res.status(500).json({
            success: false,
            message: '服务器内部错误'
            });
    }
});

// 医护人员注册
router.post('/medical-staff/register', async (req, res) => {
    try {
        const { 
            staffId, 
            name, 
            account, 
            password, 
            phone, 
            department, 
            position, 
            institutionId 
        } = req.body;

        // 验证必填字段
        if (!staffId || !name || !account || !password || !institutionId) {
            return res.status(400).json({
                success: false,
                message: '请填写所有必填字段'
            });
        }

        // 检查工号是否已存在
        const [existingStaffId] = await query(
            'SELECT id FROM medical_staff WHERE staff_id = ?',
            [staffId]
        );

        if (existingStaffId) {
            return res.status(400).json({
                success: false,
                message: '工号已存在'
            });
        }

        // 检查账号是否已存在
        const [existingAccount] = await query(
            'SELECT id FROM medical_staff WHERE account = ?',
            [account]
        );

        if (existingAccount) {
            return res.status(400).json({
                success: false,
                message: '账号已存在'
            });
        }

        // 验证机构是否存在
        const [institution] = await query(
            'SELECT id FROM institutions WHERE id = ?',
            [institutionId]
        );

        if (!institution) {
            return res.status(400).json({
                success: false,
                message: '机构不存在'
            });
        }

        // 加密密码
        const hashedPassword = await bcrypt.hash(password, 10);

        // 插入医护人员记录
        const result = await query(
            `INSERT INTO medical_staff 
             (staff_id, name, account, password, phone, department, position, institution_id) 
             VALUES (?, ?, ?, ?, ?, ?, ?, ?)`,
            [staffId, name, account, hashedPassword, phone, department, position, institutionId]
        );

        res.status(201).json({
            success: true,
            message: '医护人员注册成功',
            data: {
                id: result.insertId,
                staffId,
                name,
                account
            }
        });

    } catch (error) {
        console.error('医护人员注册错误:', error);
        res.status(500).json({
            success: false,
            message: '服务器内部错误'
        });
    }
});

// 医护人员登录
router.post('/medical-staff/login', async (req, res) => {
    try {
        const { account, password } = req.body;

        // 验证必填字段
        if (!account || !password) {
            return res.status(400).json({
                success: false,
                message: '请填写账号和密码'
            });
        }

        // 查询医护人员信息
        const [staff] = await query(
            `SELECT ms.id, ms.staff_id, ms.name, ms.account, ms.password, ms.phone, 
                    ms.department, ms.position, ms.institution_id, ms.is_active,
                    i.institution_id as institution_code, i.institution_name
             FROM medical_staff ms
             JOIN institutions i ON ms.institution_id = i.id
             WHERE ms.account = ?`,
            [account]
        );

        if (!staff) {
            return res.status(401).json({
                success: false,
                message: '账号不存在'
            });
        }

        if (!staff.is_active) {
            return res.status(401).json({
                success: false,
                message: '账号已被禁用'
            });
        }

        // 验证密码
        const isValidPassword = await bcrypt.compare(password, staff.password);
        if (!isValidPassword) {
            return res.status(401).json({
                success: false,
                message: '密码错误'
            });
        }

        // 生成JWT token
        const token = jwt.sign(
            { 
                userId: staff.id, 
                account: staff.account,
                institutionId: staff.institution_id 
            },
            JWT_SECRET,
            { expiresIn: JWT_EXPIRES_IN }
        );

        // 返回登录成功信息
        res.json({
            success: true,
            message: '登录成功',
            data: {
                token,
                user: {
                    id: staff.id,
                    staffId: staff.staff_id,
                    name: staff.name,
                    account: staff.account,
                    phone: staff.phone,
                    department: staff.department,
                    position: staff.position,
                    institutionId: staff.institution_id,
                    institutionCode: staff.institution_code,
                    institutionName: staff.institution_name
                }
            }
        });

    } catch (error) {
        console.error('医护人员登录错误:', error);
        res.status(500).json({
            success: false,
            message: '服务器内部错误'
        });
    }
});

// 获取当前登录用户信息
router.get('/me', authenticateToken, async (req, res) => {
    try {
        const [staff] = await query(
            `SELECT ms.id, ms.staff_id, ms.name, ms.account, ms.phone, 
                    ms.department, ms.position, ms.institution_id, ms.is_active,
                    i.institution_id as institution_code, i.institution_name
             FROM medical_staff ms
             JOIN institutions i ON ms.institution_id = i.id
             WHERE ms.id = ?`,
            [req.user.id]
        );

        if (!staff) {
            return res.status(404).json({
                success: false,
                message: '用户不存在'
            });
        }

        res.json({
            success: true,
            data: {
                id: staff.id,
                staffId: staff.staff_id,
                name: staff.name,
                account: staff.account,
                phone: staff.phone,
                department: staff.department,
                position: staff.position,
                institutionId: staff.institution_id,
                institutionCode: staff.institution_code,
                institutionName: staff.institution_name
            }
        });

    } catch (error) {
        console.error('获取用户信息错误:', error);
        res.status(500).json({
            success: false,
            message: '服务器内部错误'
        });
    }
});

// 退出登录
router.post('/logout', authenticateToken, (req, res) => {
    // JWT是无状态的，客户端删除token即可
    // 这里可以记录退出日志或加入黑名单
    res.json({
        success: true,
        message: '退出登录成功'
    });
});

module.exports = router;

