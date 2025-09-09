const jwt = require('jsonwebtoken');
const { query } = require('../config/database');
const { JWT_SECRET } = require('../config/jwt');

// 验证JWT token
const authenticateToken = async (req, res, next) => {
    const authHeader = req.headers['authorization'];
    const token = authHeader && authHeader.split(' ')[1]; // Bearer TOKEN

    if (!token) {
        return res.status(401).json({ 
            success: false, 
            message: '访问令牌缺失' 
        });
    }



    try {
        const decoded = jwt.verify(token, JWT_SECRET);
        
        // 验证用户是否仍然存在于数据库中
        const [user] = await query(
            'SELECT id, account, staff_id, name, institution_id FROM medical_staff WHERE id = ? AND is_active = TRUE',
            [decoded.userId]
        );

        if (!user) {
            return res.status(401).json({ 
                success: false, 
                message: '用户不存在或已被禁用' 
            });
        }

        req.user = user;
        next();
    } catch (error) {
        if (error.name === 'TokenExpiredError') {
            return res.status(401).json({ 
                success: false, 
                message: '访问令牌已过期' 
            });
        }
        
        return res.status(403).json({ 
            success: false, 
            message: '无效的访问令牌' 
        });
    }
};

// 验证机构访问权限
const authenticateInstitution = async (req, res, next) => {
    try {
        const { institutionId } = req.params;
        
        if (!req.user) {
            return res.status(401).json({ 
                success: false, 
                message: '用户未认证' 
            });
        }

        // 检查用户是否属于指定机构
        if (req.user.institution_id.toString() !== institutionId) {
            return res.status(403).json({ 
                success: false, 
                message: '无权访问该机构数据' 
            });
        }

        next();
    } catch (error) {
        console.error('机构权限验证错误:', error);
        return res.status(500).json({ 
            success: false, 
            message: '权限验证失败' 
        });
    }
};

// 验证测试者访问权限
const authenticateTesterAccess = async (req, res, next) => {
    try {
        const { testerId } = req.params;
        
        if (!req.user) {
            return res.status(401).json({ 
                success: false, 
                message: '用户未认证' 
            });
        }

        // 检查测试者是否属于当前工作人员
        const [tester] = await query(
            'SELECT id FROM testers WHERE id = ? AND medical_staff_id = ?',
            [testerId, req.user.id]
        );

        if (!tester) {
            return res.status(403).json({ 
                success: false, 
                message: '无权访问该测试者数据' 
            });
        }

        next();
    } catch (error) {
        console.error('测试者权限验证错误:', error);
        return res.status(500).json({ 
            success: false, 
            message: '权限验证失败' 
        });
    }
};

module.exports = {
    authenticateToken,
    authenticateInstitution,
    authenticateTesterAccess
};

