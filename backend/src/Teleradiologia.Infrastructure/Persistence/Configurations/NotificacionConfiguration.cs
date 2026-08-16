using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Teleradiologia.Domain.Entities;

namespace Teleradiologia.Infrastructure.Persistence.Configurations;

public class NotificacionConfiguration : IEntityTypeConfiguration<Notificacion>
{
    public void Configure(EntityTypeBuilder<Notificacion> builder)
    {
        builder.ToTable("Notificaciones");

        builder.Ignore(n => n.Leida);
        builder.Property(n => n.Tipo).HasConversion<string>().HasMaxLength(40);

        builder.HasIndex(n => new { n.UsuarioId, n.CreatedAt });
        builder.HasIndex(n => n.EstudioId);

        builder.HasOne<Usuario>()
            .WithMany()
            .HasForeignKey(n => n.UsuarioId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(n => n.Estudio)
            .WithMany()
            .HasForeignKey(n => n.EstudioId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
