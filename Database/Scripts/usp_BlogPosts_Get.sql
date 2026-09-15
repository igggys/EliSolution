SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- Admin: list blog articles (no Body)
-- @LanguageId: optional filter
-- @ActiveOnly: 1 = only IsActive posts

CREATE OR ALTER PROCEDURE [dbo].[usp_BlogPosts_GetAll]
    @LanguageId TINYINT = NULL,
    @ActiveOnly BIT = 0
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        bp.Id,
        bp.LanguageId,
        bp.Url,
        bp.ImageUrl,
        bp.Name,
        bp.PublishedAt,
        bp.SortOrder,
        bp.IsActive,
        bp.SeoTitle,
        bp.MetaDescription,
        bp.IsFeatured,
        CAST(CASE WHEN bp.PublishedAt IS NOT NULL THEN 1 ELSE 0 END AS BIT) AS IsPublished
    FROM dbo.BlogPosts AS bp
    WHERE (@LanguageId IS NULL OR bp.LanguageId = @LanguageId)
      AND (@ActiveOnly = 0 OR bp.IsActive = 1)
    ORDER BY bp.SortOrder, bp.PublishedAt DESC, bp.Id;
END
GO

CREATE OR ALTER PROCEDURE [dbo].[usp_BlogPosts_GetById]
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        Id,
        LanguageId,
        Url,
        ImageUrl,
        Name,
        Body,
        PublishedAt,
        SortOrder,
        IsActive,
        SeoTitle,
        MetaDescription,
        MetaRobots,
        OgTitle,
        OgDescription,
        OgImage,
        OgType,
        OgUrl,
        IsFeatured,
        CAST(CASE WHEN PublishedAt IS NOT NULL THEN 1 ELSE 0 END AS BIT) AS IsPublished
    FROM dbo.BlogPosts
    WHERE Id = @Id;
END
GO
