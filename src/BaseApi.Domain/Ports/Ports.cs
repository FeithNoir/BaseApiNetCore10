using BaseApi.Domain.Tokens;
using BaseApi.Domain.Users;

namespace BaseApi.Domain.Ports;

// Puertos: el dominio dice QUÉ necesita; Infrastructure decide CÓMO.

public interface IUserRepository
{
    Task<User?> FindByEmailAsync(Email email, CancellationToken ct);
    Task<User?> FindByIdAsync(Guid id, CancellationToken ct);
    Task AddAsync(User user, CancellationToken ct);
    Task UpdateAsync(User user, CancellationToken ct);
}

public interface IRefreshTokenRepository
{
    Task AddAsync(RefreshToken token, CancellationToken ct);
    Task<RefreshToken?> FindByHashAsync(string hash, CancellationToken ct);
    Task UpdateAsync(RefreshToken token, CancellationToken ct);
    Task RevokeAllForUserAsync(Guid userId, DateTimeOffset at, CancellationToken ct);
}

public interface IOneTimeTokenRepository
{
    Task AddAsync(OneTimeToken token, CancellationToken ct);
    Task<OneTimeToken?> FindValidAsync(string hash, TokenPurpose purpose, DateTimeOffset now, CancellationToken ct);
    Task UpdateAsync(OneTimeToken token, CancellationToken ct);
}

public interface IPasswordHasher
{
    string Hash(string plain);
    bool Verify(string plain, string hash);
}

public sealed record IssuedAccessToken(string Token, int ExpiresIn);

public interface IAccessTokenIssuer
{
    IssuedAccessToken Issue(User user);
}

public sealed record GeneratedToken(string Value, string Hash);

public interface IOpaqueTokens
{
    GeneratedToken Generate();
    string Hash(string value);
}

public sealed record EmailMessage(string To, string Subject, string Html, string Text);

public interface IEmailSender
{
    Task SendAsync(EmailMessage message, CancellationToken ct);
}

public interface IEmailRenderer
{
    EmailMessage Render(string template, string to, string subject, IReadOnlyDictionary<string, string> data);
}
