namespace Fiap.FCGames.Users.Application.Commands.AtualizarUsuario;

public class AtualizarUsuarioResponse
{
    public Guid Id { get; set; }
    public required string Nome { get; set; }
    public required string Email { get; set; }
}
