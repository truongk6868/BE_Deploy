-- AddResortAddressColumn.sql
-- Script to add Address column to the Resort table

-- Check if Address column exists, if not, add it
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Resort') AND name = 'Address')
BEGIN
    ALTER TABLE [Resort]
    ADD [Address] NVARCHAR(500) NULL;
    PRINT 'Column Address added to Resort table.';
END
ELSE
BEGIN
    PRINT 'Column Address already exists in Resort table.';
END


