using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Teleradiologia.Domain.Entities;

namespace Teleradiologia.Infrastructure.Persistence.Configurations;

public class HospitalConfiguration : IEntityTypeConfiguration<Hospital>
{
    public void Configure(EntityTypeBuilder<Hospital> builder)
    {
        builder.ToTable("Hospitales");

        builder.HasIndex(h => h.Nombre).IsUnique();

        builder.HasIndex(h => h.CodigoExterno)
            .IsUnique()
            .HasFilter("\"CodigoExterno\" IS NOT NULL");

        builder.HasOne(h => h.Catalogo)
            .WithMany()
            .HasForeignKey(h => h.CodigoExterno)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class EstablecimientoCatalogoConfiguration : IEntityTypeConfiguration<EstablecimientoCatalogo>
{
    public void Configure(EntityTypeBuilder<EstablecimientoCatalogo> builder)
    {
        builder.ToTable("CatalogoEstablecimientos");

        builder.HasKey(e => e.Codigo);
        builder.Property(e => e.Codigo).ValueGeneratedNever();

        builder.HasIndex(e => e.Provincia);
        builder.HasIndex(e => e.Tipo);
    }
}

public class UsuarioHospitalConfiguration : IEntityTypeConfiguration<UsuarioHospital>
{
    public void Configure(EntityTypeBuilder<UsuarioHospital> builder)
    {
        builder.ToTable("UsuarioHospitales");

        builder.HasKey(uh => new { uh.UsuarioId, uh.HospitalId });

        builder.HasIndex(uh => uh.HospitalId);

        builder.HasOne(uh => uh.Usuario)
            .WithMany(u => u.Hospitales)
            .HasForeignKey(uh => uh.UsuarioId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(uh => uh.Hospital)
            .WithMany(h => h.Usuarios)
            .HasForeignKey(uh => uh.HospitalId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
