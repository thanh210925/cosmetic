-- =======================================================
-- SQL SCRIPT: CREATE FLASH SALE TABLES IF NOT EXIST
-- Database: CosmeticsDB
-- =======================================================

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'FlashSales')
BEGIN
    CREATE TABLE FlashSales (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Title NVARCHAR(255) NOT NULL,
        StartTime DATETIME NOT NULL,
        EndTime DATETIME NOT NULL,
        IsActive BIT NOT NULL DEFAULT 1,
        CreatedAt DATETIME NOT NULL DEFAULT GETDATE()
    );
    PRINT 'Created table FlashSales successfully.';
END
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'FlashSaleItems')
BEGIN
    CREATE TABLE FlashSaleItems (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        FlashSaleId INT NOT NULL,
        ProductId INT NOT NULL,
        DiscountPrice DECIMAL(10, 2) NOT NULL,
        QuantityForSale INT NOT NULL DEFAULT 1,
        SoldQuantity INT NOT NULL DEFAULT 0,
        IsActive BIT NOT NULL DEFAULT 1,
        CONSTRAINT FK_FlashSaleItems_FlashSales FOREIGN KEY (FlashSaleId) REFERENCES FlashSales(Id) ON DELETE CASCADE,
        CONSTRAINT FK_FlashSaleItems_Products FOREIGN KEY (ProductId) REFERENCES Products(Id) ON DELETE CASCADE
    );
    PRINT 'Created table FlashSaleItems successfully.';
END
GO
