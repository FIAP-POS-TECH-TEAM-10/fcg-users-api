using Fiap.FCGames.Users.Domain.Aggregates;

namespace Fiap.FCGames.Users.Application.Queries.BuscarUsuarioPorId;

public class DetalhesUsuarioDto
{
    public Guid Id { get; set; }
    public required string Nome { get; set; }
    public required string Email { get; set; }
    public TipoAcesso TipoAcesso { get; set; }
    public DateTime CriadoEm { get; set; }
}
