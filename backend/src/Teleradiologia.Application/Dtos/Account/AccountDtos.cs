using System.ComponentModel.DataAnnotations;
using Teleradiologia.Domain.Enums;

namespace Teleradiologia.Application.Dtos.Account;

public record RegistroRequest(
    [Required, MaxLength(200)] string NombreCompleto,
    [Required, MaxLength(256), EmailAddress] string Email,
    [Required, MinLength(8)] string Password);

public record RegistroResponse(Guid UsuarioId, string Email, EstadoAcceso EstadoAcceso, string Mensaje);

public record AutenticacionRequest(
    [Required, EmailAddress] string Email,
    [Required] string Password);

public record AutenticacionResponse(
    string Token,
    string? RefreshToken,
    DateTimeOffset ExpiresAt,
    UsuarioDto Usuario);

public record UsuarioDto(
    Guid Id,
    string NombreCompleto,
    string Email,
    RolUsuario Rol,
    EstadoAcceso EstadoAcceso,
    DateTimeOffset CreatedAt,
    DateTimeOffset? FechaDecision,
    string? MotivoDecision,
    string? Matricula)
{
    public bool Activo => EstadoAcceso == EstadoAcceso.Aprobado;
}

public record AprobarUsuarioRequest([Required] RolUsuario Rol);

public record DecisionRequest([MaxLength(500)] string? Motivo);
