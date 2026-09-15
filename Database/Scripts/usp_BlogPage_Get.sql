SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- PublicSite: one call for blog listing page
-- Result sets:
--   1) active languages
--   2) active menu for language (Id/ParentId for tree)
--   3) paging (TotalCount, PageNumber, PageSize)
--   4) published blog posts for the requested page
--   5) site contact information
-- @LanguageCode: optional, default language when NULL/empty
-- @PageNumber: 1-based page index
-- @PageSize: posts per page (1..50, default 6)

CREATE OR ALTER PROCEDURE [dbo].[usp_BlogPage_Get]
    @LanguageCode NVARCHAR(10) = NULL,
    @PageNumber   INT = 1,
    @PageSize     INT = 6
AS
BEGIN
    SET NOCOUNT ON;

    IF @LanguageCode IS NULL OR LTRIM(RTRIM(@LanguageCode)) = N''
    BEGIN
        SELECT TOP (1) @LanguageCode = Code
        FROM dbo.Languages
        WHERE IsActive = 1
          AND IsDefault = 1
        ORDER BY SortOrder, Code;
    END
    ELSE
    BEGIN
        SET @LanguageCode = LTRIM(RTRIM(@LanguageCode));
    END

    IF @PageSize IS NULL OR @PageSize < 1
        SET @PageSize = 6;
    IF @PageSize > 50
        SET @PageSize = 50;
    IF @PageNumber IS NULL OR @PageNumber < 1
        SET @PageNumber = 1;

    DECLARE @TotalCount INT;

    SELECT @TotalCount = COUNT(1)
    FROM dbo.BlogPosts AS bp
    INNER JOIN dbo.Languages AS lang
        ON lang.Id = bp.LanguageId
    WHERE bp.IsActive = 1
      AND lang.IsActive = 1
      AND lang.Code = @LanguageCode
      AND bp.PublishedAt IS NOT NULL
      AND bp.PublishedAt <= SYSUTCDATETIME();

    DECLARE @PageCount INT =
        CASE
            WHEN @TotalCount = 0 THEN 1
            ELSE CEILING(@TotalCount * 1.0 / @PageSize)
        END;

    IF @PageNumber > @PageCount
        SET @PageNumber = @PageCount;

    DECLARE @Offset INT = (@PageNumber - 1) * @PageSize;

    --------------------------------------------------------------------------
    -- 1. Languages
    --------------------------------------------------------------------------
    SELECT
        Code,
        NativeName,
        IsRtl,
        IsDefault,
        SortOrder
    FROM dbo.Languages
    WHERE IsActive = 1
    ORDER BY SortOrder, Code;

    --------------------------------------------------------------------------
    -- 2. Menu
    --------------------------------------------------------------------------
    SELECT
        m.Id,
        m.ParentId,
        m.Url,
        m.SortOrder,
        t.Title
    FROM dbo.MenuItems AS m
    INNER JOIN dbo.MenuItemTranslations AS t
        ON t.MenuItemId = m.Id
    INNER JOIN dbo.Languages AS l
        ON l.Id = t.LanguageId
    LEFT JOIN dbo.MenuItems AS parent
        ON parent.Id = m.ParentId
    WHERE m.IsActive = 1
      AND l.IsActive = 1
      AND l.Code = @LanguageCode
      AND (m.ParentId IS NULL OR parent.IsActive = 1)
    ORDER BY
        CASE WHEN m.ParentId IS NULL THEN m.SortOrder ELSE parent.SortOrder END,
        CASE WHEN m.ParentId IS NULL THEN 0 ELSE 1 END,
        m.SortOrder,
        m.Id;

    --------------------------------------------------------------------------
    -- 3. Paging
    --------------------------------------------------------------------------
    SELECT
        @TotalCount AS TotalCount,
        @PageNumber AS PageNumber,
        @PageSize AS PageSize;

    --------------------------------------------------------------------------
    -- 4. Published blog posts (page)
    --------------------------------------------------------------------------
    SELECT
        bp.Url,
        bp.ImageUrl,
        bp.Name,
        bp.MetaDescription
    FROM dbo.BlogPosts AS bp
    INNER JOIN dbo.Languages AS lang
        ON lang.Id = bp.LanguageId
    WHERE bp.IsActive = 1
      AND lang.IsActive = 1
      AND lang.Code = @LanguageCode
      AND bp.PublishedAt IS NOT NULL
      AND bp.PublishedAt <= SYSUTCDATETIME()
    ORDER BY bp.SortOrder, bp.PublishedAt DESC, bp.Id
    OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;

    --------------------------------------------------------------------------
    -- 5. Contact information
    --------------------------------------------------------------------------
    SELECT TOP (1)
        Email,
        Phone,
        WhatsApp
    FROM dbo.ContactInfo
    WHERE Email IS NOT NULL
       OR Phone IS NOT NULL
       OR WhatsApp IS NOT NULL
    ORDER BY Id;
END
GO
