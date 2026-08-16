using System.ComponentModel.DataAnnotations;
using Teleradiologia.Domain.Common;
using Teleradiologia.Domain.Enums;

namespace Teleradiologia.Domain.Entities;

public class Paciente : AuditableBaseEntity
{
    public Guid Id { get; set; }

    public Guid HospitalId { get; set; }
    public Hospital Hospital { get; set; } = null!;

    [Required, MaxLength(200)]
    public string NombreCompleto { get; set; } = string.Empty;

    [Required, MaxLength(50)]
    public string DocumentoIdentidad { get; set; } = string.Empty;

    public DateOnly FechaNacimiento { get; set; }

    public SexoPaciente Sexo { get; set; }

    public ICollection<Estudio> Estudios { get; set; } = [];
}
