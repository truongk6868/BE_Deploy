-- Script to add missing columns to Package table
-- Date: 2025-12-08
-- Columns: MaxListingCount, CanUseFeaturedListing, MaxBlogRequestsPerMonth, IsVerifiedBadgeEnabled, DisplayColorTheme, PriorityLevel

-- Check and add MaxListingCount
IF NOT EXISTS (
    SELECT 1 
    FROM sys.columns 
    WHERE object_id = OBJECT_ID(N'[dbo].[Package]') 
    AND name = 'MaxListingCount'
)
BEGIN
    ALTER TABLE [dbo].[Package]
    ADD [MaxListingCount] INT NULL;
    PRINT 'MaxListingCount column added successfully to Package table.';
END
ELSE
BEGIN
    PRINT 'MaxListingCount column already exists in Package table.';
END
GO

-- Check and add CanUseFeaturedListing
IF NOT EXISTS (
    SELECT 1 
    FROM sys.columns 
    WHERE object_id = OBJECT_ID(N'[dbo].[Package]') 
    AND name = 'CanUseFeaturedListing'
)
BEGIN
    ALTER TABLE [dbo].[Package]
    ADD [CanUseFeaturedListing] BIT NULL;
    PRINT 'CanUseFeaturedListing column added successfully to Package table.';
END
ELSE
BEGIN
    PRINT 'CanUseFeaturedListing column already exists in Package table.';
END
GO

-- Check and add MaxBlogRequestsPerMonth
IF NOT EXISTS (
    SELECT 1 
    FROM sys.columns 
    WHERE object_id = OBJECT_ID(N'[dbo].[Package]') 
    AND name = 'MaxBlogRequestsPerMonth'
)
BEGIN
    ALTER TABLE [dbo].[Package]
    ADD [MaxBlogRequestsPerMonth] INT NULL;
    PRINT 'MaxBlogRequestsPerMonth column added successfully to Package table.';
END
ELSE
BEGIN
    PRINT 'MaxBlogRequestsPerMonth column already exists in Package table.';
END
GO

-- Check and add IsVerifiedBadgeEnabled
IF NOT EXISTS (
    SELECT 1 
    FROM sys.columns 
    WHERE object_id = OBJECT_ID(N'[dbo].[Package]') 
    AND name = 'IsVerifiedBadgeEnabled'
)
BEGIN
    ALTER TABLE [dbo].[Package]
    ADD [IsVerifiedBadgeEnabled] BIT NULL;
    PRINT 'IsVerifiedBadgeEnabled column added successfully to Package table.';
END
ELSE
BEGIN
    PRINT 'IsVerifiedBadgeEnabled column already exists in Package table.';
END
GO

-- Check and add DisplayColorTheme
IF NOT EXISTS (
    SELECT 1 
    FROM sys.columns 
    WHERE object_id = OBJECT_ID(N'[dbo].[Package]') 
    AND name = 'DisplayColorTheme'
)
BEGIN
    ALTER TABLE [dbo].[Package]
    ADD [DisplayColorTheme] NVARCHAR(50) NULL;
    PRINT 'DisplayColorTheme column added successfully to Package table.';
END
ELSE
BEGIN
    PRINT 'DisplayColorTheme column already exists in Package table.';
END
GO

-- Check and add PriorityLevel
IF NOT EXISTS (
    SELECT 1 
    FROM sys.columns 
    WHERE object_id = OBJECT_ID(N'[dbo].[Package]') 
    AND name = 'PriorityLevel'
)
BEGIN
    ALTER TABLE [dbo].[Package]
    ADD [PriorityLevel] INT NULL;
    PRINT 'PriorityLevel column added successfully to Package table.';
END
ELSE
BEGIN
    PRINT 'PriorityLevel column already exists in Package table.';
END
GO



