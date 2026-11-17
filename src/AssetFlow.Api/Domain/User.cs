namespace AssetFlow.Api.Domain;

public class User
{
    public int Id { get; private set; }
    public string Name { get; private set; } = "";
    public string Email { get; private set; } = "";
    public string PasswordHash { get; private set; } = "";
    public UserRole Role { get; private set; }
    public string? Department { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private User() { } // for EF Core

    public User(string name, string email, UserRole role, string? department, DateTime createdAt)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new DomainException("Name is required.");
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@')) throw new DomainException("A valid email is required.");

        Name = name.Trim();
        Email = NormalizeEmail(email);
        Role = role;
        Department = department?.Trim();
        CreatedAt = createdAt;
    }

    public static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    public void SetPasswordHash(string hash) => PasswordHash = hash;

    public bool IsAdmin => Role == UserRole.Admin;
}
