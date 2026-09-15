SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- Admin: rebuild SortOrder for every blog post in a language.
-- @Id takes @SortOrder. @PlaceFirst = 1 puts it before any post that already
-- had that number; 0 puts it after (used when moving an existing post down).

CREATE OR ALTER PROCEDURE [dbo].[usp_BlogPosts_ResequenceSortOrder]
    @LanguageId TINYINT,
    @Id         INT,
    @SortOrder  INT,
    @PlaceFirst BIT = 1
AS
BEGIN
    SET NOCOUNT ON;

    IF @SortOrder < 1
        SET @SortOrder = 1;

    ;WITH numbered AS
    (
        SELECT
            Id,
            ROW_NUMBER() OVER (
                ORDER BY
                    CASE WHEN Id = @Id THEN @SortOrder ELSE SortOrder END,
                    CASE
                        WHEN Id = @Id THEN CASE WHEN @PlaceFirst = 1 THEN 0 ELSE 1 END
                        ELSE CASE WHEN @PlaceFirst = 1 THEN 1 ELSE 0 END
                    END,
                    Id
            ) AS NewSortOrder
        FROM dbo.BlogPosts
        WHERE LanguageId = @LanguageId
    )
    UPDATE bp
    SET SortOrder = numbered.NewSortOrder
    FROM dbo.BlogPosts AS bp
    INNER JOIN numbered ON numbered.Id = bp.Id;
END
GO
