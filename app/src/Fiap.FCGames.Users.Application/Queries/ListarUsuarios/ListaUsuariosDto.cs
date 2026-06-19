using Fiap.FCGames.Users.Domain.Aggregates;

namespace Fiap.FCGames.Users.Application.Queries.ListarUsuarios;

public class ListaUsuariosDto
{
    public Guid Id { get; set; }
    public required string Nome { get; set; }
    public required string Email { get; set; }
    public TipoAcesso TipoAcesso { get; set; }
}
