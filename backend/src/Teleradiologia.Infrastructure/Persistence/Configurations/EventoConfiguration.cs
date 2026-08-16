using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Teleradiologia.Domain.Entities;

namespace Teleradiologia.Infrastructure.Persistence.Configurations;

public class EventoConfiguration : IEntityTypeConfiguration<Evento>
{
    public void Configure(EntityTypeBuilder<Evento> builder)
    {
        builder.ToTable("Eventos");

        builder.Property(e => e.Operacion).HasConversion<string>().HasMaxLength(20);
        builder.Property(e => e.Cambios).HasColumnType("jsonb");

        builder.HasIndex(e => e.Timestamp).IsDescending();
        builder.HasIndex(e => new { e.Entidad, e.Timestamp });
        builder.HasIndex(e => e.UsuarioId);
        builder.HasIndex(e => e.Operacion);
    }
}
