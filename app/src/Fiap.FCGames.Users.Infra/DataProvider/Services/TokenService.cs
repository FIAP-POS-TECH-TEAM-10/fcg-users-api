using Fiap.FCGames.Users.Domain.Aggregates;
using Fiap.FCGames.Users.Domain.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Fiap.FCGames.Users.Infra.DataProvider.Services;

public class TokenService : ITokenService
{
    private readonly string _jwtKey;
    private readonly string _jwtIssuer;

    public TokenService(IConfiguration configuration)
    {
        _jwtKey = configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("Configuração Jwt:Key não encontrada. Defina a env var JWT__KEY.");
        _jwtIssuer = configuration["Jwt:Issuer"]
            ?? throw new InvalidOperationException("Configuração Jwt:Issuer não encontrada. Defina a env var JWT__ISSUER.");
    }

    public string GerarToken(Guid userId, string email, TipoAcesso tipoAcesso, DateTime expiracao)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Sub, email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Role, tipoAcesso.ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtIssuer,
            claims: claims,
            expires: expiracao,
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
