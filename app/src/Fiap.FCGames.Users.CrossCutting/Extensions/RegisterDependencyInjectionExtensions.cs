using Fiap.FCGames.Users.Domain.Interfaces;
using Fiap.FCGames.Users.Domain.Services;
using Fiap.FCGames.Users.Infra.DataProvider.Repositories;
using Fiap.FCGames.Users.Infra.DataProvider.Services;
using Fiap.FCGames.Users.Infra.DataProvider.UnitOfWork;
using Microsoft.Extensions.DependencyInjection;

namespace Fiap.FCGames.Users.CrossCutting.Extensions;

public static class RegisterDependencyInjectionExtensions
{
    public static void RegisterDI(this IServiceCollection services)
    {
        services.AddScoped<IUsuarioRepository, UsuarioRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IPasswordHasherService, PasswordHasherService>();
        services.AddScoped<ITokenService, TokenService>();
    }
}
