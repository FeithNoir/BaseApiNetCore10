using BaseApi.Application.Auth;
using BaseApi.Domain;
using BaseApi.Domain.Ports;
using BaseApi.Domain.Tokens;
using BaseApi.Domain.Users;
using Microsoft.Extensions.Options;

namespace BaseApi.Tests;

/// <summary>Pruebas del caso de uso con dobles en memoria: sin base, sin SMTP, sin ASP.NET.</summary>
public sealed class AuthServiceTests
{
    private sealed class Users : IUserRepository
    {
        public readonly List<User> Rows = [];
        public Task<User?> FindByEmailAsync(Email e, CancellationToken ct) => Task.FromResult(Rows.FirstOrDefault(u => u.Email == e));
        public Task<User?> FindByIdAsync(Guid id, CancellationToken ct) => Task.FromResult(Rows.FirstOrDefault(u => u.Id == id));
        public Task AddAsync(User u, CancellationToken ct) { Rows.Add(u); return Task.CompletedTask; }
        public Task UpdateAsync(User u, CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class Refresh : IRefreshTokenRepository
    {
        public readonly List<RefreshToken> Rows = [];
        public Task AddAsync(RefreshToken t, CancellationToken ct) { Rows.Add(t); return Task.CompletedTask; }
        public Task<RefreshToken?> FindByHashAsync(string h, CancellationToken ct) => Task.FromResult(Rows.FirstOrDefault(t => t.TokenHash == h));
        public Task UpdateAsync(RefreshToken t, CancellationToken ct) => Task.CompletedTask;
        public Task RevokeAllForUserAsync(Guid userId, DateTimeOffset at, CancellationToken ct)
        {
            foreach (var t in Rows.Where(t => t.UserId == userId)) t.Revoke(at);
            return Task.CompletedTask;
        }
    }

    private sealed class Otts : IOneTimeTokenRepository
    {
        private readonly List<OneTimeToken> _rows = [];
        public Task AddAsync(OneTimeToken t, CancellationToken ct) { _rows.Add(t); return Task.CompletedTask; }
        public Task<OneTimeToken?> FindValidAsync(string h, TokenPurpose p, DateTimeOffset now, CancellationToken ct) =>
            Task.FromResult(_rows.FirstOrDefault(t => t.TokenHash == h && t.Purpose == p && t.UsedAt == null && t.ExpiresAt > now));
        public Task UpdateAsync(OneTimeToken t, CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class FakeHasher : IPasswordHasher
    {
        public string Hash(string p) => "h:" + p;
        public bool Verify(string p, string h) => h == "h:" + p;
    }

    private sealed class FakeJwt : IAccessTokenIssuer
    {
        public IssuedAccessToken Issue(User u) => new("jwt-" + u.Id, 900);
    }

    private sealed class FakeTokens : IOpaqueTokens
    {
        private int _n;
        public GeneratedToken Generate() { var v = "tok" + ++_n; return new(v, Hash(v)); }
        public string Hash(string v) => "sha:" + v;
    }

    private sealed class Outbox : IEmailSender, IEmailRenderer
    {
        public readonly List<EmailMessage> Sent = [];
        public Task SendAsync(EmailMessage m, CancellationToken ct) { Sent.Add(m); return Task.CompletedTask; }
        public EmailMessage Render(string tpl, string to, string subject, IReadOnlyDictionary<string, string> d) =>
            new(to, subject, tpl, d.GetValueOrDefault("link") ?? d["message"]);
        public string LastToken() => Sent[^1].Text[(Sent[^1].Text.IndexOf("token=") + 6)..];
    }

    private sealed class FixedClock : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(2026, 11, 25, 12, 0, 0, TimeSpan.Zero);
    }

    private readonly Outbox _outbox = new();
    private readonly AuthService _sut;
    private static readonly CancellationToken Ct = CancellationToken.None;

    public AuthServiceTests() =>
        _sut = new AuthService(new Users(), new Refresh(), new Otts(), new FakeHasher(), new FakeJwt(), new FakeTokens(),
            _outbox, _outbox, new FixedClock(), Options.Create(new AuthSettings { AppUrl = "http://front" }));

    private async Task<TokenPairResponse> RegisterVerifyLogin()
    {
        await _sut.RegisterAsync(new RegisterRequest(" Ana@Example.COM ", "secreta123", "Ana"), Ct);
        await _sut.VerifyEmailAsync(new TokenRequest(_outbox.LastToken()), Ct);
        return await _sut.LoginAsync(new LoginRequest("ana@example.com", "secreta123"), Ct);
    }

    [Fact]
    public async Task Register_normaliza_el_correo_y_envia_bienvenida()
    {
        var res = await _sut.RegisterAsync(new RegisterRequest(" Ana@Example.COM ", "secreta123", "Ana"), Ct);
        Assert.Equal("ana@example.com", res.Email);
        Assert.False(res.EmailVerified);
        Assert.Single(_outbox.Sent);
        var dup = await Assert.ThrowsAsync<DomainException>(() =>
            _sut.RegisterAsync(new RegisterRequest("ana@example.com", "otraclave1", "Ana"), Ct));
        Assert.Equal(DomainErrorKind.Conflict, dup.Kind);
    }

    [Fact]
    public async Task Login_exige_correo_verificado()
    {
        await _sut.RegisterAsync(new RegisterRequest("ana@example.com", "secreta123", "Ana"), Ct);
        var ex = await Assert.ThrowsAsync<DomainException>(() =>
            _sut.LoginAsync(new LoginRequest("ana@example.com", "secreta123"), Ct));
        Assert.Equal(DomainErrorKind.Forbidden, ex.Kind);
    }

    [Fact]
    public async Task Reusar_un_refresh_rotado_cierra_todas_las_sesiones()
    {
        var first = await RegisterVerifyLogin();
        var second = await _sut.RefreshAsync(new RefreshRequest(first.RefreshToken), Ct);
        await Assert.ThrowsAsync<DomainException>(() => _sut.RefreshAsync(new RefreshRequest(first.RefreshToken), Ct));
        await Assert.ThrowsAsync<DomainException>(() => _sut.RefreshAsync(new RefreshRequest(second.RefreshToken), Ct));
    }

    [Fact]
    public async Task Olvide_clave_con_correo_desconocido_no_envia_nada()
    {
        await _sut.ForgotPasswordAsync(new EmailRequest("nadie@example.com"), Ct);
        Assert.Empty(_outbox.Sent);
    }
}
