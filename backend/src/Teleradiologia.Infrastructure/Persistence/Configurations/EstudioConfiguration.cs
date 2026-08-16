using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Teleradiologia.Domain.Entities;


namespace Teleradiologia.Infrastructure.Persistence.Configurations;

public class EstudioConfiguration : IEntityTypeConfiguration<Estudio>
{
    public void Configure(EntityTypeBuilder<Estudio> builder)
    {
        builder.Property(e => e.Estado).HasConversion<string>().HasMaxLength(20);
        builder.Property(e => e.Prioridad).HasConversion<string>().HasMaxLength(20);

        builder.HasIndex(e => new { e.HospitalId, e.StudyInstanceUid }).IsUnique();
        builder.HasIndex(e => e.StudyInstanceUid);
        builder.HasIndex(e => new { e.HospitalId, e.OrthancStudyId }).IsUnique();
        builder.HasIndex(e => e.Estado);
        builder.HasIndex(e => e.HospitalId);

        builder.HasOne(e => e.Hospital)
            .WithMany()
            .HasForeignKey(e => e.HospitalId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Paciente)
            .WithMany(p => p.Estudios)
            .HasForeignKey(e => e.PacienteId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Usuario>()
            .WithMany()
            .HasForeignKey(e => e.RadiologoAsignadoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Usuario>()
            .WithMany()
            .HasForeignKey(e => e.SubidoPorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
