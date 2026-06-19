using Fiap.FCGames.Users.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fiap.FCGames.Users.Infra.DataProvider.EntityConfigurations;

public class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> builder)
    {
        builder.ToTable("usuarios");

        builder.HasKey(e => e.Id).HasName("PK_usuarios");

        builder.Property(e => e.Id)
            .HasColumnName("id")
            .HasConversion(id => id.Value, value => new UsuarioId(value))
            .ValueGeneratedNever();

        builder.Property(e => e.Nome)
            .HasColumnName("nome")
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(e => e.Email)
            .HasColumnName("email")
            .IsRequired()
            .HasMaxLength(200);

        builder.HasIndex(e => e.Email).IsUnique();

        builder.Property(e => e.SenhaHash)
            .HasColumnName("senha_hash")
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(e => e.CriadoEm)
            .HasColumnName("criado_em")
            .IsRequired();

        builder.Property(e => e.IdTipoAcesso)
            .HasColumnName("tipo_acesso")
            .IsRequired();

        builder.Ignore(e => e.TipoAcesso);
    }
}
