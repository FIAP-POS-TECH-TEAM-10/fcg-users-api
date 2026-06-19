using MediatR;

namespace Fiap.FCGames.Users.Application.Commands.Login;

public record LoginCommand(string Email, string Senha) : IRequest<UsuarioLogadoDto>;
