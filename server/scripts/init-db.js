#!/usr/bin/env node

/**
 * 数据库初始化脚本
 * 用于创建数据库和表结构，插入初始数据
 */

const mysql = require('mysql2/promise');
const fs = require('fs');
const path = require('path');
require('dotenv').config();

const dbConfig = {
    host: process.env.DB_HOST || 'localhost',
    port: process.env.DB_PORT || 3306,
    user: process.env.DB_USER || 'root',
    password: process.env.DB_PASSWORD || '',
    charset: 'utf8mb4',
    timezone: '+08:00'
};

async function initDatabase() {
    let connection;
    
    try {
        console.log('🚀 开始初始化数据库...');
        
        // 连接MySQL服务器（不指定数据库）
        connection = await mysql.createConnection({
            ...dbConfig,
            multipleStatements: true
        });
        
        console.log('✅ 成功连接到MySQL服务器');
        
        // 创建数据库
        console.log('📝 创建数据库...');
        await connection.execute(`
            CREATE DATABASE IF NOT EXISTS brain_mirror 
            CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci
        `);
        
        console.log('✅ 数据库创建成功');
        
        // 关闭当前连接，重新连接到指定数据库
        await connection.end();
        
        // 重新连接到指定数据库
        connection = await mysql.createConnection({
            ...dbConfig,
            database: 'brain_mirror',
            multipleStatements: true
        });
        
        console.log('✅ 成功连接到 brain_mirror 数据库');
        
        // 读取并执行完整的 schema.sql 文件
        console.log('📋 创建表结构...');
        const schemaPath = path.join(__dirname, '..', 'database', 'schema.sql');
        const schemaContent = fs.readFileSync(schemaPath, 'utf8');
        
        console.log(`📄 Schema文件大小: ${schemaContent.length} 字符`);
        
        // 手动分离不同类型的SQL语句
        const lines = schemaContent.split('\n');
        const tableStatements = [];
        const indexStatements = [];
        const insertStatements = [];
        
        let currentStatement = '';
        let inComment = false;
        
        for (const line of lines) {
            const trimmedLine = line.trim();
            
            // 跳过注释行和空行
            if (trimmedLine.startsWith('--') || trimmedLine === '') {
                continue;
            }
            
            // 跳过CREATE DATABASE和USE语句
            if (trimmedLine.toUpperCase().startsWith('CREATE DATABASE') || 
                trimmedLine.toUpperCase().startsWith('USE')) {
                continue;
            }
            
            // 累积当前语句
            currentStatement += line + '\n';
            
            // 如果行以分号结尾，说明语句结束
            if (trimmedLine.endsWith(';')) {
                const fullStatement = currentStatement.trim();
                if (fullStatement) {
                    if (fullStatement.toUpperCase().includes('CREATE TABLE')) {
                        tableStatements.push(fullStatement);
                    } else if (fullStatement.toUpperCase().includes('CREATE INDEX')) {
                        indexStatements.push(fullStatement);
                    } else if (fullStatement.toUpperCase().includes('INSERT INTO')) {
                        insertStatements.push(fullStatement);
                    }
                }
                currentStatement = '';
            }
        }
        
        console.log(`📊 找到 ${tableStatements.length} 个表创建语句`);
        console.log(`📊 找到 ${indexStatements.length} 个索引创建语句`);
        console.log(`📊 找到 ${insertStatements.length} 个数据插入语句`);
        
        // 先执行表创建语句
        console.log('📋 创建表结构...');
        
        // 先删除现有表（如果存在）
        console.log('🗑️  清理现有表...');
        const dropOrder = [
            'test_results',
            'brainwave_data', 
            'test_records',
            'testers',
            'medical_staff',
            'institutions'
        ];
        
        for (const tableName of dropOrder) {
            try {
                await connection.execute(`DROP TABLE IF EXISTS ${tableName}`);
                console.log(`✅ 删除表: ${tableName}`);
            } catch (error) {
                console.log(`ℹ️  表 ${tableName} 不存在或删除失败: ${error.message}`);
            }
        }
        
        // 创建新表
        for (const statement of tableStatements) {
            try {
                await connection.execute(statement);
                console.log(`✅ 执行表创建: ${statement.substring(0, 50)}...`);
            } catch (error) {
                console.error(`❌ 执行表创建失败: ${statement.substring(0, 50)}...`);
                console.error('错误详情:', error.message);
                throw error;
            }
        }
        
        // 再执行数据插入语句
        console.log('📋 插入初始数据...');
        for (const statement of insertStatements) {
            try {
                await connection.execute(statement);
                console.log(`✅ 执行数据插入: ${statement.substring(0, 50)}...`);
            } catch (error) {
                console.error(`❌ 执行数据插入失败: ${statement.substring(0, 50)}...`);
                console.error('错误详情:', error.message);
                throw error;
            }
        }
        
        // 最后执行索引创建语句
        console.log('📋 创建索引...');
        for (const statement of indexStatements) {
            try {
                await connection.execute(statement);
                console.log(`✅ 执行索引创建: ${statement.substring(0, 50)}...`);
            } catch (error) {
                console.error(`❌ 执行索引创建失败: ${statement.substring(0, 50)}...`);
                console.error('错误详情:', error.message);
                throw error;
            }
        }
        
        console.log('✅ 表结构创建成功');
        
        // 检查是否已有初始数据
        const [institutionCount] = await connection.execute('SELECT COUNT(*) as count FROM institutions');
        
        if (institutionCount[0].count === 0) {
            console.log('📊 插入初始数据...');
            
            // 插入默认机构（密码：123456）
            const bcrypt = require('bcryptjs');
            const hashedPassword1 = await bcrypt.hash('123456', 10);
            const hashedPassword2 = await bcrypt.hash('2', 10);
            
            await connection.execute(`
                INSERT INTO institutions (institution_id, institution_name, password, contact_person, contact_phone, address) 
                VALUES (?, ?, ?, ?, ?, ?)
            `, ['默认机构', '默认机构名称', hashedPassword1, '管理员', '13800138000', '默认地址']);
            
            await connection.execute(`
                INSERT INTO institutions (institution_id, institution_name, password, contact_person, contact_phone, address) 
                VALUES (?, ?, ?, ?, ?, ?)
            `, ['2', '测试机构2', hashedPassword2, '测试联系人', '13800138001', '测试地址']);
            
            console.log('✅ 初始数据插入成功');
        } else {
            console.log('ℹ️  数据库已有初始数据，跳过插入');
        }
        
        console.log('🎉 数据库初始化完成！');
        
    } catch (error) {
        console.error('❌ 数据库初始化失败:', error.message);
        if (error.code) {
            console.error('错误代码:', error.code);
        }
        if (error.sql) {
            console.error('SQL语句:', error.sql);
        }
        process.exit(1);
    } finally {
        if (connection) {
            await connection.end();
        }
    }
}

// 如果直接运行此脚本
if (require.main === module) {
    initDatabase();
}

module.exports = { initDatabase };
