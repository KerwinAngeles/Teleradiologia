using System.ComponentModel.DataAnnotations;
using Teleradiologia.Domain.Enums;

namespace Teleradiologia.Api.Controllers;

public class SubirEstudioForm
{
    [Required]
    public IFormFileCollection Archivos { get; set; } = null!;

    [Required]
    public Guid HospitalId { get; set; }

    public PrioridadEstudio Prioridad { get; set; } = PrioridadEstudio.Rutina;
}
