using Fiap.FCGames.Users.Domain.Aggregates;

namespace Fiap.FCGames.Users.Domain.Interfaces;

public interface IUsuarioRepository
{
    void Adicionar(Usuario usuario);
    void Atualizar(Usuario usuario);
    Task<Usuario?> ObterPorEmailAsync(string email);
    Task<IEnumerable<Usuario>> ObterTodosAsync();
    Task<Usuario?> ObterPorIdAsync(UsuarioId id);
    Task<bool> ExisteEmailAsync(string email);
}
