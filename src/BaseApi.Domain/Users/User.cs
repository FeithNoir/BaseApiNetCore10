namespace BaseApi.Domain.Users;

/// <summary>
/// Entidad de dominio. Setters privados: el estado solo cambia por métodos con nombre de negocio.
/// EF Core la mapea desde Infrastructure con Fluent API, sin atributos aquí.
/// </summary>
public class User
{
    public Guid Id { get; private set; }
    public Email Email { get; private set; } = null!;
    public string Name { get; private set; } = "";
    public string PasswordHash { get; private set; } = "";
    public DateTimeOffset? EmailVerifiedAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private User() { } // para EF Core

    public static User Register(Email email, string name, string passwordHash, DateTimeOffset now) => new()
    {
        Id = Guid.CreateVersion7(now),
        Email = email,
        Name = name.Trim(),
        PasswordHash = passwordHash,
        CreatedAt = now,
        UpdatedAt = now,
    };

    /// <summary>Regla única de la serie: 8 a 72 caracteres.</summary>
    public static void ValidatePassword(string? raw)
    {
        var len = raw is null ? 0 : raw.EnumerateRunes().Count();
        if (len is < 8 or > 72)
            throw DomainException.Validation("password: debe tener entre 8 y 72 caracteres");
    }

    public bool IsVerified => EmailVerifiedAt is not null;

    public void VerifyEmail(DateTimeOffset now)
    {
        if (EmailVerifiedAt is not null) return;
        EmailVerifiedAt = now;
        UpdatedAt = now;
    }

    public void ChangePassword(string newHash, DateTimeOffset now)
    {
        PasswordHash = newHash;
        UpdatedAt = now;
    }
}
