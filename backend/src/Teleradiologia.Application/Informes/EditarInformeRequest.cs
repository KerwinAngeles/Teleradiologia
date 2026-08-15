using System.ComponentModel.DataAnnotations;

namespace Teleradiologia.Application.Informes;

public record EditarInformeRequest([Required] string Contenido);
