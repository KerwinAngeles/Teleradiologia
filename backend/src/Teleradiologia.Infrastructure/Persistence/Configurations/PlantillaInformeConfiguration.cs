using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Teleradiologia.Domain.Entities;

namespace Teleradiologia.Infrastructure.Persistence.Configurations;

public class PlantillaInformeConfiguration : IEntityTypeConfiguration<PlantillaInforme>
{
    public void Configure(EntityTypeBuilder<PlantillaInforme> builder)
    {
        builder.ToTable("PlantillasInforme");

        builder.Property(p => p.Secciones).HasColumnType("jsonb");

        builder.HasIndex(p => p.RadiologoId);
        builder.HasIndex(p => p.Modalidad);

        builder.HasOne<Usuario>()
            .WithMany()
            .HasForeignKey(p => p.RadiologoId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
