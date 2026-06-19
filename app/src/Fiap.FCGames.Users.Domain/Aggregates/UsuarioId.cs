namespace Fiap.FCGames.Users.Domain.Aggregates;

public record struct UsuarioId(Guid Value)
{
    public static UsuarioId New() => new(Guid.NewGuid());
    public override readonly string ToString() => Value.ToString();
}
