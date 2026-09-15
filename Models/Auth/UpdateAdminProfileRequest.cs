namespace Models.Auth;

public class UpdateAdminProfileRequest
{
    public required string Email { get; set; }

    public required string DisplayName { get; set; }

    public string? Password { get; set; }
}
