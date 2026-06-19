using Fiap.FCGames.Users.Domain.Aggregates;
using Fiap.FCGames.Users.Domain.Interfaces;
using Fiap.FCGames.Users.Infra.DataProvider.Contexto;
using Fiap.FCGames.Users.Infra.DataProvider.Repositories.Shared;
using Microsoft.EntityFrameworkCore;

namespace Fiap.FCGames.Users.Infra.DataProvider.Repositories;

public class UsuarioRepository : GenericRepository<Usuario>, IUsuarioRepository
{
    public UsuarioRepository(FcGamesContexto context) : base(context) { }

    public void Adicionar(Usuario usuario) => Create(usuario);
    public void Atualizar(Usuario usuario) => Update(usuario);

    public async Task<IEnumerable<Usuario>> ObterTodosAsync()
        => await _dbSet.AsNoTracking().ToListAsync();

    public async Task<Usuario?> ObterPorEmailAsync(string email)
        => await _dbSet.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Email.ToLower() == email.ToLower());

    public async Task<Usuario?> ObterPorIdAsync(UsuarioId id)
        => await _dbSet.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id);

    public Task<bool> ExisteEmailAsync(string email)
        => _dbSet.AsNoTracking().AnyAsync(x => x.Email.ToLower() == email.ToLower());
}
