using BaseApi.Application.Auth;
using BaseApi.Domain.Ports;
using BaseApi.Infrastructure.Mail;
using BaseApi.Infrastructure.Persistence;
using BaseApi.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BaseApi.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Une cada puerto con su adaptador y lee la configuración con los MISMOS nombres de
    /// variables que el resto de la serie (DATABASE_URL, JWT_SECRET, SMTP_HOST...).
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<AppDbContext>(o => o.UseNpgsql(ToNpgsql(
            config["DATABASE_URL"] ?? "postgres://baseapi:baseapi@localhost:5432/baseapi")));

        services.AddOptions<JwtOptions>()
            .Configure(o =>
            {
                o.Secret = config["JWT_SECRET"] ?? "";
                o.Issuer = config["JWT_ISSUER"] ?? o.Issuer;
                o.Audience = config["JWT_AUDIENCE"] ?? o.Audience;
                o.AccessTtlMinutes = int.Parse(config["JWT_ACCESS_TTL_MINUTES"] ?? "15");
            })
            .Validate(o => o.Secret.Length >= 32, "JWT_SECRET debe tener al menos 32 caracteres")
            .ValidateOnStart(); // falla al arrancar, no en la primera petición

        services.AddOptions<SmtpOptions>().Configure(o =>
        {
            o.Host = config["SMTP_HOST"] ?? o.Host;
            o.Port = int.Parse(config["SMTP_PORT"] ?? "1025");
            o.From = config["SMTP_FROM"] ?? o.From;
        });

        services.AddOptions<AuthSettings>().Configure(o =>
        {
            o.AppUrl = config["APP_URL"] ?? o.AppUrl;
            o.RefreshTtlDays = int.Parse(config["REFRESH_TTL_DAYS"] ?? "7");
        });

        services.AddSingleton(TimeProvider.System);
        services.AddScoped<IUserRepository, EfUserRepository>();
        services.AddScoped<IRefreshTokenRepository, EfRefreshTokenRepository>();
        services.AddScoped<IOneTimeTokenRepository, EfOneTimeTokenRepository>();
        services.AddSingleton<IPasswordHasher, AspNetPasswordHasher>();
        services.AddSingleton<IAccessTokenIssuer, JwtIssuer>();
        services.AddSingleton<IOpaqueTokens, RandomTokens>();
        services.AddSingleton<IEmailRenderer, EmbeddedEmailRenderer>();
        services.AddScoped<IEmailSender, MailKitEmailSender>();
        return services;
    }

    /// <summary>postgres://usuario:clave@host:puerto/base -> cadena de conexión de Npgsql.</summary>
    public static string ToNpgsql(string url)
    {
        var uri = new Uri(url);
        var userInfo = uri.UserInfo.Split(':', 2);
        return $"Host={uri.Host};Port={(uri.Port > 0 ? uri.Port : 5432)};Database={uri.AbsolutePath.TrimStart('/')};" +
               $"Username={Uri.UnescapeDataString(userInfo[0])};Password={(userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : "")}";
    }
}
