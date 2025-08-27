#!/usr/bin/env node

/**
 * 生成密码的bcrypt加密版本
 * 用于手动创建加密密码
 */

const bcrypt = require('bcryptjs');

async function generatePassword() {
    const password = '2';
    const hashedPassword = await bcrypt.hash(password, 10);
    
    console.log(`密码: ${password}`);
    console.log(`加密后: ${hashedPassword}`);
    
    // 生成SQL语句
    console.log('\nSQL语句:');
    console.log(`INSERT INTO institutions (institution_id, institution_name, password, contact_person, contact_phone, address) VALUES ('2', '测试机构2', '${hashedPassword}', '测试联系人', '13800138001', '测试地址');`);
}

generatePassword().catch(console.error);
