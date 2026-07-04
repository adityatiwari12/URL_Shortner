-- =========================================================
-- URL Shortener - Database creation script
-- Run this in SQL Server Management Studio (SSMS)
-- =========================================================

IF DB_ID('UrlShortenerDB') IS NULL
BEGIN
    CREATE DATABASE UrlShortenerDB;
END
GO

USE UrlShortenerDB;
GO

IF OBJECT_ID('dbo.Urls', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Urls
    (
        Id           INT IDENTITY(1,1) PRIMARY KEY,
        OriginalUrl  NVARCHAR(MAX)   NOT NULL,
        ShortCode    VARCHAR(8)      NOT NULL,
        CreatedAt    DATETIME        NOT NULL DEFAULT GETDATE(),
        ClickCount   INT             NOT NULL DEFAULT 0,
        CONSTRAINT UQ_Urls_ShortCode UNIQUE (ShortCode)
    );
END
GO
