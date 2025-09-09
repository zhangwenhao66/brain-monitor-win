-- Create database
CREATE DATABASE IF NOT EXISTS brain_mirror CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
USE brain_mirror;

-- Institutions table
CREATE TABLE institutions (
    id INT PRIMARY KEY AUTO_INCREMENT,
    institution_id VARCHAR(100) UNIQUE NOT NULL COMMENT 'Institution ID',
    institution_name VARCHAR(200) NOT NULL COMMENT 'Institution Name',
    password VARCHAR(255) COMMENT 'Institution Password (Optional)',
    contact_person VARCHAR(100) COMMENT 'Contact Person',
    contact_phone VARCHAR(20) COMMENT 'Contact Phone',
    address TEXT COMMENT 'Institution Address',
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NULL DEFAULT NULL
);

-- Medical staff table
CREATE TABLE medical_staff (
    id INT PRIMARY KEY AUTO_INCREMENT,
    staff_id VARCHAR(100) NOT NULL COMMENT 'Staff ID',
    name VARCHAR(100) NOT NULL COMMENT 'Name',
    account VARCHAR(100) NOT NULL COMMENT 'Login Account',
    password VARCHAR(255) NOT NULL COMMENT 'Password (Encrypted)',
    phone VARCHAR(20) COMMENT 'Contact Phone',
    department VARCHAR(100) COMMENT 'Department',
    position VARCHAR(100) COMMENT 'Position',
    institution_id INT NOT NULL COMMENT 'Institution ID',
    is_active BOOLEAN DEFAULT TRUE COMMENT 'Is Active',
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NULL DEFAULT NULL,
    FOREIGN KEY (institution_id) REFERENCES institutions(id) ON DELETE CASCADE,
    UNIQUE KEY uk_staff_id_institution (staff_id, institution_id),
    UNIQUE KEY uk_account_institution (account, institution_id)
);

-- Testers table
CREATE TABLE testers (
    id INT PRIMARY KEY AUTO_INCREMENT,
    tester_id VARCHAR(100) NOT NULL COMMENT 'Tester ID',
    name VARCHAR(100) NOT NULL COMMENT 'Name',
    age VARCHAR(10) COMMENT 'Age',
    gender ENUM('Male', 'Female', 'Other') COMMENT 'Gender',
    phone VARCHAR(20) COMMENT 'Contact Phone',
    medical_staff_id INT NOT NULL COMMENT 'Responsible Medical Staff ID',
    institution_id INT NOT NULL COMMENT 'Institution ID',
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NULL DEFAULT NULL,
    FOREIGN KEY (medical_staff_id) REFERENCES medical_staff(id) ON DELETE CASCADE,
    FOREIGN KEY (institution_id) REFERENCES institutions(id) ON DELETE CASCADE,
    UNIQUE KEY uk_tester_id_institution (tester_id, institution_id)
);

-- Test results table
CREATE TABLE test_results (
    id INT PRIMARY KEY AUTO_INCREMENT,
    csv_file_path VARCHAR(500) NOT NULL COMMENT 'CSV File Path',
    theta_value DECIMAL(5,2) COMMENT 'Theta Value',
    alpha_value DECIMAL(5,2) COMMENT 'Alpha Value',
    beta_value DECIMAL(5,2) COMMENT 'Beta Value',
    result ENUM('Open Eyes', 'Closed Eyes') NOT NULL COMMENT 'Result Type',
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Test records table
CREATE TABLE test_records (
    id INT PRIMARY KEY AUTO_INCREMENT,
    tester_id INT NOT NULL COMMENT 'Tester ID',
    medical_staff_id INT NOT NULL COMMENT 'Medical Staff ID',
    institution_id INT NOT NULL COMMENT 'Institution ID',
    test_start_time TIMESTAMP NULL DEFAULT NULL COMMENT 'Test Start Time',
    test_end_time TIMESTAMP NULL DEFAULT NULL COMMENT 'Test End Time',
    test_status ENUM('In Progress', 'Completed', 'Cancelled') DEFAULT 'In Progress' COMMENT 'Test Status',
    moca_score DECIMAL(5,2) COMMENT 'MoCA Score',
    mmse_score DECIMAL(5,2) COMMENT 'MMSE Score',
    grip_strength DECIMAL(8,2) COMMENT 'Grip Strength',
    ad_risk_value DECIMAL(5,2) COMMENT 'AD Risk Value',
    brain_age DECIMAL(5,2) COMMENT 'Brain Age',
    open_eyes_result_id INT NULL COMMENT 'Open Eyes Result ID',
    closed_eyes_result_id INT NULL COMMENT 'Closed Eyes Result ID',
    notes TEXT COMMENT 'Notes',
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NULL DEFAULT NULL,
    FOREIGN KEY (tester_id) REFERENCES testers(id) ON DELETE CASCADE,
    FOREIGN KEY (medical_staff_id) REFERENCES medical_staff(id) ON DELETE CASCADE,
    FOREIGN KEY (institution_id) REFERENCES institutions(id) ON DELETE CASCADE,
    FOREIGN KEY (open_eyes_result_id) REFERENCES test_results(id) ON DELETE SET NULL,
    FOREIGN KEY (closed_eyes_result_id) REFERENCES test_results(id) ON DELETE SET NULL
);

-- Create indexes
CREATE INDEX idx_medical_staff_institution ON medical_staff(institution_id);
CREATE INDEX idx_testers_medical_staff ON testers(medical_staff_id);
CREATE INDEX idx_testers_institution ON testers(institution_id);
CREATE INDEX idx_test_records_tester ON test_records(tester_id);
CREATE INDEX idx_test_records_medical_staff ON test_records(medical_staff_id);
CREATE INDEX idx_test_records_institution ON test_records(institution_id);
CREATE INDEX idx_test_records_open_eyes_result ON test_records(open_eyes_result_id);
CREATE INDEX idx_test_records_closed_eyes_result ON test_records(closed_eyes_result_id);