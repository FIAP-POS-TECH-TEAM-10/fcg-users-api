using Fiap.FCGames.Users.Domain.Aggregates;

namespace Fiap.FCGames.Users.Domain.Services;

public interface ITokenService
{
    string GerarToken(Guid userId, string email, TipoAcesso tipoAcesso, DateTime expiracao);
}
