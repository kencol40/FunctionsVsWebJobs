-- Creates the POC database and the four tables used by the Function and WebJob stacks.
-- This script is idempotent and safe to re-run. It also runs automatically via EF Core's
-- Database.EnsureCreated() call made by both hosts on startup, but is kept here as a
-- reference / manual fallback (e.g. for running against the container directly with sqlcmd).

IF DB_ID('PocDb') IS NULL
BEGIN
	CREATE DATABASE PocDb;
END
GO

USE PocDb;
GO

IF OBJECT_ID('dbo.function_blobrow_data', 'U') IS NULL
BEGIN
	CREATE TABLE dbo.function_blobrow_data
	(
		Id INT IDENTITY(1,1) PRIMARY KEY,
		BlobName NVARCHAR(1024) NOT NULL,
		RowJson NVARCHAR(MAX) NOT NULL,
		CreatedUtc DATETIMEOFFSET NOT NULL
	);
END
GO

IF OBJECT_ID('dbo.webjob_blobrow_data', 'U') IS NULL
BEGIN
	CREATE TABLE dbo.webjob_blobrow_data
	(
		Id INT IDENTITY(1,1) PRIMARY KEY,
		BlobName NVARCHAR(1024) NOT NULL,
		RowJson NVARCHAR(MAX) NOT NULL,
		CreatedUtc DATETIMEOFFSET NOT NULL
	);
END
GO

IF OBJECT_ID('dbo.function_message_data', 'U') IS NULL
BEGIN
	CREATE TABLE dbo.function_message_data
	(
		Id INT IDENTITY(1,1) PRIMARY KEY,
		MessageId NVARCHAR(256) NOT NULL,
		BodyJson NVARCHAR(MAX) NOT NULL,
		CreatedUtc DATETIMEOFFSET NOT NULL
	);
END
GO

IF OBJECT_ID('dbo.webjob_message_data', 'U') IS NULL
BEGIN
	CREATE TABLE dbo.webjob_message_data
	(
		Id INT IDENTITY(1,1) PRIMARY KEY,
		MessageId NVARCHAR(256) NOT NULL,
		BodyJson NVARCHAR(MAX) NOT NULL,
		CreatedUtc DATETIMEOFFSET NOT NULL
	);
END
GO
