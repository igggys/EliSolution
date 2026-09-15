namespace DataManager.AdministratorSite;

using System.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Models.Auth;

public class AuthDataManager
{
    private readonly string _connectionString;

    public AuthDataManager(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        _connectionString = connectionString;
    }

    public async Task<AdminUser?> ValidateLoginAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        try
        {
            await using var connection = new SqlConnection(_connectionString);
            await using var command = new SqlCommand("dbo.usp_AdminUsers_ValidateLogin", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            command.Parameters.Add(new SqlParameter("@Email", SqlDbType.NVarChar, 256) { Value = email });

            await connection.OpenAsync(cancellationToken);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                return null;
            }

            var user = MapAdminUser(reader);
            var hasher = new PasswordHasher<AdminUser>();
            var verification = hasher.VerifyHashedPassword(user, user.PasswordHash, password);
            if (verification == PasswordVerificationResult.Failed)
            {
                return null;
            }

            return user;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (SqlException ex)
        {
            throw new DataManagerException("Failed to validate admin login.", ex);
        }
        catch (Exception ex)
        {
            throw new DataManagerException("Failed to validate admin login.", ex);
        }
    }

    public async Task<AdminUser?> GetAdminUserByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = new SqlConnection(_connectionString);
            await using var command = new SqlCommand("dbo.usp_AdminUsers_GetById", connection)
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

            return MapAdminUser(reader);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (SqlException ex)
        {
            throw new DataManagerException("Failed to get admin user by id.", ex);
        }
        catch (Exception ex)
        {
            throw new DataManagerException("Failed to get admin user by id.", ex);
        }
    }

    public async Task UpdateAdminUserAsync(
        int id,
        string email,
        string displayName,
        string? passwordHash,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);

        try
        {
            await using var connection = new SqlConnection(_connectionString);
            await using var command = new SqlCommand("dbo.usp_AdminUsers_Update", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.Add(new SqlParameter("@Id", SqlDbType.Int) { Value = id });
            command.Parameters.Add(new SqlParameter("@Email", SqlDbType.NVarChar, 256) { Value = email });
            command.Parameters.Add(new SqlParameter("@DisplayName", SqlDbType.NVarChar, 200) { Value = displayName });
            command.Parameters.Add(new SqlParameter("@PasswordHash", SqlDbType.NVarChar, 500)
            {
                Value = string.IsNullOrWhiteSpace(passwordHash) ? DBNull.Value : passwordHash
            });

            await connection.OpenAsync(cancellationToken);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (SqlException ex)
        {
            throw new DataManagerException("Failed to update admin user.", ex);
        }
        catch (Exception ex)
        {
            throw new DataManagerException("Failed to update admin user.", ex);
        }
    }

    public async Task<int> InsertAdminUserAsync(AdminUser user, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(user);

        try
        {
            await using var connection = new SqlConnection(_connectionString);
            await using var command = new SqlCommand("dbo.usp_AdminUsers_Insert", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.Add(new SqlParameter("@Email", SqlDbType.NVarChar, 256) { Value = user.Email });
            command.Parameters.Add(new SqlParameter("@PasswordHash", SqlDbType.NVarChar, 500) { Value = user.PasswordHash });
            command.Parameters.Add(new SqlParameter("@DisplayName", SqlDbType.NVarChar, 200) { Value = user.DisplayName });
            command.Parameters.Add(new SqlParameter("@IsActive", SqlDbType.Bit) { Value = user.IsActive });

            var idParameter = new SqlParameter("@Id", SqlDbType.Int)
            {
                Direction = ParameterDirection.Output
            };
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
            throw new DataManagerException("Failed to insert admin user.", ex);
        }
        catch (Exception ex)
        {
            throw new DataManagerException("Failed to insert admin user.", ex);
        }
    }

    private static AdminUser MapAdminUser(SqlDataReader reader)
    {
        return new AdminUser
        {
            Id = reader.GetInt32(reader.GetOrdinal("Id")),
            Email = reader.GetString(reader.GetOrdinal("Email")),
            PasswordHash = reader.GetString(reader.GetOrdinal("PasswordHash")),
            DisplayName = reader.GetString(reader.GetOrdinal("DisplayName")),
            IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"))
        };
    }
}
