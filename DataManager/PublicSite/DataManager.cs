namespace DataManager.PublicSite;

using System.Data;
using Microsoft.Data.SqlClient;
using Models.PublicSite;

public class DataManager
{
    private readonly string _connectionString;

    public DataManager(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        _connectionString = connectionString;
    }

    public async Task<IReadOnlyList<Language>> GetActiveLanguagesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var languages = new List<Language>();

            await using var connection = new SqlConnection(_connectionString);
            await using var command = new SqlCommand("dbo.usp_Languages_GetActive", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            await connection.OpenAsync(cancellationToken);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            var codeOrdinal = reader.GetOrdinal("Code");
            var nativeNameOrdinal = reader.GetOrdinal("NativeName");
            var isRtlOrdinal = reader.GetOrdinal("IsRtl");
            var isDefaultOrdinal = reader.GetOrdinal("IsDefault");
            var sortOrderOrdinal = reader.GetOrdinal("SortOrder");

            while (await reader.ReadAsync(cancellationToken))
            {
                languages.Add(new Language
                {
                    Code = reader.GetString(codeOrdinal),
                    NativeName = reader.GetString(nativeNameOrdinal),
                    IsRtl = reader.GetBoolean(isRtlOrdinal),
                    IsDefault = reader.GetBoolean(isDefaultOrdinal),
                    SortOrder = GetInt32Compat(reader, sortOrderOrdinal)
                });
            }

            return languages;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (SqlException ex)
        {
            throw new DataManagerException("Failed to get active languages.", ex);
        }
        catch (Exception ex)
        {
            throw new DataManagerException("Failed to get active languages.", ex);
        }
    }

    public async Task<Menu> GetActiveMenuAsync(
        string? languageCode = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var rows = new List<(int Id, int? ParentId, string? Url, string Title)>();

            await using var connection = new SqlConnection(_connectionString);
            await using var command = new SqlCommand("dbo.usp_Menu_GetActive", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.Add(new SqlParameter("@LanguageCode", SqlDbType.NVarChar, 10)
            {
                Value = string.IsNullOrWhiteSpace(languageCode) ? DBNull.Value : languageCode
            });

            await connection.OpenAsync(cancellationToken);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            var idOrdinal = reader.GetOrdinal("Id");
            var parentIdOrdinal = reader.GetOrdinal("ParentId");
            var urlOrdinal = reader.GetOrdinal("Url");
            var titleOrdinal = reader.GetOrdinal("Title");

            while (await reader.ReadAsync(cancellationToken))
            {
                rows.Add((
                    GetInt32Compat(reader, idOrdinal),
                    reader.IsDBNull(parentIdOrdinal) ? null : GetInt32Compat(reader, parentIdOrdinal),
                    reader.IsDBNull(urlOrdinal) ? null : reader.GetString(urlOrdinal),
                    reader.GetString(titleOrdinal)));
            }

            return BuildMenu(rows);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (SqlException ex)
        {
            throw new DataManagerException("Failed to get active menu.", ex);
        }
        catch (Exception ex)
        {
            throw new DataManagerException("Failed to get active menu.", ex);
        }
    }

    public async Task<NotFoundPage> GetNotFoundPageAsync(
        string? languageCode = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = new SqlConnection(_connectionString);
            await using var command = new SqlCommand("dbo.usp_NotFoundPage_Get", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.Add(new SqlParameter("@LanguageCode", SqlDbType.NVarChar, 10)
            {
                Value = string.IsNullOrWhiteSpace(languageCode) ? DBNull.Value : languageCode
            });

            await connection.OpenAsync(cancellationToken);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            var languages = await ReadLanguagesAsync(reader, cancellationToken);

            await reader.NextResultAsync(cancellationToken);
            var menuRows = await ReadMenuRowsAsync(reader, cancellationToken);

            await reader.NextResultAsync(cancellationToken);
            var contactInfo = await ReadContactInfoAsync(reader, cancellationToken);

            var currentLanguage = ResolveCurrentLanguage(languages, languageCode);

            return new NotFoundPage
            {
                CurrentLanguage = currentLanguage,
                Languages = languages,
                Menu = BuildMenu(menuRows),
                ContactInfo = contactInfo
            };
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (SqlException ex)
        {
            throw new DataManagerException("Failed to get not found page.", ex);
        }
        catch (Exception ex)
        {
            throw new DataManagerException("Failed to get not found page.", ex);
        }
    }

    private static Menu BuildMenu(IReadOnlyList<(int Id, int? ParentId, string? Url, string Title)> rows)
    {
        var itemsById = new Dictionary<int, MenuItem>(rows.Count);
        var roots = new List<MenuItem>();
        var subMenus = new Dictionary<int, List<MenuItem>>();

        foreach (var (id, parentId, url, title) in rows)
        {
            var item = new MenuItem
            {
                Url = url,
                Title = title
            };

            itemsById[id] = item;

            if (parentId is null)
            {
                roots.Add(item);
                continue;
            }

            if (!subMenus.TryGetValue(parentId.Value, out var children))
            {
                children = [];
                subMenus[parentId.Value] = children;
            }

            children.Add(item);
        }

        foreach (var (parentId, children) in subMenus)
        {
            if (itemsById.TryGetValue(parentId, out var parent))
            {
                parent.SubMenu = children;
            }
        }

        return new Menu { Items = roots };
    }

    public async Task<IReadOnlyList<BlogPostLink>> GetFeaturedBlogPostsAsync(
        string? languageCode = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = new SqlConnection(_connectionString);
            await using var command = new SqlCommand("dbo.usp_BlogPosts_GetFeatured", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.Add(new SqlParameter("@LanguageCode", SqlDbType.NVarChar, 10)
            {
                Value = string.IsNullOrWhiteSpace(languageCode) ? DBNull.Value : languageCode
            });

            await connection.OpenAsync(cancellationToken);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            return await ReadBlogPostLinksAsync(reader, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (SqlException ex)
        {
            throw new DataManagerException("Failed to get featured blog posts.", ex);
        }
        catch (Exception ex)
        {
            throw new DataManagerException("Failed to get featured blog posts.", ex);
        }
    }

    public async Task<PracticeAreaPage> GetPracticeAreaPageAsync(
        string practiceAreaUrl,
        string? languageCode = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(practiceAreaUrl);

        try
        {
            await using var connection = new SqlConnection(_connectionString);
            await using var command = new SqlCommand("dbo.usp_PracticeAreaPage_Get", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.Add(new SqlParameter("@LanguageCode", SqlDbType.NVarChar, 10)
            {
                Value = string.IsNullOrWhiteSpace(languageCode) ? DBNull.Value : languageCode
            });
            command.Parameters.Add(new SqlParameter("@PracticeAreaUrl", SqlDbType.NVarChar, 500)
            {
                Value = practiceAreaUrl
            });

            await connection.OpenAsync(cancellationToken);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            var languages = await ReadLanguagesAsync(reader, cancellationToken);

            await reader.NextResultAsync(cancellationToken);
            var menuRows = await ReadMenuRowsAsync(reader, cancellationToken);

            await reader.NextResultAsync(cancellationToken);
            var practiceAreaLinks = await ReadPracticeAreaLinksAsync(reader, cancellationToken);

            await reader.NextResultAsync(cancellationToken);
            var practiceArea = await ReadPracticeAreaAsync(reader, cancellationToken);

            await reader.NextResultAsync(cancellationToken);
            var contactInfo = await ReadContactInfoAsync(reader, cancellationToken);

            var currentLanguage = ResolveCurrentLanguage(languages, languageCode);

            return new PracticeAreaPage
            {
                CurrentLanguage = currentLanguage,
                Languages = languages,
                Menu = BuildMenu(menuRows),
                PracticeAreas = practiceAreaLinks,
                PracticeArea = practiceArea,
                ContactInfo = contactInfo
            };
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (SqlException ex)
        {
            throw new DataManagerException("Failed to get practice area page.", ex);
        }
        catch (Exception ex)
        {
            throw new DataManagerException("Failed to get practice area page.", ex);
        }
    }

    public async Task<AboutPage> GetAboutPageAsync(
        string? languageCode = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = new SqlConnection(_connectionString);
            await using var command = new SqlCommand("dbo.usp_AboutPage_Get", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.Add(new SqlParameter("@LanguageCode", SqlDbType.NVarChar, 10)
            {
                Value = string.IsNullOrWhiteSpace(languageCode) ? DBNull.Value : languageCode
            });

            await connection.OpenAsync(cancellationToken);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            var languages = await ReadLanguagesAsync(reader, cancellationToken);

            await reader.NextResultAsync(cancellationToken);
            var menuRows = await ReadMenuRowsAsync(reader, cancellationToken);

            await reader.NextResultAsync(cancellationToken);
            var about = await ReadAboutAsync(reader, cancellationToken);

            await reader.NextResultAsync(cancellationToken);
            var contactInfo = await ReadContactInfoAsync(reader, cancellationToken);

            var currentLanguage = ResolveCurrentLanguage(languages, languageCode);

            return new AboutPage
            {
                CurrentLanguage = currentLanguage,
                Languages = languages,
                Menu = BuildMenu(menuRows),
                About = about,
                ContactInfo = contactInfo
            };
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (SqlException ex)
        {
            throw new DataManagerException("Failed to get about page.", ex);
        }
        catch (Exception ex)
        {
            throw new DataManagerException("Failed to get about page.", ex);
        }
    }

    public async Task<PrivacyPolicyPage> GetPrivacyPolicyPageAsync(
        string? languageCode = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = new SqlConnection(_connectionString);
            await using var command = new SqlCommand("dbo.usp_PrivacyPolicyPage_Get", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.Add(new SqlParameter("@LanguageCode", SqlDbType.NVarChar, 10)
            {
                Value = string.IsNullOrWhiteSpace(languageCode) ? DBNull.Value : languageCode
            });

            await connection.OpenAsync(cancellationToken);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            var languages = await ReadLanguagesAsync(reader, cancellationToken);

            await reader.NextResultAsync(cancellationToken);
            var menuRows = await ReadMenuRowsAsync(reader, cancellationToken);

            await reader.NextResultAsync(cancellationToken);
            var privacyPolicy = await ReadPrivacyPolicyAsync(reader, cancellationToken);

            await reader.NextResultAsync(cancellationToken);
            var contactInfo = await ReadContactInfoAsync(reader, cancellationToken);

            var currentLanguage = ResolveCurrentLanguage(languages, languageCode);

            return new PrivacyPolicyPage
            {
                CurrentLanguage = currentLanguage,
                Languages = languages,
                Menu = BuildMenu(menuRows),
                PrivacyPolicy = privacyPolicy,
                ContactInfo = contactInfo
            };
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (SqlException ex)
        {
            throw new DataManagerException("Failed to get privacy policy page.", ex);
        }
        catch (Exception ex)
        {
            throw new DataManagerException("Failed to get privacy policy page.", ex);
        }
    }

    public async Task<TermsOfUsePage> GetTermsOfUsePageAsync(
        string? languageCode = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = new SqlConnection(_connectionString);
            await using var command = new SqlCommand("dbo.usp_TermsOfUsePage_Get", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.Add(new SqlParameter("@LanguageCode", SqlDbType.NVarChar, 10)
            {
                Value = string.IsNullOrWhiteSpace(languageCode) ? DBNull.Value : languageCode
            });

            await connection.OpenAsync(cancellationToken);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            var languages = await ReadLanguagesAsync(reader, cancellationToken);

            await reader.NextResultAsync(cancellationToken);
            var menuRows = await ReadMenuRowsAsync(reader, cancellationToken);

            await reader.NextResultAsync(cancellationToken);
            var termsOfUse = await ReadTermsOfUseAsync(reader, cancellationToken);

            await reader.NextResultAsync(cancellationToken);
            var contactInfo = await ReadContactInfoAsync(reader, cancellationToken);

            var currentLanguage = ResolveCurrentLanguage(languages, languageCode);

            return new TermsOfUsePage
            {
                CurrentLanguage = currentLanguage,
                Languages = languages,
                Menu = BuildMenu(menuRows),
                TermsOfUse = termsOfUse,
                ContactInfo = contactInfo
            };
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (SqlException ex)
        {
            throw new DataManagerException("Failed to get terms of use page.", ex);
        }
        catch (Exception ex)
        {
            throw new DataManagerException("Failed to get terms of use page.", ex);
        }
    }

    public async Task<BlogPage> GetBlogPageAsync(
        string? languageCode = null,
        int pageNumber = 1,
        int pageSize = 6,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = new SqlConnection(_connectionString);
            await using var command = new SqlCommand("dbo.usp_BlogPage_Get", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.Add(new SqlParameter("@LanguageCode", SqlDbType.NVarChar, 10)
            {
                Value = string.IsNullOrWhiteSpace(languageCode) ? DBNull.Value : languageCode
            });
            command.Parameters.Add(new SqlParameter("@PageNumber", SqlDbType.Int)
            {
                Value = pageNumber
            });
            command.Parameters.Add(new SqlParameter("@PageSize", SqlDbType.Int)
            {
                Value = pageSize
            });

            await connection.OpenAsync(cancellationToken);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            var languages = await ReadLanguagesAsync(reader, cancellationToken);

            await reader.NextResultAsync(cancellationToken);
            var menuRows = await ReadMenuRowsAsync(reader, cancellationToken);

            await reader.NextResultAsync(cancellationToken);
            var paging = await ReadBlogPagingAsync(reader, cancellationToken);

            await reader.NextResultAsync(cancellationToken);
            var posts = await ReadBlogPostLinksAsync(reader, cancellationToken);

            await reader.NextResultAsync(cancellationToken);
            var contactInfo = await ReadContactInfoAsync(reader, cancellationToken);

            var currentLanguage = ResolveCurrentLanguage(languages, languageCode);

            return new BlogPage
            {
                CurrentLanguage = currentLanguage,
                Languages = languages,
                Menu = BuildMenu(menuRows),
                Posts = posts,
                PageNumber = paging.PageNumber,
                PageSize = paging.PageSize,
                TotalCount = paging.TotalCount,
                ContactInfo = contactInfo
            };
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (SqlException ex)
        {
            throw new DataManagerException("Failed to get blog page.", ex);
        }
        catch (Exception ex)
        {
            throw new DataManagerException("Failed to get blog page.", ex);
        }
    }

    public async Task<BlogPostPage> GetBlogPostPageAsync(
        string blogPostUrl,
        string? languageCode = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(blogPostUrl);

        try
        {
            await using var connection = new SqlConnection(_connectionString);
            await using var command = new SqlCommand("dbo.usp_BlogPostPage_Get", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.Add(new SqlParameter("@LanguageCode", SqlDbType.NVarChar, 10)
            {
                Value = string.IsNullOrWhiteSpace(languageCode) ? DBNull.Value : languageCode
            });
            command.Parameters.Add(new SqlParameter("@BlogPostUrl", SqlDbType.NVarChar, 500)
            {
                Value = blogPostUrl
            });

            await connection.OpenAsync(cancellationToken);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            var languages = await ReadLanguagesAsync(reader, cancellationToken);

            await reader.NextResultAsync(cancellationToken);
            var menuRows = await ReadMenuRowsAsync(reader, cancellationToken);

            await reader.NextResultAsync(cancellationToken);
            var latestPosts = await ReadBlogPostLinksAsync(reader, cancellationToken);

            await reader.NextResultAsync(cancellationToken);
            var post = await ReadBlogPostAsync(reader, cancellationToken);

            await reader.NextResultAsync(cancellationToken);
            var contactInfo = await ReadContactInfoAsync(reader, cancellationToken);

            var currentLanguage = ResolveCurrentLanguage(languages, languageCode);

            return new BlogPostPage
            {
                CurrentLanguage = currentLanguage,
                Languages = languages,
                Menu = BuildMenu(menuRows),
                Post = post,
                LatestPosts = latestPosts,
                ContactInfo = contactInfo
            };
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (SqlException ex)
        {
            throw new DataManagerException("Failed to get blog post page.", ex);
        }
        catch (Exception ex)
        {
            throw new DataManagerException("Failed to get blog post page.", ex);
        }
    }

    public async Task<HomePage> GetHomePageAsync(
        string? languageCode = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            languageCode = await ResolveActiveLanguageCodeAsync(languageCode, cancellationToken);

            await using var connection = new SqlConnection(_connectionString);
            await using var command = new SqlCommand("dbo.usp_HomePage_Get", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.Add(new SqlParameter("@LanguageCode", SqlDbType.NVarChar, 10)
            {
                Value = string.IsNullOrWhiteSpace(languageCode) ? DBNull.Value : languageCode
            });

            await connection.OpenAsync(cancellationToken);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            var languages = await ReadLanguagesAsync(reader, cancellationToken);

            await reader.NextResultAsync(cancellationToken);
            var menuRows = await ReadMenuRowsAsync(reader, cancellationToken);

            await reader.NextResultAsync(cancellationToken);
            var home = await ReadHomeContentAsync(reader, cancellationToken);

            await reader.NextResultAsync(cancellationToken);
            var features = await ReadHomeFeaturesAsync(reader, cancellationToken);

            await reader.NextResultAsync(cancellationToken);
            var practiceAreas = await ReadPracticeAreaLinksAsync(reader, cancellationToken);

            await reader.NextResultAsync(cancellationToken);
            var featuredBlogs = await ReadBlogPostLinksAsync(reader, cancellationToken);

            await reader.NextResultAsync(cancellationToken);
            var contactInfo = await ReadContactInfoAsync(reader, cancellationToken);

            var currentLanguage = ResolveCurrentLanguage(languages, languageCode);

            return new HomePage
            {
                CurrentLanguage = currentLanguage,
                Languages = languages,
                Menu = BuildMenu(menuRows),
                PracticeAreas = practiceAreas,
                FeaturedBlogs = featuredBlogs,
                ContactInfo = contactInfo,
                HeroEyebrow = home.HeroEyebrow,
                HeroTitle = home.HeroTitle,
                PageText = home.PageText,
                HeroCtaText = home.HeroCtaText,
                HeroCtaUrl = home.HeroCtaUrl,
                HeroVideoUrl = home.HeroVideoUrl,
                Features = features,
                StripBannerText = home.StripBannerText,
                SeoTitle = home.SeoTitle,
                MetaDescription = home.MetaDescription,
                MetaRobots = home.MetaRobots,
                OgTitle = home.OgTitle,
                OgDescription = home.OgDescription,
                OgImage = home.OgImage,
                OgType = home.OgType,
                OgUrl = home.OgUrl
            };
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (SqlException ex)
        {
            throw new DataManagerException("Failed to get home page.", ex);
        }
        catch (Exception ex)
        {
            throw new DataManagerException("Failed to get home page.", ex);
        }
    }

    private async Task<string> ResolveActiveLanguageCodeAsync(
        string? languageCode,
        CancellationToken cancellationToken)
    {
        var languages = await GetActiveLanguagesAsync(cancellationToken);
        return ResolveCurrentLanguage(languages, languageCode).Code;
    }

    private static Language ResolveCurrentLanguage(
        IReadOnlyList<Language> languages,
        string? languageCode)
    {
        if (languages.Count == 0)
        {
            throw new DataManagerException("No active languages found.");
        }

        if (!string.IsNullOrWhiteSpace(languageCode))
        {
            var matched = languages.FirstOrDefault(language =>
                string.Equals(language.Code, languageCode, StringComparison.OrdinalIgnoreCase));

            if (matched is not null)
            {
                return matched;
            }
        }

        return languages.FirstOrDefault(language => language.IsDefault) ?? languages[0];
    }

    private static async Task<List<Language>> ReadLanguagesAsync(
        SqlDataReader reader,
        CancellationToken cancellationToken)
    {
        var languages = new List<Language>();

        var codeOrdinal = reader.GetOrdinal("Code");
        var nativeNameOrdinal = reader.GetOrdinal("NativeName");
        var isRtlOrdinal = reader.GetOrdinal("IsRtl");
        var isDefaultOrdinal = reader.GetOrdinal("IsDefault");
        var sortOrderOrdinal = reader.GetOrdinal("SortOrder");

        while (await reader.ReadAsync(cancellationToken))
        {
            languages.Add(new Language
            {
                Code = reader.GetString(codeOrdinal),
                NativeName = reader.GetString(nativeNameOrdinal),
                IsRtl = reader.GetBoolean(isRtlOrdinal),
                IsDefault = reader.GetBoolean(isDefaultOrdinal),
                SortOrder = GetInt32Compat(reader, sortOrderOrdinal)
            });
        }

        return languages;
    }

    private static async Task<List<(int Id, int? ParentId, string? Url, string Title)>> ReadMenuRowsAsync(
        SqlDataReader reader,
        CancellationToken cancellationToken)
    {
        var rows = new List<(int Id, int? ParentId, string? Url, string Title)>();

        var idOrdinal = reader.GetOrdinal("Id");
        var parentIdOrdinal = reader.GetOrdinal("ParentId");
        var urlOrdinal = reader.GetOrdinal("Url");
        var titleOrdinal = reader.GetOrdinal("Title");

        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add((
                GetInt32Compat(reader, idOrdinal),
                reader.IsDBNull(parentIdOrdinal) ? null : GetInt32Compat(reader, parentIdOrdinal),
                reader.IsDBNull(urlOrdinal) ? null : reader.GetString(urlOrdinal),
                reader.GetString(titleOrdinal)));
        }

        return rows;
    }

    private static async Task<List<PracticeAreaLink>> ReadPracticeAreaLinksAsync(
        SqlDataReader reader,
        CancellationToken cancellationToken)
    {
        var links = new List<PracticeAreaLink>();

        var urlOrdinal = reader.GetOrdinal("Url");
        var nameOrdinal = reader.GetOrdinal("Name");
        var summaryOrdinal = reader.GetOrdinal("Summary");
        var imageUrlOrdinal = reader.GetOrdinal("ImageUrl");
        var iconUrlOrdinal = TryGetOrdinal(reader, "IconUrl");
        var sortOrderOrdinal = reader.GetOrdinal("SortOrder");

        while (await reader.ReadAsync(cancellationToken))
        {
            links.Add(new PracticeAreaLink
            {
                Url = reader.IsDBNull(urlOrdinal) ? null : reader.GetString(urlOrdinal),
                Name = reader.GetString(nameOrdinal),
                Summary = reader.IsDBNull(summaryOrdinal) ? null : reader.GetString(summaryOrdinal),
                ImageUrl = reader.IsDBNull(imageUrlOrdinal) ? null : reader.GetString(imageUrlOrdinal),
                IconUrl = iconUrlOrdinal is int iconIndex && !reader.IsDBNull(iconIndex)
                    ? reader.GetString(iconIndex)
                    : null,
                SortOrder = GetInt32Compat(reader, sortOrderOrdinal)
            });
        }

        return links;
    }

    private static async Task<(int TotalCount, int PageNumber, int PageSize)> ReadBlogPagingAsync(
        SqlDataReader reader,
        CancellationToken cancellationToken)
    {
        if (!await reader.ReadAsync(cancellationToken))
        {
            return (0, 1, 6);
        }

        var totalCount = GetInt32Compat(reader, reader.GetOrdinal("TotalCount"));
        var pageNumber = GetInt32Compat(reader, reader.GetOrdinal("PageNumber"));
        var pageSize = GetInt32Compat(reader, reader.GetOrdinal("PageSize"));
        return (totalCount, pageNumber, pageSize);
    }

    private static async Task<List<BlogPostLink>> ReadBlogPostLinksAsync(
        SqlDataReader reader,
        CancellationToken cancellationToken)
    {
        var posts = new List<BlogPostLink>();

        if (reader.FieldCount == 0)
        {
            return posts;
        }

        var urlOrdinal = TryGetOrdinal(reader, "Url");
        var imageUrlOrdinal = TryGetOrdinal(reader, "ImageUrl");
        var nameOrdinal = TryGetOrdinal(reader, "Name");
        if (urlOrdinal is null || imageUrlOrdinal is null || nameOrdinal is null)
        {
            return posts;
        }

        var excerptOrdinal = TryGetOrdinal(reader, "MetaDescription");

        while (await reader.ReadAsync(cancellationToken))
        {
            posts.Add(new BlogPostLink
            {
                Url = reader.IsDBNull(urlOrdinal.Value) ? null : reader.GetString(urlOrdinal.Value),
                ImageUrl = reader.IsDBNull(imageUrlOrdinal.Value) ? null : reader.GetString(imageUrlOrdinal.Value),
                Name = reader.GetString(nameOrdinal.Value),
                Excerpt = excerptOrdinal is int excerptIndex && !reader.IsDBNull(excerptIndex)
                    ? reader.GetString(excerptIndex)
                    : null
            });
        }

        return posts;
    }

    private static async Task<List<HomeFeature>> ReadHomeFeaturesAsync(
        SqlDataReader reader,
        CancellationToken cancellationToken)
    {
        var features = new List<HomeFeature>();

        var imageUrlOrdinal = reader.GetOrdinal("ImageUrl");
        var textOrdinal = reader.GetOrdinal("Text");
        var sortOrderOrdinal = reader.GetOrdinal("SortOrder");

        while (await reader.ReadAsync(cancellationToken))
        {
            features.Add(new HomeFeature
            {
                ImageUrl = reader.IsDBNull(imageUrlOrdinal) ? null : reader.GetString(imageUrlOrdinal),
                Name = string.Empty,
                Text = reader.IsDBNull(textOrdinal) ? null : reader.GetString(textOrdinal),
                SortOrder = GetInt32Compat(reader, sortOrderOrdinal)
            });
        }

        return features;
    }

    private static async Task<HomeContent> ReadHomeContentAsync(
        SqlDataReader reader,
        CancellationToken cancellationToken)
    {
        if (!await reader.ReadAsync(cancellationToken))
        {
            return new HomeContent(
                HeroEyebrow: null,
                HeroTitle: null,
                PageText: null,
                HeroCtaText: null,
                HeroCtaUrl: null,
                HeroVideoUrl: null,
                StripBannerText: null,
                SeoTitle: null,
                MetaDescription: null,
                MetaRobots: "index, follow",
                OgTitle: null,
                OgDescription: null,
                OgImage: null,
                OgType: "website",
                OgUrl: null);
        }

        return new HomeContent(
            HeroEyebrow: GetNullableString(reader, "HeroEyebrow"),
            HeroTitle: GetNullableString(reader, "HeroTitle"),
            PageText: GetNullableString(reader, "PageText"),
            HeroCtaText: GetNullableString(reader, "HeroCtaText"),
            HeroCtaUrl: GetNullableString(reader, "HeroCtaUrl"),
            HeroVideoUrl: GetNullableString(reader, "HeroVideoUrl"),
            StripBannerText: GetNullableString(reader, "StripBannerText"),
            SeoTitle: GetNullableString(reader, "SeoTitle"),
            MetaDescription: GetNullableString(reader, "MetaDescription"),
            MetaRobots: reader.GetString(reader.GetOrdinal("MetaRobots")),
            OgTitle: GetNullableString(reader, "OgTitle"),
            OgDescription: GetNullableString(reader, "OgDescription"),
            OgImage: GetNullableString(reader, "OgImage"),
            OgType: reader.GetString(reader.GetOrdinal("OgType")),
            OgUrl: GetNullableString(reader, "OgUrl"));
    }

    private static string? GetNullableString(SqlDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    }

    private sealed record HomeContent(
        string? HeroEyebrow,
        string? HeroTitle,
        string? PageText,
        string? HeroCtaText,
        string? HeroCtaUrl,
        string? HeroVideoUrl,
        string? StripBannerText,
        string? SeoTitle,
        string? MetaDescription,
        string MetaRobots,
        string? OgTitle,
        string? OgDescription,
        string? OgImage,
        string OgType,
        string? OgUrl);

    private static async Task<PracticeArea?> ReadPracticeAreaAsync(
        SqlDataReader reader,
        CancellationToken cancellationToken)
    {
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        var urlOrdinal = reader.GetOrdinal("Url");
        var nameOrdinal = reader.GetOrdinal("Name");
        var bodyOrdinal = reader.GetOrdinal("Body");
        var imageUrlOrdinal = reader.GetOrdinal("ImageUrl");
        var iconUrlOrdinal = TryGetOrdinal(reader, "IconUrl");
        var seoTitleOrdinal = reader.GetOrdinal("SeoTitle");
        var metaDescriptionOrdinal = reader.GetOrdinal("MetaDescription");
        var metaRobotsOrdinal = reader.GetOrdinal("MetaRobots");
        var ogTitleOrdinal = reader.GetOrdinal("OgTitle");
        var ogDescriptionOrdinal = reader.GetOrdinal("OgDescription");
        var ogImageOrdinal = reader.GetOrdinal("OgImage");
        var ogTypeOrdinal = reader.GetOrdinal("OgType");
        var ogUrlOrdinal = reader.GetOrdinal("OgUrl");

        return new PracticeArea
        {
            Url = reader.IsDBNull(urlOrdinal) ? null : reader.GetString(urlOrdinal),
            Name = reader.GetString(nameOrdinal),
            Body = reader.IsDBNull(bodyOrdinal) ? null : reader.GetString(bodyOrdinal),
            ImageUrl = reader.IsDBNull(imageUrlOrdinal) ? null : reader.GetString(imageUrlOrdinal),
            IconUrl = iconUrlOrdinal is int iconIndex && !reader.IsDBNull(iconIndex)
                ? reader.GetString(iconIndex)
                : null,
            SeoTitle = reader.IsDBNull(seoTitleOrdinal) ? null : reader.GetString(seoTitleOrdinal),
            MetaDescription = reader.IsDBNull(metaDescriptionOrdinal) ? null : reader.GetString(metaDescriptionOrdinal),
            MetaRobots = reader.GetString(metaRobotsOrdinal),
            OgTitle = reader.IsDBNull(ogTitleOrdinal) ? null : reader.GetString(ogTitleOrdinal),
            OgDescription = reader.IsDBNull(ogDescriptionOrdinal) ? null : reader.GetString(ogDescriptionOrdinal),
            OgImage = reader.IsDBNull(ogImageOrdinal) ? null : reader.GetString(ogImageOrdinal),
            OgType = reader.GetString(ogTypeOrdinal),
            OgUrl = reader.IsDBNull(ogUrlOrdinal) ? null : reader.GetString(ogUrlOrdinal)
        };
    }

    private static async Task<BlogPost?> ReadBlogPostAsync(
        SqlDataReader reader,
        CancellationToken cancellationToken)
    {
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        var urlOrdinal = reader.GetOrdinal("Url");
        var nameOrdinal = reader.GetOrdinal("Name");
        var bodyOrdinal = reader.GetOrdinal("Body");
        var imageUrlOrdinal = reader.GetOrdinal("ImageUrl");
        var seoTitleOrdinal = reader.GetOrdinal("SeoTitle");
        var metaDescriptionOrdinal = reader.GetOrdinal("MetaDescription");
        var metaRobotsOrdinal = reader.GetOrdinal("MetaRobots");
        var ogTitleOrdinal = reader.GetOrdinal("OgTitle");
        var ogDescriptionOrdinal = reader.GetOrdinal("OgDescription");
        var ogImageOrdinal = reader.GetOrdinal("OgImage");
        var ogTypeOrdinal = reader.GetOrdinal("OgType");
        var ogUrlOrdinal = reader.GetOrdinal("OgUrl");

        return new BlogPost
        {
            Url = reader.IsDBNull(urlOrdinal) ? null : reader.GetString(urlOrdinal),
            Name = reader.GetString(nameOrdinal),
            Body = reader.IsDBNull(bodyOrdinal) ? null : reader.GetString(bodyOrdinal),
            ImageUrl = reader.IsDBNull(imageUrlOrdinal) ? null : reader.GetString(imageUrlOrdinal),
            SeoTitle = reader.IsDBNull(seoTitleOrdinal) ? null : reader.GetString(seoTitleOrdinal),
            MetaDescription = reader.IsDBNull(metaDescriptionOrdinal) ? null : reader.GetString(metaDescriptionOrdinal),
            MetaRobots = reader.GetString(metaRobotsOrdinal),
            OgTitle = reader.IsDBNull(ogTitleOrdinal) ? null : reader.GetString(ogTitleOrdinal),
            OgDescription = reader.IsDBNull(ogDescriptionOrdinal) ? null : reader.GetString(ogDescriptionOrdinal),
            OgImage = reader.IsDBNull(ogImageOrdinal) ? null : reader.GetString(ogImageOrdinal),
            OgType = reader.GetString(ogTypeOrdinal),
            OgUrl = reader.IsDBNull(ogUrlOrdinal) ? null : reader.GetString(ogUrlOrdinal)
        };
    }

    private static async Task<About?> ReadAboutAsync(
        SqlDataReader reader,
        CancellationToken cancellationToken)
    {
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        var urlOrdinal = reader.GetOrdinal("Url");
        var nameOrdinal = reader.GetOrdinal("Name");
        var sloganOrdinal = reader.GetOrdinal("Slogan");
        var bodyOrdinal = reader.GetOrdinal("Body");
        var imageUrlOrdinal = reader.GetOrdinal("ImageUrl");
        var experienceYearsOrdinal = reader.GetOrdinal("ExperienceYears");
        var seoTitleOrdinal = reader.GetOrdinal("SeoTitle");
        var metaDescriptionOrdinal = reader.GetOrdinal("MetaDescription");
        var metaRobotsOrdinal = reader.GetOrdinal("MetaRobots");
        var ogTitleOrdinal = reader.GetOrdinal("OgTitle");
        var ogDescriptionOrdinal = reader.GetOrdinal("OgDescription");
        var ogImageOrdinal = reader.GetOrdinal("OgImage");
        var ogTypeOrdinal = reader.GetOrdinal("OgType");
        var ogUrlOrdinal = reader.GetOrdinal("OgUrl");

        return new About
        {
            Url = reader.IsDBNull(urlOrdinal) ? null : reader.GetString(urlOrdinal),
            Name = reader.GetString(nameOrdinal),
            Slogan = reader.IsDBNull(sloganOrdinal) ? null : reader.GetString(sloganOrdinal),
            Body = reader.IsDBNull(bodyOrdinal) ? null : reader.GetString(bodyOrdinal),
            ImageUrl = reader.IsDBNull(imageUrlOrdinal) ? null : reader.GetString(imageUrlOrdinal),
            ExperienceYears = reader.IsDBNull(experienceYearsOrdinal) ? null : GetInt32Compat(reader, experienceYearsOrdinal),
            SeoTitle = reader.IsDBNull(seoTitleOrdinal) ? null : reader.GetString(seoTitleOrdinal),
            MetaDescription = reader.IsDBNull(metaDescriptionOrdinal) ? null : reader.GetString(metaDescriptionOrdinal),
            MetaRobots = reader.GetString(metaRobotsOrdinal),
            OgTitle = reader.IsDBNull(ogTitleOrdinal) ? null : reader.GetString(ogTitleOrdinal),
            OgDescription = reader.IsDBNull(ogDescriptionOrdinal) ? null : reader.GetString(ogDescriptionOrdinal),
            OgImage = reader.IsDBNull(ogImageOrdinal) ? null : reader.GetString(ogImageOrdinal),
            OgType = reader.GetString(ogTypeOrdinal),
            OgUrl = reader.IsDBNull(ogUrlOrdinal) ? null : reader.GetString(ogUrlOrdinal)
        };
    }

    private static async Task<PrivacyPolicy?> ReadPrivacyPolicyAsync(
        SqlDataReader reader,
        CancellationToken cancellationToken)
    {
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        var urlOrdinal = reader.GetOrdinal("Url");
        var nameOrdinal = reader.GetOrdinal("Name");
        var bodyOrdinal = reader.GetOrdinal("Body");
        var seoTitleOrdinal = reader.GetOrdinal("SeoTitle");
        var metaDescriptionOrdinal = reader.GetOrdinal("MetaDescription");
        var metaRobotsOrdinal = reader.GetOrdinal("MetaRobots");
        var ogTitleOrdinal = reader.GetOrdinal("OgTitle");
        var ogDescriptionOrdinal = reader.GetOrdinal("OgDescription");
        var ogImageOrdinal = reader.GetOrdinal("OgImage");
        var ogTypeOrdinal = reader.GetOrdinal("OgType");
        var ogUrlOrdinal = reader.GetOrdinal("OgUrl");

        return new PrivacyPolicy
        {
            Url = reader.IsDBNull(urlOrdinal) ? null : reader.GetString(urlOrdinal),
            Name = reader.GetString(nameOrdinal),
            Body = reader.IsDBNull(bodyOrdinal) ? null : reader.GetString(bodyOrdinal),
            SeoTitle = reader.IsDBNull(seoTitleOrdinal) ? null : reader.GetString(seoTitleOrdinal),
            MetaDescription = reader.IsDBNull(metaDescriptionOrdinal) ? null : reader.GetString(metaDescriptionOrdinal),
            MetaRobots = reader.GetString(metaRobotsOrdinal),
            OgTitle = reader.IsDBNull(ogTitleOrdinal) ? null : reader.GetString(ogTitleOrdinal),
            OgDescription = reader.IsDBNull(ogDescriptionOrdinal) ? null : reader.GetString(ogDescriptionOrdinal),
            OgImage = reader.IsDBNull(ogImageOrdinal) ? null : reader.GetString(ogImageOrdinal),
            OgType = reader.GetString(ogTypeOrdinal),
            OgUrl = reader.IsDBNull(ogUrlOrdinal) ? null : reader.GetString(ogUrlOrdinal)
        };
    }

    private static async Task<TermsOfUse?> ReadTermsOfUseAsync(
        SqlDataReader reader,
        CancellationToken cancellationToken)
    {
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        var urlOrdinal = reader.GetOrdinal("Url");
        var nameOrdinal = reader.GetOrdinal("Name");
        var bodyOrdinal = reader.GetOrdinal("Body");
        var seoTitleOrdinal = reader.GetOrdinal("SeoTitle");
        var metaDescriptionOrdinal = reader.GetOrdinal("MetaDescription");
        var metaRobotsOrdinal = reader.GetOrdinal("MetaRobots");
        var ogTitleOrdinal = reader.GetOrdinal("OgTitle");
        var ogDescriptionOrdinal = reader.GetOrdinal("OgDescription");
        var ogImageOrdinal = reader.GetOrdinal("OgImage");
        var ogTypeOrdinal = reader.GetOrdinal("OgType");
        var ogUrlOrdinal = reader.GetOrdinal("OgUrl");

        return new TermsOfUse
        {
            Url = reader.IsDBNull(urlOrdinal) ? null : reader.GetString(urlOrdinal),
            Name = reader.GetString(nameOrdinal),
            Body = reader.IsDBNull(bodyOrdinal) ? null : reader.GetString(bodyOrdinal),
            SeoTitle = reader.IsDBNull(seoTitleOrdinal) ? null : reader.GetString(seoTitleOrdinal),
            MetaDescription = reader.IsDBNull(metaDescriptionOrdinal) ? null : reader.GetString(metaDescriptionOrdinal),
            MetaRobots = reader.GetString(metaRobotsOrdinal),
            OgTitle = reader.IsDBNull(ogTitleOrdinal) ? null : reader.GetString(ogTitleOrdinal),
            OgDescription = reader.IsDBNull(ogDescriptionOrdinal) ? null : reader.GetString(ogDescriptionOrdinal),
            OgImage = reader.IsDBNull(ogImageOrdinal) ? null : reader.GetString(ogImageOrdinal),
            OgType = reader.GetString(ogTypeOrdinal),
            OgUrl = reader.IsDBNull(ogUrlOrdinal) ? null : reader.GetString(ogUrlOrdinal)
        };
    }

    public async Task<Models.BlogImage?> GetBlogImageByFileNameAsync(
        string fileName,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

        try
        {
            await using var connection = new SqlConnection(_connectionString);
            await using var command = new SqlCommand("dbo.usp_BlogImages_GetByFileName", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.Add(new SqlParameter("@FileName", SqlDbType.NVarChar, 200) { Value = fileName });
            await connection.OpenAsync(cancellationToken);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                return null;
            }

            var contentOrdinal = reader.GetOrdinal("Content");
            return new Models.BlogImage
            {
                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                FileName = reader.GetString(reader.GetOrdinal("FileName")),
                ContentType = reader.GetString(reader.GetOrdinal("ContentType")),
                Content = reader.IsDBNull(contentOrdinal) ? [] : (byte[])reader.GetValue(contentOrdinal)
            };
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (SqlException ex)
        {
            throw new DataManagerException("Failed to get blog image.", ex);
        }
        catch (Exception ex)
        {
            throw new DataManagerException("Failed to get blog image.", ex);
        }
    }

    private static async Task<ContactInfo?> ReadContactInfoAsync(
        SqlDataReader reader,
        CancellationToken cancellationToken)
    {
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        var emailOrdinal = reader.GetOrdinal("Email");
        var phoneOrdinal = reader.GetOrdinal("Phone");
        var whatsAppOrdinal = reader.GetOrdinal("WhatsApp");

        return new ContactInfo
        {
            Email = reader.IsDBNull(emailOrdinal) ? null : reader.GetString(emailOrdinal),
            Phone = reader.IsDBNull(phoneOrdinal) ? null : reader.GetString(phoneOrdinal),
            WhatsApp = reader.IsDBNull(whatsAppOrdinal) ? null : reader.GetString(whatsAppOrdinal)
        };
    }

    private static int GetInt32Compat(SqlDataReader reader, int ordinal)
    {
        if (reader.IsDBNull(ordinal))
        {
            return 0;
        }

        return Convert.ToInt32(reader.GetValue(ordinal), System.Globalization.CultureInfo.InvariantCulture);
    }

    private static int? TryGetOrdinal(SqlDataReader reader, string columnName)
    {
        for (var i = 0; i < reader.FieldCount; i++)
        {
            if (string.Equals(reader.GetName(i), columnName, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return null;
    }
}
