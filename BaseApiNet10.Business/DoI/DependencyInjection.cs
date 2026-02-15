using BaseApiNet10.Business.Interfaces.Repositories;
using BaseApiNet10.Business.MappingProfiles;
using BaseApiNet10.Business.Repositories;
using BaseApiNet10.Business.Services;
using Microsoft.Extensions.DependencyInjection;

namespace BaseApiNet10.Business.DoI
{
    public static class DependencyInjection
    {
        public static void AddCustomServices(this IServiceCollection services)
        {
            // Configuración de AutoMapper
            services.AddAutoMapper(typeof(MappingConfig));

            // Configuración de repositorios
            services.AddScoped<IAuthRepository, AuthRepository>();

            // Configuración de servicios
            services.AddScoped<AuthService>();
        }
    }
}
