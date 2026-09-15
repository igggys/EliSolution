namespace Models.Auth;

public class AdminUser
{
    public int Id { get; set; }

    /// <summary>Login identifier. Column name is Email; the value can be a simple name.</summary>
    public required string Email { get; set; }

    public required string PasswordHash { get; set; }

    public required string DisplayName { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }
}
