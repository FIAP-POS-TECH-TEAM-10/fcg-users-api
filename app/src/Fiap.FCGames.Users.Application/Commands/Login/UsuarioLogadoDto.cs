namespace Fiap.FCGames.Users.Application.Commands.Login;

public class UsuarioLogadoDto
{
    public required string Email { get; set; }
    public required string Token { get; set; }
    public DateTime Expiracao { get; set; }
}
