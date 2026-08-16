using System.ComponentModel.DataAnnotations;

namespace Teleradiologia.Application.Informes;

public record CrearInformeRequest([Required] string Contenido);

public record FirmarInformeRequest(string? FirmaImagen);
