using MediatR;

namespace Fiap.FCGames.Users.Application.Commands.CriarUsuario;

public record CriarUsuarioCommand(
    string Nome,
    string Email,
    string Senha) : IRequest<CriarUsuarioResponse>;
