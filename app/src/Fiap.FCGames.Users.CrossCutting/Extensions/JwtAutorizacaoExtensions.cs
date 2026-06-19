using Microsoft.Extensions.DependencyInjection;

namespace Fiap.FCGames.Users.CrossCutting.Extensions;

public static class JwtAutorizacaoExtensions
{
    public static void AddAutorizacaoApi(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            options.AddPolicy("Admin", policy => policy.RequireRole("Admin"));
            options.AddPolicy("Standard", policy => policy.RequireRole("Standard"));
        });
    }
}
