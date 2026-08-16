using System.ComponentModel.DataAnnotations;

namespace Teleradiologia.Application.Dtos.Hospitales;

public record HospitalDto(
    Guid Id,
    string Nombre,
    int? CodigoExterno,
    string? Provincia,
    string? Municipio,
    string? EmailContacto,
    bool Activo,
    int? SlaStatMinutos,
    int? SlaUrgenteMinutos,
    int? SlaRutinaMinutos);

public record EstablecimientoCatalogoDto(
    int Codigo,
    string Nombre,
    string? NivelAtencion,
    string? Tipo,
    string? Provincia,
    string? Municipio);

public record CrearHospitalRequest(
    [Required, MaxLength(200)] string Nombre,
    int? CodigoExterno,
    [MaxLength(80)] string? Provincia,
    [MaxLength(120)] string? Municipio,
    [MaxLength(256), EmailAddress] string? EmailContacto,
    [Range(1, 20160)] int? SlaStatMinutos = null,
    [Range(1, 20160)] int? SlaUrgenteMinutos = null,
    [Range(1, 20160)] int? SlaRutinaMinutos = null);
