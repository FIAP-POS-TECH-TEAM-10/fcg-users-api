using Fiap.FCGames.Users.Domain.Aggregates;
using Fiap.FCGames.Users.Domain.Exceptions;
using Fiap.FCGames.Users.Domain.Interfaces;
using MediatR;

namespace Fiap.FCGames.Users.Application.Queries.BuscarUsuarioPorId;

public class BuscarUsuarioPorIdQueryHandler : IRequestHandler<BuscarUsuarioPorIdQuery, DetalhesUsuarioDto>
{
    private readonly IUsuarioRepository _usuarioRepository;

    public BuscarUsuarioPorIdQueryHandler(IUsuarioRepository usuarioRepository)
        => _usuarioRepository = usuarioRepository;

    public async Task<DetalhesUsuarioDto> Handle(BuscarUsuarioPorIdQuery request, CancellationToken cancellationToken)
    {
        var usuario = await _usuarioRepository.ObterPorIdAsync(new UsuarioId(request.Id));
        if (usuario is null)
            throw new NotFoundException("Usuário não encontrado.");

        return new DetalhesUsuarioDto
        {
            Id = usuario.Id.Value,
            Nome = usuario.Nome,
            Email = usuario.Email,
            TipoAcesso = usuario.TipoAcesso,
            CriadoEm = usuario.CriadoEm
        };
    }
}
