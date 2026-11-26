using BaseApi.Domain;
using BaseApi.Domain.Ports;
using BaseApi.Domain.Tokens;
using BaseApi.Domain.Users;
using Microsoft.Extensions.Options;

namespace BaseApi.Application.Auth;

public interface IAuthService
{
    Task<UserResponse> RegisterAsync(RegisterRequest req, CancellationToken ct);
    Task VerifyEmailAsync(TokenRequest req, CancellationToken ct);
    Task<TokenPairResponse> LoginAsync(LoginRequest req, CancellationToken ct);
    Task<TokenPairResponse> RefreshAsync(RefreshRequest req, CancellationToken ct);
    Task LogoutAsync(RefreshRequest req, CancellationToken ct);
    Task ChangePasswordAsync(Guid userId, ChangePasswordRequest req, CancellationToken ct);
    Task ForgotPasswordAsync(EmailRequest req, CancellationToken ct);
    Task ResetPasswordAsync(ResetPasswordRequest req, CancellationToken ct);
    Task<UserResponse> MeAsync(Guid userId, CancellationToken ct);
}

/// <summary>
/// Casos de uso de autenticación. Recibe TODO por constructor (primary constructor de C# 12);
/// no sabe si detrás hay Postgres, MailKit o dobles de prueba.
/// </summary>
public sealed class AuthService(
    IUserRepository users,
    IRefreshTokenRepository refreshTokens,
    IOneTimeTokenRepository oneTimeTokens,
    IPasswordHasher hasher,
    IAccessTokenIssuer accessTokens,
    IOpaqueTokens opaque,
    IEmailSender mailer,
    IEmailRenderer emails,
    TimeProvider clock,
    IOptions<AuthSettings> options) : IAuthService
{
    private readonly AuthSettings _settings = options.Value;

    public async Task<UserResponse> RegisterAsync(RegisterRequest req, CancellationToken ct)
    {
        var email = Email.Parse(req.Email);
        User.ValidatePassword(req.Password);
        if (await users.FindByEmailAsync(email, ct) is not null) throw DomainException.EmailTaken();

        var user = User.Register(email, req.Name, hasher.Hash(req.Password), clock.GetUtcNow());
        await users.AddAsync(user, ct);

        var token = await CreateOneTimeTokenAsync(user, TokenPurpose.VerifyEmail, TimeSpan.FromHours(24), ct);
        var link = $"{_settings.AppUrl}/verify-email?token={token}";
        await mailer.SendAsync(emails.Render("welcome-verify", user.Email.Value, "Bienvenido, confirma tu correo",
            new Dictionary<string, string> { ["name"] = user.Name, ["link"] = link }), ct);
        return ToResponse(user);
    }

    public async Task VerifyEmailAsync(TokenRequest req, CancellationToken ct)
    {
        var now = clock.GetUtcNow();
        var ott = await oneTimeTokens.FindValidAsync(opaque.Hash(req.Token), TokenPurpose.VerifyEmail, now, ct)
                  ?? throw DomainException.InvalidToken();
        var user = await users.FindByIdAsync(ott.UserId, ct) ?? throw DomainException.InvalidToken();
        user.VerifyEmail(now);
        ott.MarkUsed(now);
        await users.UpdateAsync(user, ct);
        await oneTimeTokens.UpdateAsync(ott, ct);
    }

    public async Task<TokenPairResponse> LoginAsync(LoginRequest req, CancellationToken ct)
    {
        Email email;
        try { email = Email.Parse(req.Email); }
        catch (DomainException) { throw DomainException.InvalidCredentials(); }

        // Mismo error si el correo no existe o la clave falla: no revelamos qué cuentas hay.
        var user = await users.FindByEmailAsync(email, ct);
        if (user is null || !hasher.Verify(req.Password, user.PasswordHash)) throw DomainException.InvalidCredentials();
        if (!user.IsVerified) throw DomainException.EmailNotVerified();
        return await IssuePairAsync(user, ct);
    }

    public async Task<TokenPairResponse> RefreshAsync(RefreshRequest req, CancellationToken ct)
    {
        var now = clock.GetUtcNow();
        var stored = await refreshTokens.FindByHashAsync(opaque.Hash(req.RefreshToken), ct)
                     ?? throw DomainException.InvalidToken();
        if (stored.IsRevoked)
        {
            // Reuso de un token ya rotado: alguien lo robó. Cerramos todas las sesiones.
            await refreshTokens.RevokeAllForUserAsync(stored.UserId, now, ct);
            throw DomainException.InvalidToken();
        }
        if (stored.IsExpired(now)) throw DomainException.InvalidToken();

        stored.Revoke(now);
        await refreshTokens.UpdateAsync(stored, ct);
        var user = await users.FindByIdAsync(stored.UserId, ct) ?? throw DomainException.InvalidToken();
        return await IssuePairAsync(user, ct);
    }

    public async Task LogoutAsync(RefreshRequest req, CancellationToken ct)
    {
        var stored = await refreshTokens.FindByHashAsync(opaque.Hash(req.RefreshToken), ct);
        if (stored is null || stored.IsRevoked) return; // idempotente
        stored.Revoke(clock.GetUtcNow());
        await refreshTokens.UpdateAsync(stored, ct);
    }

    public async Task ChangePasswordAsync(Guid userId, ChangePasswordRequest req, CancellationToken ct)
    {
        var user = await users.FindByIdAsync(userId, ct) ?? throw DomainException.Unauthorized();
        if (!hasher.Verify(req.CurrentPassword, user.PasswordHash))
            throw DomainException.Validation("currentPassword: no es correcta"); // 400, la sesión sí es válida
        User.ValidatePassword(req.NewPassword);

        var now = clock.GetUtcNow();
        user.ChangePassword(hasher.Hash(req.NewPassword), now);
        await users.UpdateAsync(user, ct);
        await refreshTokens.RevokeAllForUserAsync(user.Id, now, ct);
        await mailer.SendAsync(emails.Render("notification", user.Email.Value, "Tu contraseña cambió",
            new Dictionary<string, string>
            {
                ["name"] = user.Name,
                ["message"] = "La contraseña de tu cuenta se cambió hace un momento. Si no fuiste tú, restablécela de inmediato.",
            }), ct);
    }

    public async Task ForgotPasswordAsync(EmailRequest req, CancellationToken ct)
    {
        var user = await users.FindByEmailAsync(Email.Parse(req.Email), ct);
        if (user is null) return; // siempre 202: no confirmamos si el correo existe

        var token = await CreateOneTimeTokenAsync(user, TokenPurpose.ResetPassword, TimeSpan.FromMinutes(30), ct);
        var link = $"{_settings.AppUrl}/reset-password?token={token}";
        await mailer.SendAsync(emails.Render("reset-password", user.Email.Value, "Restablece tu contraseña",
            new Dictionary<string, string> { ["name"] = user.Name, ["link"] = link }), ct);
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest req, CancellationToken ct)
    {
        User.ValidatePassword(req.NewPassword);
        var now = clock.GetUtcNow();
        var ott = await oneTimeTokens.FindValidAsync(opaque.Hash(req.Token), TokenPurpose.ResetPassword, now, ct)
                  ?? throw DomainException.InvalidToken();
        var user = await users.FindByIdAsync(ott.UserId, ct) ?? throw DomainException.InvalidToken();

        user.ChangePassword(hasher.Hash(req.NewPassword), now);
        user.VerifyEmail(now); // si leyó el correo, el correo es suyo
        ott.MarkUsed(now);
        await users.UpdateAsync(user, ct);
        await oneTimeTokens.UpdateAsync(ott, ct);
        await refreshTokens.RevokeAllForUserAsync(user.Id, now, ct);
        await mailer.SendAsync(emails.Render("notification", user.Email.Value, "Tu contraseña se restableció",
            new Dictionary<string, string>
            {
                ["name"] = user.Name,
                ["message"] = "Restableciste tu contraseña. Cerramos tus sesiones abiertas por seguridad.",
            }), ct);
    }

    public async Task<UserResponse> MeAsync(Guid userId, CancellationToken ct) =>
        ToResponse(await users.FindByIdAsync(userId, ct) ?? throw DomainException.Unauthorized());

    // ---- ayudantes privados ----

    private async Task<TokenPairResponse> IssuePairAsync(User user, CancellationToken ct)
    {
        var access = accessTokens.Issue(user);
        var refresh = opaque.Generate();
        await refreshTokens.AddAsync(
            RefreshToken.Create(user.Id, refresh.Hash, clock.GetUtcNow(), TimeSpan.FromDays(_settings.RefreshTtlDays)), ct);
        return new TokenPairResponse(access.Token, refresh.Value, "Bearer", access.ExpiresIn);
    }

    private async Task<string> CreateOneTimeTokenAsync(User user, TokenPurpose purpose, TimeSpan ttl, CancellationToken ct)
    {
        var t = opaque.Generate();
        await oneTimeTokens.AddAsync(OneTimeToken.Create(user.Id, purpose, t.Hash, clock.GetUtcNow(), ttl), ct);
        return t.Value;
    }

    private static UserResponse ToResponse(User u) => new(u.Id, u.Email.Value, u.Name, u.IsVerified, u.CreatedAt);
}
