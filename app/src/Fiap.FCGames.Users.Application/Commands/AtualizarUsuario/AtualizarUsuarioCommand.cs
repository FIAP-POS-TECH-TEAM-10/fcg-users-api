using MediatR;

namespace Fiap.FCGames.Users.Application.Commands.AtualizarUsuario;

public record AtualizarUsuarioCommand(
    Guid Id,
    string Nome,
    string Email,
    string Senha) : IRequest<AtualizarUsuarioResponse>;
