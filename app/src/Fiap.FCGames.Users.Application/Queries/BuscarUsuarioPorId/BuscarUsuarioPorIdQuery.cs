using MediatR;

namespace Fiap.FCGames.Users.Application.Queries.BuscarUsuarioPorId;

public record BuscarUsuarioPorIdQuery(Guid Id) : IRequest<DetalhesUsuarioDto>;
