using System.ComponentModel.DataAnnotations;

namespace Teleradiologia.Application.Dtos.Plantillas;

public record SeccionPlantillaDto(
    [Required, MaxLength(120)] string Titulo,
    string? Contenido,
    int Orden);

public record PlantillaDto(
    Guid Id,
    string Nombre,
    string? Modalidad,
    string? RegionAnatomica,
    string? Descripcion,
    IReadOnlyList<SeccionPlantillaDto> Secciones,
    bool Favorita,
    int VecesUsada,
    DateTimeOffset CreatedAt);

public record GuardarPlantillaRequest(
    [Required, MaxLength(200)] string Nombre,
    [MaxLength(16)] string? Modalidad,
    [MaxLength(120)] string? RegionAnatomica,
    [MaxLength(500)] string? Descripcion,
    IReadOnlyList<SeccionPlantillaDto> Secciones,
    bool Favorita);
