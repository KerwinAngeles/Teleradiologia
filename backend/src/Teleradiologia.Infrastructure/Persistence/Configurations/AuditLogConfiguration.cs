using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Teleradiologia.Domain.Entities;


namespace Teleradiologia.Infrastructure.Persistence.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.Property(a => a.Accion).HasConversion<string>().HasMaxLength(30);

        builder.HasIndex(a => new { a.EstudioId, a.Timestamp });

        builder.HasOne(a => a.Estudio)
            .WithMany(e => e.AuditLogs)
            .HasForeignKey(a => a.EstudioId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Usuario>()
            .WithMany()
            .HasForeignKey(a => a.UsuarioId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
