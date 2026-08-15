using System.ComponentModel.DataAnnotations;

namespace Teleradiologia.Api.Controllers;

public class SubirEstudioForm
{
    [Required]
    public IFormFileCollection Archivos { get; set; } = null!;

    [Required, MaxLength(200)]
    public string HospitalOrigen { get; set; } = string.Empty;
}
