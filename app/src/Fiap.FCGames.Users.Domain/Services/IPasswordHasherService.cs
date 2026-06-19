namespace Fiap.FCGames.Users.Domain.Services;

public interface IPasswordHasherService
{
    string GerarHash(string senha);
    bool Verificar(string senhaTexto, string senhaHash);
}
