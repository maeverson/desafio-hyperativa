using DesafioHyperativa.Application.Interfaces;
using DesafioHyperativa.Application.Services;
using DesafioHyperativa.Domain.Interfaces;
using DesafioHyperativa.Infrastructure.Data;
using DesafioHyperativa.Infrastructure.Repositories;
using DesafioHyperativa.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DesafioHyperativa.Infrastructure.Extensions;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Database
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                npgsql => npgsql.CommandTimeout(30)
            ));

        // Repositories
        services.AddScoped<ICardRepository, CardRepository>();
        services.AddScoped<IUserRepository, UserRepository>();

        // Services
        services.AddScoped<IEncryptionService, AesEncryptionService>();
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ICardService, CardService>();

        return services;
    }
}
