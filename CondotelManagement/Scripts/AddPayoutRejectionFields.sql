-- Script để thêm các field tracking payout rejection vào bảng Booking
-- Chạy script này để thêm 2 cột mới: PayoutRejectedAt và PayoutRejectionReason

USE [YourDatabaseName]; -- Thay đổi tên database của bạn
GO

-- Thêm cột PayoutRejectedAt (datetime, nullable)
IF NOT EXISTS (
    SELECT * FROM sys.columns 
    WHERE object_id = OBJECT_ID(N'[dbo].[Booking]') 
    AND name = 'PayoutRejectedAt'
)
BEGIN
    ALTER TABLE [dbo].[Booking]
    ADD [PayoutRejectedAt] DATETIME NULL;
    
    PRINT 'Đã thêm cột PayoutRejectedAt vào bảng Booking';
END
ELSE
BEGIN
    PRINT 'Cột PayoutRejectedAt đã tồn tại';
END
GO

-- Thêm cột PayoutRejectionReason (nvarchar(500), nullable)
IF NOT EXISTS (
    SELECT * FROM sys.columns 
    WHERE object_id = OBJECT_ID(N'[dbo].[Booking]') 
    AND name = 'PayoutRejectionReason'
)
BEGIN
    ALTER TABLE [dbo].[Booking]
    ADD [PayoutRejectionReason] NVARCHAR(500) NULL;
    
    PRINT 'Đã thêm cột PayoutRejectionReason vào bảng Booking';
END
ELSE
BEGIN
    PRINT 'Cột PayoutRejectionReason đã tồn tại';
END
GO

PRINT 'Hoàn thành! Các cột đã được thêm vào bảng Booking.';
GO


