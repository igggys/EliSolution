namespace DataManager.AdministratorSite;

using System.Data;
using Microsoft.Data.SqlClient;
using Models.AdministratorSite;

public class DataManager
{
    private readonly string _connectionString;

    public DataManager(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        _connectionString = connectionString;
    }

    public async Task<IReadOnlyList<Language>> GetAllLanguagesAsync(
        bool activeOnly = false,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var languages = new List<Language>();

            await using var connection = new SqlConnection(_connectionString);
            await using var command = new SqlCommand("dbo.usp_Languages_GetAll", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            command.Parameters.Add(new SqlParameter("@ActiveOnly", SqlDbType.Bit) { Value = activeOnly });

            await connection.OpenAsync(cancellationToken);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            var idOrdinal = reader.GetOrdinal("Id");
            var codeOrdinal = reader.GetOrdinal("Code");
            var nameOrdinal = reader.GetOrdinal("Name");
            var nativeNameOrdinal = reader.GetOrdinal("NativeName");
            var isRtlOrdinal = reader.GetOrdinal("IsRtl");
            var isDefaultOrdinal = reader.GetOrdinal("IsDefault");
            var isActiveOrdinal = reader.GetOrdinal("IsActive");
            var sortOrderOrdinal = reader.GetOrdinal("SortOrder");

            while (await reader.ReadAsync(cancellationToken))
            {
                languages.Add(new Language
                {
                    Id = reader.GetByte(idOrdinal),
                    Code = reader.GetString(codeOrdinal),
                    Name = reader.GetString(nameOrdinal),
                    NativeName = reader.GetString(nativeNameOrdinal),
                    IsRtl = reader.GetBoolean(isRtlOrdinal),
                    IsDefault = reader.GetBoolean(isDefaultOrdinal),
                    IsActive = reader.GetBoolean(isActiveOrdinal),
                    SortOrder = reader.GetInt32(sortOrderOrdinal)
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
            throw new DataManagerException("Failed to get languages.", ex);
        }
        catch (Exception ex)
        {
            throw new DataManagerException("Failed to get languages.", ex);
        }
    }

    public async Task<byte> InsertLanguageAsync(Language language, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(language);

        try
        {
            await using var connection = new SqlConnection(_connectionString);
            await using var command = new SqlCommand("dbo.usp_Languages_Insert", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.Add(new SqlParameter("@Code", SqlDbType.NVarChar, 10) { Value = language.Code });
            command.Parameters.Add(new SqlParameter("@Name", SqlDbType.NVarChar, 100) { Value = language.Name });
            command.Parameters.Add(new SqlParameter("@NativeName", SqlDbType.NVarChar, 100) { Value = language.NativeName });
            command.Parameters.Add(new SqlParameter("@IsRtl", SqlDbType.Bit) { Value = language.IsRtl });
            command.Parameters.Add(new SqlParameter("@IsDefault", SqlDbType.Bit) { Value = language.IsDefault });
            command.Parameters.Add(new SqlParameter("@IsActive", SqlDbType.Bit) { Value = language.IsActive });
            command.Parameters.Add(new SqlParameter("@SortOrder", SqlDbType.Int) { Value = language.SortOrder });

            var idParameter = new SqlParameter("@Id", SqlDbType.TinyInt)
            {
                Direction = ParameterDirection.Output
            };
            command.Parameters.Add(idParameter);

            await connection.OpenAsync(cancellationToken);
            await command.ExecuteNonQueryAsync(cancellationToken);

            return (byte)idParameter.Value!;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (SqlException ex)
        {
            throw new DataManagerException("Failed to insert language.", ex);
        }
        catch (Exception ex)
        {
            throw new DataManagerException("Failed to insert language.", ex);
        }
    }

    public async Task UpdateLanguageAsync(Language language, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(language);

        try
        {
            await using var connection = new SqlConnection(_connectionString);
            await using var command = new SqlCommand("dbo.usp_Languages_Update", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.Add(new SqlParameter("@Id", SqlDbType.TinyInt) { Value = language.Id });
            command.Parameters.Add(new SqlParameter("@Code", SqlDbType.NVarChar, 10) { Value = language.Code });
            command.Parameters.Add(new SqlParameter("@Name", SqlDbType.NVarChar, 100) { Value = language.Name });
            command.Parameters.Add(new SqlParameter("@NativeName", SqlDbType.NVarChar, 100) { Value = language.NativeName });
            command.Parameters.Add(new SqlParameter("@IsRtl", SqlDbType.Bit) { Value = language.IsRtl });
            command.Parameters.Add(new SqlParameter("@IsDefault", SqlDbType.Bit) { Value = language.IsDefault });
            command.Parameters.Add(new SqlParameter("@IsActive", SqlDbType.Bit) { Value = language.IsActive });
            command.Parameters.Add(new SqlParameter("@SortOrder", SqlDbType.Int) { Value = language.SortOrder });

            await connection.OpenAsync(cancellationToken);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (SqlException ex)
        {
            throw new DataManagerException("Failed to update language.", ex);
        }
        catch (Exception ex)
        {
            throw new DataManagerException("Failed to update language.", ex);
        }
    }

    public async Task<IReadOnlyList<BlogPost>> GetAllBlogPostsAsync(
        byte? languageId = null,
        bool activeOnly = false,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var posts = new List<BlogPost>();

            await using var connection = new SqlConnection(_connectionString);
            await using var command = new SqlCommand("dbo.usp_BlogPosts_GetAll", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            command.Parameters.Add(new SqlParameter("@LanguageId", SqlDbType.TinyInt)
            {
                Value = languageId.HasValue ? languageId.Value : DBNull.Value
            });
            command.Parameters.Add(new SqlParameter("@ActiveOnly", SqlDbType.Bit) { Value = activeOnly });

            await connection.OpenAsync(cancellationToken);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                posts.Add(MapBlogPost(reader));
            }

            return posts;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (SqlException ex)
        {
            throw new DataManagerException("Failed to get blog posts.", ex);
        }
        catch (Exception ex)
        {
            throw new DataManagerException("Failed to get blog posts.", ex);
        }
    }

    public async Task<BlogPost?> GetBlogPostByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = new SqlConnection(_connectionString);
            await using var command = new SqlCommand("dbo.usp_BlogPosts_GetById", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            command.Parameters.Add(new SqlParameter("@Id", SqlDbType.Int) { Value = id });

            await connection.OpenAsync(cancellationToken);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            if (!await reader.ReadAsync(cancellationToken))
            {
                return null;
            }

            return MapBlogPost(reader);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (SqlException ex)
        {
            throw new DataManagerException("Failed to get blog post.", ex);
        }
        catch (Exception ex)
        {
            throw new DataManagerException("Failed to get blog post.", ex);
        }
    }

    public async Task<int> InsertBlogPostAsync(BlogPost post, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(post);
        ArgumentException.ThrowIfNullOrWhiteSpace(post.Url);
        ArgumentException.ThrowIfNullOrWhiteSpace(post.Name);

        try
        {
            await using var connection = new SqlConnection(_connectionString);
            await using var command = new SqlCommand("dbo.usp_BlogPosts_Insert", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.Add(new SqlParameter("@LanguageId", SqlDbType.TinyInt) { Value = post.LanguageId });
            command.Parameters.Add(new SqlParameter("@Url", SqlDbType.NVarChar, 500) { Value = post.Url });
            command.Parameters.Add(new SqlParameter("@Name", SqlDbType.NVarChar, 200) { Value = post.Name });
            command.Parameters.Add(new SqlParameter("@Body", SqlDbType.NVarChar, -1)
            {
                Value = (object?)post.Body ?? DBNull.Value
            });
            command.Parameters.Add(new SqlParameter("@ImageUrl", SqlDbType.NVarChar, 500)
            {
                Value = ToDbValue(post.ImageUrl)
            });
            command.Parameters.Add(new SqlParameter("@PublishedAt", SqlDbType.DateTime2)
            {
                Value = (object?)post.PublishedAt ?? DBNull.Value
            });
            command.Parameters.Add(new SqlParameter("@IsPublished", SqlDbType.Bit) { Value = post.IsPublished });
            command.Parameters.Add(new SqlParameter("@SortOrder", SqlDbType.Int)
            {
                Value = (object?)post.SortOrder ?? DBNull.Value
            });
            command.Parameters.Add(new SqlParameter("@IsActive", SqlDbType.Bit) { Value = post.IsActive });
            command.Parameters.Add(new SqlParameter("@SeoTitle", SqlDbType.NVarChar, 200)
            {
                Value = ToDbValue(post.SeoTitle)
            });
            command.Parameters.Add(new SqlParameter("@MetaDescription", SqlDbType.NVarChar, 500)
            {
                Value = ToDbValue(post.MetaDescription)
            });
            command.Parameters.Add(new SqlParameter("@MetaRobots", SqlDbType.NVarChar, 100)
            {
                Value = ToDbValue(post.MetaRobots)
            });
            command.Parameters.Add(new SqlParameter("@OgTitle", SqlDbType.NVarChar, 200)
            {
                Value = ToDbValue(post.OgTitle)
            });
            command.Parameters.Add(new SqlParameter("@OgDescription", SqlDbType.NVarChar, 500)
            {
                Value = ToDbValue(post.OgDescription)
            });
            command.Parameters.Add(new SqlParameter("@OgImage", SqlDbType.NVarChar, 500)
            {
                Value = ToDbValue(post.OgImage)
            });
            command.Parameters.Add(new SqlParameter("@OgType", SqlDbType.NVarChar, 50)
            {
                Value = ToDbValue(post.OgType)
            });
            command.Parameters.Add(new SqlParameter("@OgUrl", SqlDbType.NVarChar, 500)
            {
                Value = ToDbValue(post.OgUrl)
            });
            command.Parameters.Add(new SqlParameter("@IsFeatured", SqlDbType.Bit) { Value = post.IsFeatured });

            var idParameter = new SqlParameter("@Id", SqlDbType.Int)
            {
                Direction = ParameterDirection.Output
            };
            command.Parameters.Add(idParameter);

            await connection.OpenAsync(cancellationToken);
            await command.ExecuteNonQueryAsync(cancellationToken);

            post.Id = (int)idParameter.Value!;
            return post.Id;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (SqlException ex)
        {
            throw new DataManagerException("Failed to insert blog post.", ex);
        }
        catch (Exception ex)
        {
            throw new DataManagerException("Failed to insert blog post.", ex);
        }
    }

    public async Task<bool> UpdateBlogPostAsync(BlogPost post, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(post);
        ArgumentException.ThrowIfNullOrWhiteSpace(post.Url);
        ArgumentException.ThrowIfNullOrWhiteSpace(post.Name);

        try
        {
            await using var connection = new SqlConnection(_connectionString);
            await using var command = new SqlCommand("dbo.usp_BlogPosts_Update", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.Add(new SqlParameter("@Id", SqlDbType.Int) { Value = post.Id });
            command.Parameters.Add(new SqlParameter("@LanguageId", SqlDbType.TinyInt) { Value = post.LanguageId });
            command.Parameters.Add(new SqlParameter("@Url", SqlDbType.NVarChar, 500) { Value = post.Url });
            command.Parameters.Add(new SqlParameter("@Name", SqlDbType.NVarChar, 200) { Value = post.Name });
            command.Parameters.Add(new SqlParameter("@Body", SqlDbType.NVarChar, -1)
            {
                Value = (object?)post.Body ?? DBNull.Value
            });
            command.Parameters.Add(new SqlParameter("@ImageUrl", SqlDbType.NVarChar, 500)
            {
                Value = ToDbValue(post.ImageUrl)
            });
            command.Parameters.Add(new SqlParameter("@PublishedAt", SqlDbType.DateTime2)
            {
                Value = (object?)post.PublishedAt ?? DBNull.Value
            });
            command.Parameters.Add(new SqlParameter("@IsPublished", SqlDbType.Bit) { Value = post.IsPublished });
            command.Parameters.Add(new SqlParameter("@SortOrder", SqlDbType.Int)
            {
                Value = (object?)post.SortOrder ?? DBNull.Value
            });
            command.Parameters.Add(new SqlParameter("@IsActive", SqlDbType.Bit) { Value = post.IsActive });
            command.Parameters.Add(new SqlParameter("@SeoTitle", SqlDbType.NVarChar, 200)
            {
                Value = ToDbValue(post.SeoTitle)
            });
            command.Parameters.Add(new SqlParameter("@MetaDescription", SqlDbType.NVarChar, 500)
            {
                Value = ToDbValue(post.MetaDescription)
            });
            command.Parameters.Add(new SqlParameter("@MetaRobots", SqlDbType.NVarChar, 100)
            {
                Value = ToDbValue(post.MetaRobots)
            });
            command.Parameters.Add(new SqlParameter("@OgTitle", SqlDbType.NVarChar, 200)
            {
                Value = ToDbValue(post.OgTitle)
            });
            command.Parameters.Add(new SqlParameter("@OgDescription", SqlDbType.NVarChar, 500)
            {
                Value = ToDbValue(post.OgDescription)
            });
            command.Parameters.Add(new SqlParameter("@OgImage", SqlDbType.NVarChar, 500)
            {
                Value = ToDbValue(post.OgImage)
            });
            command.Parameters.Add(new SqlParameter("@OgType", SqlDbType.NVarChar, 50)
            {
                Value = ToDbValue(post.OgType)
            });
            command.Parameters.Add(new SqlParameter("@OgUrl", SqlDbType.NVarChar, 500)
            {
                Value = ToDbValue(post.OgUrl)
            });
            command.Parameters.Add(new SqlParameter("@IsFeatured", SqlDbType.Bit) { Value = post.IsFeatured });

            await connection.OpenAsync(cancellationToken);
            await command.ExecuteNonQueryAsync(cancellationToken);
            return true;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (SqlException ex) when (ex.Number == 50045)
        {
            return false;
        }
        catch (SqlException ex) when (ex.Number is 50041 or 50042 or 50043 or 50044)
        {
            throw new InvalidOperationException(ex.Message, ex);
        }
        catch (SqlException ex)
        {
            throw new DataManagerException("Failed to update blog post.", ex);
        }
        catch (Exception ex)
        {
            throw new DataManagerException("Failed to update blog post.", ex);
        }
    }

    public async Task<bool> DeleteBlogPostAsync(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = new SqlConnection(_connectionString);
            await using var command = new SqlCommand("dbo.usp_BlogPosts_Delete", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            command.Parameters.Add(new SqlParameter("@Id", SqlDbType.Int) { Value = id });

            await connection.OpenAsync(cancellationToken);
            await command.ExecuteNonQueryAsync(cancellationToken);
            return true;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (SqlException ex) when (ex.Number == 50045)
        {
            return false;
        }
        catch (SqlException ex)
        {
            throw new DataManagerException("Failed to delete blog post.", ex);
        }
        catch (Exception ex)
        {
            throw new DataManagerException("Failed to delete blog post.", ex);
        }
    }

    public async Task<int> InsertBlogImageAsync(
        string fileName,
        string contentType,
        byte[] content,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);
        ArgumentNullException.ThrowIfNull(content);

        try
        {
            await using var connection = new SqlConnection(_connectionString);
            await using var command = new SqlCommand("dbo.usp_BlogImages_Insert", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.Add(new SqlParameter("@FileName", SqlDbType.NVarChar, 200) { Value = fileName });
            command.Parameters.Add(new SqlParameter("@ContentType", SqlDbType.NVarChar, 100) { Value = contentType });
            command.Parameters.Add(new SqlParameter("@Content", SqlDbType.VarBinary, -1) { Value = content });
            var idParameter = new SqlParameter("@Id", SqlDbType.Int) { Direction = ParameterDirection.Output };
            command.Parameters.Add(idParameter);

            await connection.OpenAsync(cancellationToken);
            await command.ExecuteNonQueryAsync(cancellationToken);
            return (int)idParameter.Value!;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (SqlException ex)
        {
            throw new DataManagerException("Failed to insert blog image.", ex);
        }
        catch (Exception ex)
        {
            throw new DataManagerException("Failed to insert blog image.", ex);
        }
    }

    public async Task<bool> BlogImageExistsAsync(string fileName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

        try
        {
            await using var connection = new SqlConnection(_connectionString);
            await using var command = new SqlCommand("dbo.usp_BlogImages_Exists", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.Add(new SqlParameter("@FileName", SqlDbType.NVarChar, 200) { Value = fileName });
            var existsParameter = new SqlParameter("@Exists", SqlDbType.Bit)
            {
                Direction = ParameterDirection.Output
            };
            command.Parameters.Add(existsParameter);

            await connection.OpenAsync(cancellationToken);
            await command.ExecuteNonQueryAsync(cancellationToken);
            return existsParameter.Value is not null
                && existsParameter.Value is not DBNull
                && Convert.ToBoolean(existsParameter.Value);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (SqlException ex)
        {
            throw new DataManagerException("Failed to check blog image.", ex);
        }
        catch (Exception ex)
        {
            throw new DataManagerException("Failed to check blog image.", ex);
        }
    }

    public async Task DeleteBlogImageAsync(string fileName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return;
        }

        try
        {
            await using var connection = new SqlConnection(_connectionString);
            await using var command = new SqlCommand("dbo.usp_BlogImages_Delete", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.Add(new SqlParameter("@FileName", SqlDbType.NVarChar, 200) { Value = fileName });
            await connection.OpenAsync(cancellationToken);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (SqlException ex)
        {
            throw new DataManagerException("Failed to delete blog image.", ex);
        }
        catch (Exception ex)
        {
            throw new DataManagerException("Failed to delete blog image.", ex);
        }
    }

    private static object ToDbValue(string? value) =>
        string.IsNullOrWhiteSpace(value) ? DBNull.Value : value;

    private static BlogPost MapBlogPost(SqlDataReader reader)
    {
        var publishedAtOrdinal = reader.GetOrdinal("PublishedAt");
        var sortOrderOrdinal = reader.GetOrdinal("SortOrder");

        return new BlogPost
        {
            Id = reader.GetInt32(reader.GetOrdinal("Id")),
            LanguageId = reader.GetByte(reader.GetOrdinal("LanguageId")),
            Url = reader.GetString(reader.GetOrdinal("Url")),
            Name = reader.GetString(reader.GetOrdinal("Name")),
            Body = GetNullableString(reader, "Body"),
            ImageUrl = GetNullableString(reader, "ImageUrl"),
            PublishedAt = reader.IsDBNull(publishedAtOrdinal) ? null : reader.GetDateTime(publishedAtOrdinal),
            IsPublished = reader.GetBoolean(reader.GetOrdinal("IsPublished")),
            SortOrder = reader.IsDBNull(sortOrderOrdinal) ? null : reader.GetInt32(sortOrderOrdinal),
            IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
            SeoTitle = GetNullableString(reader, "SeoTitle"),
            MetaDescription = GetNullableString(reader, "MetaDescription"),
            MetaRobots = GetNullableString(reader, "MetaRobots"),
            OgTitle = GetNullableString(reader, "OgTitle"),
            OgDescription = GetNullableString(reader, "OgDescription"),
            OgImage = GetNullableString(reader, "OgImage"),
            OgType = GetNullableString(reader, "OgType"),
            OgUrl = GetNullableString(reader, "OgUrl"),
            IsFeatured = reader.GetBoolean(reader.GetOrdinal("IsFeatured"))
        };
    }

    private static string? GetNullableString(SqlDataReader reader, string columnName)
    {
        var ordinal = TryGetOrdinal(reader, columnName);
        if (ordinal is null || reader.IsDBNull(ordinal.Value))
        {
            return null;
        }

        return reader.GetString(ordinal.Value);
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
