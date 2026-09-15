namespace Models.Auth;

public class LoginResponse
{
    public required string AccessToken { get; set; }

    public DateTimeOffset ExpiresAt { get; set; }

    public required string DisplayName { get; set; }

    public required string Email { get; set; }
}
