using Microsoft.AspNetCore.Builder;

namespace Fiap.FCGames.Users.CrossCutting.Middleware;

public static class ErrorHandlingMiddlewareExtensions
{
    public static IApplicationBuilder UseErrorHandlingMiddleware(this IApplicationBuilder builder)
        => builder.UseMiddleware<ErrorHandlingMiddleware>();
}
