using BaseApi.Domain;
using BaseApi.Domain.Ports;
using BaseApi.Domain.Tokens;
using BaseApi.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace BaseApi.Infrastructure.Persistence;

// Cada método guarda al terminar (SaveChangesAsync). Así una excepción de dominio posterior
// no deshace lo ya escrito, como la revocación por reuso de refresh token.

internal sealed class EfUserRepository(AppDbContext db) : IUserRepository
{
    public Task<User?> FindByEmailAsync(Email email, CancellationToken ct) =>
        db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);

    public Task<User?> FindByIdAsync(Guid id, CancellationToken ct) =>
        db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);

    public async Task AddAsync(User user, CancellationToken ct)
    {
        db.Users.Add(user);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            throw DomainException.EmailTaken(); // dos registros simultáneos con el mismo correo
        }
    }

    public Task UpdateAsync(User user, CancellationToken ct) => db.SaveChangesAsync(ct);
}

internal sealed class EfRefreshTokenRepository(AppDbContext db) : IRefreshTokenRepository
{
    public async Task AddAsync(RefreshToken token, CancellationToken ct)
    {
        db.RefreshTokens.Add(token);
        await db.SaveChangesAsync(ct);
    }

    public Task<RefreshToken?> FindByHashAsync(string hash, CancellationToken ct) =>
        db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash, ct);

    public Task UpdateAsync(RefreshToken token, CancellationToken ct) => db.SaveChangesAsync(ct);

    // ExecuteUpdate: un solo UPDATE en SQL, sin cargar filas en memoria.
    public Task RevokeAllForUserAsync(Guid userId, DateTimeOffset at, CancellationToken ct) =>
        db.RefreshTokens.Where(t => t.UserId == userId && t.RevokedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, at), ct);
}

internal sealed class EfOneTimeTokenRepository(AppDbContext db) : IOneTimeTokenRepository
{
    public async Task AddAsync(OneTimeToken token, CancellationToken ct)
    {
        db.OneTimeTokens.Add(token);
        await db.SaveChangesAsync(ct);
    }

    public Task<OneTimeToken?> FindValidAsync(string hash, TokenPurpose purpose, DateTimeOffset now, CancellationToken ct) =>
        db.OneTimeTokens.FirstOrDefaultAsync(
            t => t.TokenHash == hash && t.Purpose == purpose && t.UsedAt == null && t.ExpiresAt > now, ct);

    public Task UpdateAsync(OneTimeToken token, CancellationToken ct) => db.SaveChangesAsync(ct);
}
