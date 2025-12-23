-- Script to add ImageUrl column to Location table
-- Date: 2025-12-07

-- Check if column already exists before adding
IF NOT EXISTS (
    SELECT 1 
    FROM sys.columns 
    WHERE object_id = OBJECT_ID(N'[dbo].[Location]') 
    AND name = 'ImageUrl'
)
BEGIN
    -- Add ImageUrl column
    ALTER TABLE [dbo].[Location]
    ADD [ImageUrl] NVARCHAR(500) NULL;

    PRINT 'ImageUrl column added successfully to Location table.';
END
ELSE
BEGIN
    PRINT 'ImageUrl column already exists in Location table.';
END
GO



