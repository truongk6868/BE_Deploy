-- Script để tạo lại bảng RefundRequests
-- Chạy script này sẽ DROP và tạo lại bảng với đầy đủ cấu trúc

USE [YourDatabaseName]; -- Thay đổi tên database của bạn
GO

-- Xóa các foreign key constraints trước
IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_RefundRequests_Bookings')
BEGIN
    ALTER TABLE [RefundRequests] DROP CONSTRAINT [FK_RefundRequests_Bookings];
    PRINT 'Dropped FK_RefundRequests_Bookings';
END
GO

IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_RefundRequests_Users_Customer')
BEGIN
    ALTER TABLE [RefundRequests] DROP CONSTRAINT [FK_RefundRequests_Users_Customer];
    PRINT 'Dropped FK_RefundRequests_Users_Customer';
END
GO

IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_RefundRequests_Users_Admin')
BEGIN
    ALTER TABLE [RefundRequests] DROP CONSTRAINT [FK_RefundRequests_Users_Admin];
    PRINT 'Dropped FK_RefundRequests_Users_Admin';
END
GO

-- Xóa bảng nếu tồn tại
IF OBJECT_ID('RefundRequests', 'U') IS NOT NULL
BEGIN
    DROP TABLE [RefundRequests];
    PRINT 'Dropped table RefundRequests';
END
GO

-- Tạo lại bảng RefundRequests
CREATE TABLE [RefundRequests] (
    [Id] INT IDENTITY(1,1) NOT NULL,
    [BookingId] INT NOT NULL,
    [CustomerId] INT NOT NULL,
    [CustomerName] NVARCHAR(255) NOT NULL,
    [CustomerEmail] NVARCHAR(255) NULL,
    [RefundAmount] DECIMAL(18,2) NOT NULL,
    [Status] NVARCHAR(50) NOT NULL DEFAULT 'Pending',
    [BankCode] NVARCHAR(50) NULL,
    [AccountNumber] NVARCHAR(50) NULL,
    [AccountHolder] NVARCHAR(255) NULL,
    [Reason] NVARCHAR(500) NULL,
    [CancelDate] DATETIME NULL,
    [ProcessedBy] INT NULL,
    [ProcessedAt] DATETIME NULL,
    [TransactionId] NVARCHAR(100) NULL,
    [PaymentMethod] NVARCHAR(50) NULL,
    [CreatedAt] DATETIME NOT NULL DEFAULT GETDATE(),
    [UpdatedAt] DATETIME NULL,
    
    CONSTRAINT [PK_RefundRequests] PRIMARY KEY CLUSTERED ([Id] ASC)
);
GO

PRINT 'Created table RefundRequests';
GO

-- Tạo Foreign Key: RefundRequests -> Bookings
ALTER TABLE [RefundRequests]
ADD CONSTRAINT [FK_RefundRequests_Bookings]
FOREIGN KEY ([BookingId])
REFERENCES [Bookings] ([BookingId])
ON DELETE CASCADE;
GO

PRINT 'Created FK_RefundRequests_Bookings';
GO

-- Tạo Foreign Key: RefundRequests -> Users (Customer)
ALTER TABLE [RefundRequests]
ADD CONSTRAINT [FK_RefundRequests_Users_Customer]
FOREIGN KEY ([CustomerId])
REFERENCES [Users] ([UserID])
ON DELETE NO ACTION;
GO

PRINT 'Created FK_RefundRequests_Users_Customer';
GO

-- Tạo Foreign Key: RefundRequests -> Users (ProcessedBy/Admin)
ALTER TABLE [RefundRequests]
ADD CONSTRAINT [FK_RefundRequests_Users_Admin]
FOREIGN KEY ([ProcessedBy])
REFERENCES [Users] ([UserID])
ON DELETE NO ACTION;
GO

PRINT 'Created FK_RefundRequests_Users_Admin';
GO

-- Tạo Index cho BookingId để tối ưu query
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_RefundRequests_BookingId')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_RefundRequests_BookingId]
    ON [RefundRequests] ([BookingId]);
    PRINT 'Created index IX_RefundRequests_BookingId';
END
GO

-- Tạo Index cho CustomerId để tối ưu query
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_RefundRequests_CustomerId')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_RefundRequests_CustomerId]
    ON [RefundRequests] ([CustomerId]);
    PRINT 'Created index IX_RefundRequests_CustomerId';
END
GO

-- Tạo Index cho Status để tối ưu query
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_RefundRequests_Status')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_RefundRequests_Status]
    ON [RefundRequests] ([Status]);
    PRINT 'Created index IX_RefundRequests_Status';
END
GO

PRINT 'Script completed successfully!';
GO



