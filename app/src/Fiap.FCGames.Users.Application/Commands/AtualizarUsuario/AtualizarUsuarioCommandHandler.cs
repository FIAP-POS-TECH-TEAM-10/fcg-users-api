using Fiap.FCGames.Users.Domain.Aggregates;
using Fiap.FCGames.Users.Domain.Exceptions;
using Fiap.FCGames.Users.Domain.Interfaces;
using Fiap.FCGames.Users.Domain.Services;
using MediatR;

namespace Fiap.FCGames.Users.Application.Commands.AtualizarUsuario;

public class AtualizarUsuarioCommandHandler : IRequestHandler<AtualizarUsuarioCommand, AtualizarUsuarioResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasherService _passwordHasher;

    public AtualizarUsuarioCommandHandler(IUnitOfWork unitOfWork, IPasswordHasherService passwordHasher)
    {
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
    }

    public async Task<AtualizarUsuarioResponse> Handle(AtualizarUsuarioCommand request, CancellationToken cancellationToken)
    {
        var usuario = await _unitOfWork.UsuarioRepository.ObterPorIdAsync(new UsuarioId(request.Id));
        if (usuario is null)
            throw new NotFoundException("Usuário não encontrado.");

        usuario.Nome = request.Nome;
        usuario.Email = request.Email.ToLower();
        usuario.SenhaHash = _passwordHasher.GerarHash(request.Senha);

        _unitOfWork.UsuarioRepository.Atualizar(usuario);
        await _unitOfWork.CommitAsync(cancellationToken);

        return new AtualizarUsuarioResponse
        {
            Id = usuario.Id.Value,
            Nome = usuario.Nome,
            Email = usuario.Email
        };
    }
}
