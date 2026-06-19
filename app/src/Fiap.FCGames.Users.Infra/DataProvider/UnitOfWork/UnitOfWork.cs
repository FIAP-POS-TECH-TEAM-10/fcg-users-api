using Fiap.FCGames.Users.Domain.Interfaces;
using Fiap.FCGames.Users.Infra.DataProvider.Contexto;

namespace Fiap.FCGames.Users.Infra.DataProvider.UnitOfWork;

public class UnitOfWork : IUnitOfWork
{
    private readonly FcGamesContexto _context;
    public IUsuarioRepository UsuarioRepository { get; }

    public UnitOfWork(FcGamesContexto context, IUsuarioRepository usuarioRepository)
    {
        _context = context;
        UsuarioRepository = usuarioRepository;
    }

    public Task<int> CommitAsync(CancellationToken cancellationToken = default)
        => _context.SaveChangesAsync(cancellationToken);
}
