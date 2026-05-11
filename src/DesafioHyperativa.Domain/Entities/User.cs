namespace DesafioHyperativa.Domain.Entities;

public class User
{
    public Guid Id { get; private set; }
    public string Username { get; private set; } = null!;
    public string Email { get; private set; } = null!;
    public string PasswordHash { get; private set; } = null!;
    public string[] Roles { get; private set; } = [];
    public DateTime CreatedAt { get; private set; }

    protected User() { }

    public User(string username, string email, string passwordHash, string[] roles)
    {
        Id = Guid.NewGuid();
        Username = username;
        Email = email;
        PasswordHash = passwordHash;
        Roles = roles;
        CreatedAt = DateTime.UtcNow;
    }
}
