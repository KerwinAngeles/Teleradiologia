using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Teleradiologia.Domain.Entities;

namespace Teleradiologia.Infrastructure.Persistence.Configurations;

public class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> builder)
    {
        builder.ToTable("Usuarios");

        builder.Ignore(u => u.PuedeIniciarSesion);

        builder.Property(u => u.Rol).HasConversion<string>().HasMaxLength(20);
        builder.Property(u => u.EstadoAcceso).HasConversion<string>().HasMaxLength(20);

        builder.HasIndex(u => u.Email).IsUnique();
        builder.HasIndex(u => u.EstadoAcceso);

        builder.HasIndex(u => u.ProveedorUserId)
            .IsUnique()
            .HasFilter("\"ProveedorUserId\" IS NOT NULL");

        builder.HasOne<Usuario>()
            .WithMany()
            .HasForeignKey(u => u.DecididoPorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
