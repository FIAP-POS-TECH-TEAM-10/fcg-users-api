namespace Fiap.FCGames.Users.Domain.Aggregates;

public class Usuario
{
    public UsuarioId Id { get; set; }
    public required string Nome { get; set; }
    public required string Email { get; set; }
    public required string SenhaHash { get; set; }
    public int IdTipoAcesso { get; set; }
    public TipoAcesso TipoAcesso
    {
        get => (TipoAcesso)IdTipoAcesso;
        set => IdTipoAcesso = (int)value;
    }
    public DateTime CriadoEm { get; set; }
}
