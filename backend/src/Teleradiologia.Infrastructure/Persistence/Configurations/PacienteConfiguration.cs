using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Teleradiologia.Domain.Entities;
using Teleradiologia.Domain.Enums;

namespace Teleradiologia.Infrastructure.Persistence.Configurations;

public class PacienteConfiguration : IEntityTypeConfiguration<Paciente>
{
    public void Configure(EntityTypeBuilder<Paciente> builder)
    {
        builder.Property(p => p.Sexo).HasConversion<string>().HasMaxLength(20);

        builder.HasIndex(p => new { p.HospitalId, p.DocumentoIdentidad }).IsUnique();
        builder.HasIndex(p => p.HospitalId);

        builder.HasOne(p => p.Hospital)
            .WithMany()
            .HasForeignKey(p => p.HospitalId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
