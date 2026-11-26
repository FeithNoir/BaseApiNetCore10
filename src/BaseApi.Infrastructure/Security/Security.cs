using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using BaseApi.Domain.Ports;
using BaseApi.Domain.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace BaseApi.Infrastructure.Security;

public sealed class JwtOptions
{
    public string Secret { get; set; } = "";
    public string Issuer { get; set; } = "baseapi";
    public string Audience { get; set; } = "baseapi-clients";
    public int AccessTtlMinutes { get; set; } = 15;
}

/// <summary>
/// Usa el PasswordHasher de ASP.NET Core Identity (PBKDF2-HMAC-SHA512, 100 000 iteraciones)
/// sin traer todo Identity: solo la pieza que hashea.
/// </summary>
internal sealed class AspNetPasswordHasher : IPasswordHasher
{
    private readonly PasswordHasher<object> _inner = new();
    private static readonly object Subject = new();

    public string Hash(string plain) => _inner.HashPassword(Subject, plain);

    public bool Verify(string plain, string hash) =>
        _inner.VerifyHashedPassword(Subject, hash, plain) is not PasswordVerificationResult.Failed;
}

internal sealed class JwtIssuer(IOptions<JwtOptions> options, TimeProvider clock) : IAccessTokenIssuer
{
    private readonly JwtOptions _o = options.Value;
    private readonly JsonWebTokenHandler _handler = new();

    public IssuedAccessToken Issue(User user)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_o.Secret));
        var token = _handler.CreateToken(new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity([
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email.Value),
            ]),
            Issuer = _o.Issuer,
            Audience = _o.Audience,
            IssuedAt = now,
            NotBefore = now,
            Expires = now.AddMinutes(_o.AccessTtlMinutes),
            SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256),
        });
        return new IssuedAccessToken(token, _o.AccessTtlMinutes * 60);
    }
}

internal sealed class RandomTokens : IOpaqueTokens
{
    public GeneratedToken Generate()
    {
        var value = Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(32));
        return new GeneratedToken(value, Hash(value));
    }

    public string Hash(string value) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}
