-- 创建数据库
CREATE DATABASE IF NOT EXISTS brain_monitor CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
USE brain_monitor;

-- 机构表
CREATE TABLE institutions (
    id INT PRIMARY KEY AUTO_INCREMENT,
    institution_id VARCHAR(100) UNIQUE NOT NULL COMMENT '机构ID',
    institution_name VARCHAR(200) NOT NULL COMMENT '机构名称',
    password VARCHAR(255) COMMENT '机构密码（可选）',
    contact_person VARCHAR(100) COMMENT '联系人',
    contact_phone VARCHAR(20) COMMENT '联系电话',
    address TEXT COMMENT '机构地址',
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
);

-- 医护人员表
CREATE TABLE medical_staff (
    id INT PRIMARY KEY AUTO_INCREMENT,
    staff_id VARCHAR(100) UNIQUE NOT NULL COMMENT '工号',
    name VARCHAR(100) NOT NULL COMMENT '姓名',
    account VARCHAR(100) UNIQUE NOT NULL COMMENT '登录账号',
    password VARCHAR(255) NOT NULL COMMENT '密码（加密）',
    phone VARCHAR(20) COMMENT '联系电话',
    department VARCHAR(100) COMMENT '科室',
    position VARCHAR(100) COMMENT '职位',
    institution_id INT NOT NULL COMMENT '所属机构ID',
    is_active BOOLEAN DEFAULT TRUE COMMENT '是否激活',
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    FOREIGN KEY (institution_id) REFERENCES institutions(id) ON DELETE CASCADE
);

-- 测试者表
CREATE TABLE testers (
    id INT PRIMARY KEY AUTO_INCREMENT,
    tester_id VARCHAR(100) UNIQUE NOT NULL COMMENT '测试者ID',
    name VARCHAR(100) NOT NULL COMMENT '姓名',
    age VARCHAR(10) COMMENT '年龄',
    gender ENUM('男', '女', '其他') COMMENT '性别',
    phone VARCHAR(20) COMMENT '联系电话',
    medical_staff_id INT NOT NULL COMMENT '负责医护人员ID',
    institution_id INT NOT NULL COMMENT '所属机构ID',
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    FOREIGN KEY (medical_staff_id) REFERENCES medical_staff(id) ON DELETE CASCADE,
    FOREIGN KEY (institution_id) REFERENCES institutions(id) ON DELETE CASCADE
);

-- 测试记录表
CREATE TABLE test_records (
    id INT PRIMARY KEY AUTO_INCREMENT,
    tester_id INT NOT NULL COMMENT '测试者ID',
    medical_staff_id INT NOT NULL COMMENT '医护人员ID',
    institution_id INT NOT NULL COMMENT '机构ID',
    test_start_time TIMESTAMP NOT NULL COMMENT '测试开始时间',
    test_end_time TIMESTAMP COMMENT '测试结束时间',
    test_status ENUM('进行中', '已完成', '已取消') DEFAULT '进行中' COMMENT '测试状态',
    moca_score DECIMAL(5,2) COMMENT 'MoCA评分',
    mmse_score DECIMAL(5,2) COMMENT 'MMSE评分',
    grip_strength DECIMAL(8,2) COMMENT '握力值',
    ad_risk_value DECIMAL(5,2) COMMENT 'AD风险值',
    brain_age DECIMAL(5,2) COMMENT '大脑年龄',
    open_eyes_result_id INT NULL COMMENT '睁眼测试结果ID',
    closed_eyes_result_id INT NULL COMMENT '闭眼测试结果ID',
    notes TEXT COMMENT '备注',
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    FOREIGN KEY (tester_id) REFERENCES testers(id) ON DELETE CASCADE,
    FOREIGN KEY (medical_staff_id) REFERENCES medical_staff(id) ON DELETE CASCADE,
    FOREIGN KEY (institution_id) REFERENCES institutions(id) ON DELETE CASCADE
);

-- 测试结果表
CREATE TABLE test_results (
    id INT PRIMARY KEY AUTO_INCREMENT,
    csv_file_path VARCHAR(500) NOT NULL COMMENT 'CSV文件路径',
    theta_value DECIMAL(5,2) COMMENT 'Theta值',
    alpha_value DECIMAL(5,2) COMMENT 'Alpha值',
    beta_value DECIMAL(5,2) COMMENT 'Beta值',
    result ENUM('睁眼', '闭眼') NOT NULL COMMENT '结果类型',
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- 添加外键约束
ALTER TABLE test_records 
ADD CONSTRAINT fk_test_records_open_eyes_result 
FOREIGN KEY (open_eyes_result_id) REFERENCES test_results(id) ON DELETE SET NULL;

ALTER TABLE test_records 
ADD CONSTRAINT fk_test_records_closed_eyes_result 
FOREIGN KEY (closed_eyes_result_id) REFERENCES test_results(id) ON DELETE SET NULL;

-- 插入默认机构数据（密码：123456）
INSERT INTO institutions (institution_id, institution_name, password, contact_person, contact_phone, address) VALUES
('默认机构', '默认机构名称', '$2a$10$92IXUNpkjO0rOQ5byMi.Ye4oKoEa3Ro9llC/.og/at2uheWG/igi.', '管理员', '13800138000', '默认地址');

-- 插入默认医护人员数据（密码：123456）
INSERT INTO medical_staff (staff_id, name, account, password, phone, department, position, institution_id) VALUES
('001', '测试医生', '1', '$2a$10$92IXUNpkjO0rOQ5byMi.Ye4oKoEa3Ro9llC/.og/at2uheWG/igi.', '13800138001', '神经科', '主治医师', 1),
('002', '张医生', 'doctor001', '$2a$10$92IXUNpkjO0rOQ5byMi.Ye4oKoEa3Ro9llC/.og/at2uheWG/igi.', '13800138002', '神经科', '副主任医师', 1),
('003', '李护士', 'nurse001', '$2a$10$92IXUNpkjO0rOQ5byMi.Ye4oKoEa3Ro9llC/.og/at2uheWG/igi.', '13800138003', '护理部', '主管护师', 1);

-- 插入默认测试者数据
INSERT INTO testers (tester_id, name, age, gender, phone, medical_staff_id, institution_id) VALUES
('001', '张三', '25', '男', '13800138001', 1, 1),
('002', '李四', '30', '女', '13800138002', 1, 1),
('003', '王五', '45', '男', '13800138003', 1, 1),
('004', '赵六', '35', '女', '13800138004', 1, 1),
('005', '孙七', '28', '男', '13800138005', 2, 1),
('006', '周八', '32', '女', '13800138006', 2, 1),
('007', '吴九', '40', '男', '13800138007', 3, 1);

-- 创建索引
CREATE INDEX idx_medical_staff_institution ON medical_staff(institution_id);
CREATE INDEX idx_testers_medical_staff ON testers(medical_staff_id);
CREATE INDEX idx_testers_institution ON testers(institution_id);
CREATE INDEX idx_test_records_tester ON test_records(tester_id);
CREATE INDEX idx_test_records_medical_staff ON test_records(medical_staff_id);
CREATE INDEX idx_test_records_institution ON test_records(institution_id);
CREATE INDEX idx_test_records_open_eyes_result ON test_records(open_eyes_result_id);
CREATE INDEX idx_test_records_closed_eyes_result ON test_records(closed_eyes_result_id);

