/*
================================================================================
  Blog cover and inline images stored in EliSolutionDB
  Run against EliSolutionDB.
================================================================================
*/

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

IF OBJECT_ID(N'dbo.BlogImages', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.BlogImages
    (
        Id INT IDENTITY(1,1) NOT NULL,
        FileName NVARCHAR(200) NOT NULL,
        ContentType NVARCHAR(100) NOT NULL,
        Content VARBINARY(MAX) NOT NULL,
        CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_BlogImages_CreatedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_BlogImages PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT UQ_BlogImages_FileName UNIQUE (FileName)
    );
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_BlogImages_Insert
    @FileName    NVARCHAR(200),
    @ContentType NVARCHAR(100),
    @Content     VARBINARY(MAX),
    @Id          INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    IF @FileName IS NULL OR LTRIM(RTRIM(@FileName)) = N''
    BEGIN
        THROW 50051, N'Image file name is required.', 1;
    END

    IF @ContentType IS NULL OR LTRIM(RTRIM(@ContentType)) = N''
    BEGIN
        THROW 50052, N'Image content type is required.', 1;
    END

    IF @Content IS NULL OR DATALENGTH(@Content) = 0
    BEGIN
        THROW 50053, N'Image content is required.', 1;
    END

    IF EXISTS (SELECT 1 FROM dbo.BlogImages WHERE FileName = @FileName)
    BEGIN
        THROW 50054, N'An image with this file name already exists.', 1;
    END

    INSERT INTO dbo.BlogImages (FileName, ContentType, Content)
    VALUES (@FileName, @ContentType, @Content);

    SET @Id = CAST(SCOPE_IDENTITY() AS INT);
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_BlogImages_GetByFileName
    @FileName NVARCHAR(200)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        Id,
        FileName,
        ContentType,
        Content,
        CreatedAt
    FROM dbo.BlogImages
    WHERE FileName = @FileName;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_BlogImages_Exists
    @FileName NVARCHAR(200),
    @Exists   BIT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    SET @Exists = CASE
        WHEN EXISTS (SELECT 1 FROM dbo.BlogImages WHERE FileName = @FileName) THEN 1
        ELSE 0
    END;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_BlogImages_Delete
    @FileName NVARCHAR(200)
AS
BEGIN
    SET NOCOUNT ON;

    DELETE FROM dbo.BlogImages
    WHERE FileName = @FileName;
END
GO
