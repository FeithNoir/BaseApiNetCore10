using BaseApi.Application.Auth;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace BaseApi.Application;

public static class DependencyInjection
{
    /// <summary>Registra los casos de uso y los validadores de esta capa.</summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddValidatorsFromAssemblyContaining<RegisterRequestValidator>();
        return services;
    }
}
