SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- PublicSite home: up to 3 blog cards
-- Featured posts first (PublishedAt DESC), then newest non-featured to fill to 3

CREATE OR ALTER PROCEDURE [dbo].[usp_BlogPosts_GetFeatured]
    @LanguageCode NVARCHAR(10) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @LanguageId TINYINT;

    IF @LanguageCode IS NULL OR LTRIM(RTRIM(@LanguageCode)) = N''
    BEGIN
        SELECT TOP (1) @LanguageId = Id
        FROM dbo.Languages
        WHERE IsActive = 1
          AND IsDefault = 1
        ORDER BY SortOrder, Code;
    END
    ELSE
    BEGIN
        SELECT TOP (1) @LanguageId = Id
        FROM dbo.Languages
        WHERE IsActive = 1
          AND Code = LTRIM(RTRIM(@LanguageCode));
    END

    IF @LanguageId IS NULL
    BEGIN
        SELECT TOP (1) @LanguageId = Id
        FROM dbo.Languages
        WHERE IsActive = 1
          AND IsDefault = 1
        ORDER BY SortOrder, Code;
    END

    IF @LanguageId IS NULL
    BEGIN
        SELECT
            CAST(NULL AS NVARCHAR(500)) AS Url,
            CAST(NULL AS NVARCHAR(500)) AS ImageUrl,
            CAST(NULL AS NVARCHAR(200)) AS Name
        WHERE 1 = 0;
        RETURN;
    END

    ;WITH Visible AS (
        SELECT
            Url,
            ImageUrl,
            Name,
            IsFeatured,
            PublishedAt,
            Id
        FROM dbo.BlogPosts
        WHERE LanguageId = @LanguageId
          AND IsActive = 1
          AND PublishedAt IS NOT NULL
          AND PublishedAt <= SYSUTCDATETIME()
    ),
    Ranked AS (
        SELECT
            Url,
            ImageUrl,
            Name,
            CASE WHEN IsFeatured = 1 THEN 0 ELSE 1 END AS Bucket,
            ROW_NUMBER() OVER (
                PARTITION BY CASE WHEN IsFeatured = 1 THEN 0 ELSE 1 END
                ORDER BY PublishedAt DESC, Id DESC
            ) AS Rn
        FROM Visible
    )
    SELECT TOP (3)
        Url,
        ImageUrl,
        Name
    FROM Ranked
    WHERE Rn <= 3
    ORDER BY Bucket, Rn;
END
GO
