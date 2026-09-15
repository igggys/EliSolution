SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- Admin: permanently delete a blog article
-- @Id: BlogPosts.Id

CREATE OR ALTER PROCEDURE [dbo].[usp_BlogPosts_Delete]
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;

    IF @Id IS NULL OR @Id < 1
    BEGIN
        THROW 50045, N'Blog post was not found.', 1;
    END

    IF NOT EXISTS (SELECT 1 FROM dbo.BlogPosts WHERE Id = @Id)
    BEGIN
        THROW 50045, N'Blog post was not found.', 1;
    END

    DELETE FROM dbo.BlogPosts
    WHERE Id = @Id;
END
GO
