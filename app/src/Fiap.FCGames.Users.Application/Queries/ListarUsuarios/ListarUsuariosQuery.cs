using MediatR;

namespace Fiap.FCGames.Users.Application.Queries.ListarUsuarios;

public record ListarUsuariosQuery : IRequest<IEnumerable<ListaUsuariosDto>>;
