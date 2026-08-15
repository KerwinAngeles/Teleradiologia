using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Teleradiologia.Domain.Entities;


namespace Teleradiologia.Infrastructure.Persistence.Configurations;

public class InformeConfiguration : IEntityTypeConfiguration<Informe>
{
    public void Configure(EntityTypeBuilder<Informe> builder)
    {
        builder.Property(i => i.Estado).HasConversion<string>().HasMaxLength(20);
        builder.Property(i => i.Contenido).HasColumnType("text");

        // Ya no es único: un estudio puede tener el informe original + N adendas encadenadas.
        builder.HasIndex(i => i.EstudioId);

        builder.HasOne(i => i.Estudio)
            .WithMany(e => e.Informes)
            .HasForeignKey(i => i.EstudioId)
            .OnDelete(DeleteBehavior.Restrict);

        // Auto-referencia: la adenda apunta al informe firmado que corrige/complementa.
        builder.HasOne(i => i.InformeAnterior)
            .WithMany()
            .HasForeignKey(i => i.InformeAnteriorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Usuario>()
            .WithMany()
            .HasForeignKey(i => i.RadiologoId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
