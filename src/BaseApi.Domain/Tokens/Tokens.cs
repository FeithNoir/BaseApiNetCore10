namespace BaseApi.Domain.Tokens;

public enum TokenPurpose { VerifyEmail, ResetPassword }

public class RefreshToken
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; } = "";
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private RefreshToken() { }

    public static RefreshToken Create(Guid userId, string hash, DateTimeOffset now, TimeSpan ttl) => new()
    {
        Id = Guid.CreateVersion7(now), UserId = userId, TokenHash = hash, CreatedAt = now, ExpiresAt = now + ttl,
    };

    public bool IsRevoked => RevokedAt is not null;
    public bool IsExpired(DateTimeOffset now) => ExpiresAt <= now;
    public void Revoke(DateTimeOffset now) => RevokedAt ??= now;
}

public class OneTimeToken
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public TokenPurpose Purpose { get; private set; }
    public string TokenHash { get; private set; } = "";
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset? UsedAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private OneTimeToken() { }

    public static OneTimeToken Create(Guid userId, TokenPurpose purpose, string hash, DateTimeOffset now, TimeSpan ttl) => new()
    {
        Id = Guid.CreateVersion7(now), UserId = userId, Purpose = purpose, TokenHash = hash, CreatedAt = now, ExpiresAt = now + ttl,
    };

    public void MarkUsed(DateTimeOffset now) => UsedAt = now;
}
