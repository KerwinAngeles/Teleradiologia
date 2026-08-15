using System.ComponentModel.DataAnnotations;

namespace Teleradiologia.Application.Informes;

public record CrearInformeRequest([Required] string Contenido);
