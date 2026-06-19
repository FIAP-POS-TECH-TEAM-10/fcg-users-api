using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;
using Scalar.AspNetCore;

namespace Fiap.FCGames.Users.CrossCutting.Extensions;

public static class SwaggerExtensions
{
    public static void RegisterSwaggerGenerator(this IServiceCollection services)
    {
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "API Fiap - FCGames Users", Version = "v1" });

            var securityScheme = new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Insira o token: Bearer {seu-token}"
            };

            c.AddSecurityDefinition("Bearer", securityScheme);
            c.AddSecurityRequirement(doc => new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference("Bearer", doc)] = []
            });
        });
    }

    public static void RegisterSwagger(this IApplicationBuilder app)
    {
        app.UseSwagger();
        app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "API Fiap - FCGames Users"));
    }

    public static void RegisterScalar(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapScalarApiReference(options =>
        {
            options.Title = "API Fiap - FCGames Users";
            options.WithOpenApiRoutePattern("/openapi/{documentName}.json");
        });
    }
}
