-- Add report_number field to test_records table
-- This migration script adds the report number field to existing databases
-- Note: This will drop and recreate the column to ensure clean state

USE brain_mirror;

-- Check if the column exists
SET @column_exists = (
    SELECT COUNT(*) 
    FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_SCHEMA = 'brain_mirror' 
    AND TABLE_NAME = 'test_records' 
    AND COLUMN_NAME = 'report_number'
);

-- Drop the column if it exists
SET @drop_query = IF(@column_exists > 0,
    'ALTER TABLE test_records DROP COLUMN report_number',
    'SELECT ''Column report_number does not exist, skipping drop'' AS message'
);

PREPARE stmt FROM @drop_query;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- Add the report_number column (now it definitely doesn't exist)
ALTER TABLE test_records 
ADD COLUMN report_number VARCHAR(50) UNIQUE COMMENT 'Report Number' AFTER id;

-- Generate report numbers for all existing records
-- Format: RPT-YYYYMMDD-{ID} (ID without padding, can be any length)
UPDATE test_records 
SET report_number = CONCAT('RPT-', DATE_FORMAT(created_at, '%Y%m%d'), '-', id)
WHERE report_number IS NULL OR report_number = '';

