using Microsoft.EntityFrameworkCore;
using Teleradiologia.Application.Abstractions;
using Teleradiologia.Domain.Common;
using Teleradiologia.Domain.Entities;

namespace Teleradiologia.Infrastructure.Persistence;

public class AppDbContext(
    DbContextOptions<AppDbContext> options,
    ICurrentUserService currentUser,
    ITenantContext tenant) : DbContext(options)
{
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Hospital> Hospitales => Set<Hospital>();
    public DbSet<EstablecimientoCatalogo> CatalogoEstablecimientos => Set<EstablecimientoCatalogo>();
    public DbSet<UsuarioHospital> UsuarioHospitales => Set<UsuarioHospital>();
    public DbSet<Paciente> Pacientes => Set<Paciente>();
    public DbSet<Estudio> Estudios => Set<Estudio>();
    public DbSet<Informe> Informes => Set<Informe>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Evento> Eventos => Set<Evento>();
    public DbSet<Notificacion> Notificaciones => Set<Notificacion>();
    public DbSet<PlantillaInforme> PlantillasInforme => Set<PlantillaInforme>();

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var ahora = DateTimeOffset.UtcNow;
        var autor = currentUser.Email;

        foreach (var entry in ChangeTracker.Entries<AuditableBaseEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = ahora;
                    entry.Entity.CreatedBy = autor;
                    break;

                case EntityState.Modified:
                    entry.Entity.LastModified = ahora;
                    entry.Entity.LastModifiedBy = autor;
                    break;
            }
        }

        // Se captura antes de guardar: después del SaveChanges los valores originales ya se
        // perdieron y las entradas eliminadas no están más en el tracker.
        var eventos = RegistroDeEventos.Capturar(ChangeTracker, currentUser.UsuarioId, autor, ahora);

        if (eventos.Count > 0)
        {
            Eventos.AddRange(eventos);
        }

        return base.SaveChangesAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Aislamiento por hospital aplicado en el modelo, no en cada consulta: así ningún caso de
        // uso puede olvidarse del filtro y filtrar datos de otro hospital.
        modelBuilder.Entity<Hospital>().HasQueryFilter(h =>
            tenant.VeTodosLosHospitales || tenant.HospitalesPermitidos.Contains(h.Id));

        modelBuilder.Entity<Paciente>().HasQueryFilter(p =>
            tenant.VeTodosLosHospitales || tenant.HospitalesPermitidos.Contains(p.HospitalId));

        modelBuilder.Entity<Estudio>().HasQueryFilter(e =>
            tenant.VeTodosLosHospitales || tenant.HospitalesPermitidos.Contains(e.HospitalId));

        // Informes y auditoría cuelgan de un estudio ya filtrado.
        modelBuilder.Entity<Informe>().HasQueryFilter(i =>
            tenant.VeTodosLosHospitales || tenant.HospitalesPermitidos.Contains(i.Estudio.HospitalId));
    }
}
