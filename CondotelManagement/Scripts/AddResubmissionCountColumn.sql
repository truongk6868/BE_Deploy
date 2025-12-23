-- Add ResubmissionCount column to RefundRequests table
-- Run this SQL script in your database

USE [YourDatabaseName]; -- Change to your database name
GO

-- Check if column exists
IF NOT EXISTS (
    SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'RefundRequests' 
    AND COLUMN_NAME = 'ResubmissionCount'
)
BEGIN
    -- Add the column
    ALTER TABLE [RefundRequests]
    ADD [ResubmissionCount] INT NOT NULL DEFAULT 0;
    
    PRINT 'Column ResubmissionCount added successfully';
END
ELSE
BEGIN
    PRINT 'Column ResubmissionCount already exists';
END
GO

-- Verify the column
SELECT TOP 5 Id, BookingId, Status, ResubmissionCount 
FROM RefundRequests
ORDER BY Id DESC;
GO
