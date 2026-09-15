SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- Admin: update a blog article
-- @ImageUrl: omit or pass NULL to keep the current cover
-- @SortOrder: omit or pass NULL to keep the current order.
-- If the number is already used in the language, every SortOrder in that language is rebuilt.
-- @PublishedAt: omit or pass a date to publish; pass NULL with @IsPublished = 0 for a draft

CREATE OR ALTER PROCEDURE [dbo].[usp_BlogPosts_Update]
    @Id               INT,
    @LanguageId       TINYINT,
    @Url              NVARCHAR(500),
    @Name             NVARCHAR(200),
    @Body             NVARCHAR(MAX) = NULL,
    @ImageUrl         NVARCHAR(500) = NULL,
    @PublishedAt      DATETIME2(0) = NULL,
    @IsPublished      BIT = 1,
    @SortOrder        INT = NULL,
    @IsActive         BIT = 1,
    @SeoTitle         NVARCHAR(200) = NULL,
    @MetaDescription  NVARCHAR(500) = NULL,
    @MetaRobots       NVARCHAR(100) = NULL,
    @OgTitle          NVARCHAR(200) = NULL,
    @OgDescription    NVARCHAR(500) = NULL,
    @OgImage          NVARCHAR(500) = NULL,
    @OgType           NVARCHAR(50) = NULL,
    @OgUrl            NVARCHAR(500) = NULL,
    @IsFeatured       BIT = 0
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM dbo.BlogPosts WHERE Id = @Id)
    BEGIN
        THROW 50045, N'Blog post was not found.', 1;
    END

    IF @Name IS NULL OR LTRIM(RTRIM(@Name)) = N''
    BEGIN
        THROW 50041, N'Blog post name is required.', 1;
    END

    IF @Url IS NULL OR LTRIM(RTRIM(@Url)) = N''
    BEGIN
        THROW 50044, N'Blog post url is required.', 1;
    END

    IF NOT EXISTS (SELECT 1 FROM dbo.Languages WHERE Id = @LanguageId)
    BEGIN
        THROW 50042, N'Language was not found.', 1;
    END

    SET @Name = LTRIM(RTRIM(@Name));
    SET @Url = LOWER(LTRIM(RTRIM(@Url)));
    SET @Url = REPLACE(@Url, N'\', N'/');

    WHILE @Url LIKE N'%//'
        SET @Url = REPLACE(@Url, N'//', N'/');

    IF LEFT(@Url, 1) <> N'/'
        SET @Url = N'/' + @Url;

    IF @Url NOT LIKE N'/blog/%'
    BEGIN
        IF @Url = N'/blog'
        BEGIN
            THROW 50044, N'Blog post url is required.', 1;
        END

        SET @Url = N'/blog' + CASE WHEN LEFT(@Url, 1) = N'/' THEN @Url ELSE N'/' + @Url END;
    END

    IF EXISTS (
        SELECT 1
        FROM dbo.BlogPosts
        WHERE LanguageId = @LanguageId
          AND Url = @Url
          AND Id <> @Id
    )
    BEGIN
        THROW 50043, N'A blog post with this url already exists for the language.', 1;
    END

    IF @MetaRobots IS NULL OR LTRIM(RTRIM(@MetaRobots)) = N''
        SET @MetaRobots = N'index, follow';

    IF @OgType IS NULL OR LTRIM(RTRIM(@OgType)) = N''
        SET @OgType = N'article';

    IF @SeoTitle IS NULL OR LTRIM(RTRIM(@SeoTitle)) = N''
        SET @SeoTitle = @Name;

    IF @OgTitle IS NULL OR LTRIM(RTRIM(@OgTitle)) = N''
        SET @OgTitle = @Name;

    IF @OgUrl IS NULL OR LTRIM(RTRIM(@OgUrl)) = N''
        SET @OgUrl = @Url;

    IF @IsPublished = 1
        SET @PublishedAt = ISNULL(@PublishedAt, SYSUTCDATETIME());
    ELSE
        SET @PublishedAt = NULL;

    DECLARE @CurrentLanguageId TINYINT;
    DECLARE @CurrentSortOrder INT;
    SELECT
        @CurrentLanguageId = LanguageId,
        @CurrentSortOrder = SortOrder
    FROM dbo.BlogPosts
    WHERE Id = @Id;

    DECLARE @NewSortOrder INT = ISNULL(@SortOrder, @CurrentSortOrder);
    IF @NewSortOrder < 1
        SET @NewSortOrder = 1;

    DECLARE @SortOrderCollision BIT = 0;
    IF EXISTS (
        SELECT 1
        FROM dbo.BlogPosts
        WHERE LanguageId = @LanguageId
          AND SortOrder = @NewSortOrder
          AND Id <> @Id
    )
        SET @SortOrderCollision = 1;

    DECLARE @PlaceFirst BIT = 1;
    IF @CurrentLanguageId = @LanguageId AND @NewSortOrder > @CurrentSortOrder
        SET @PlaceFirst = 0;

    BEGIN TRY
        BEGIN TRANSACTION;

        UPDATE dbo.BlogPosts
        SET
            LanguageId = @LanguageId,
            Url = @Url,
            ImageUrl = COALESCE(NULLIF(LTRIM(RTRIM(@ImageUrl)), N''), ImageUrl),
            Name = @Name,
            Body = @Body,
            PublishedAt = @PublishedAt,
            SortOrder = @NewSortOrder,
            IsActive = ISNULL(@IsActive, 1),
            SeoTitle = @SeoTitle,
            MetaDescription = NULLIF(LTRIM(RTRIM(@MetaDescription)), N''),
            MetaRobots = @MetaRobots,
            OgTitle = @OgTitle,
            OgDescription = NULLIF(LTRIM(RTRIM(@OgDescription)), N''),
            OgImage = NULLIF(LTRIM(RTRIM(@OgImage)), N''),
            OgType = @OgType,
            OgUrl = @OgUrl,
            IsFeatured = ISNULL(@IsFeatured, 0)
        WHERE Id = @Id;

        IF @SortOrderCollision = 1
            EXEC dbo.usp_BlogPosts_ResequenceSortOrder
                @LanguageId = @LanguageId,
                @Id = @Id,
                @SortOrder = @NewSortOrder,
                @PlaceFirst = @PlaceFirst;

        IF @CurrentLanguageId <> @LanguageId
            EXEC dbo.usp_BlogPosts_ResequenceSortOrder
                @LanguageId = @CurrentLanguageId,
                @Id = @Id,
                @SortOrder = 2147483647,
                @PlaceFirst = 0;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO
