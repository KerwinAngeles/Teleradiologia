using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Teleradiologia.Application.Dtos.Account;
using Teleradiologia.Application.Interfaces.Services;
using Teleradiologia.Domain.Enums;

namespace Teleradiologia.Api.Controllers;

[Route("api/account")]
public class AccountController(IAccountService accountService) : BaseApiController
{
    [HttpPost("registro")]
    [AllowAnonymous]
    public async Task<IActionResult> Registrar(RegistroRequest request, CancellationToken ct) =>
        Resultado(await accountService.RegistrarAsync(request, ct));

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login(AutenticacionRequest request, CancellationToken ct) =>
        Resultado(await accountService.LoginAsync(request, DireccionIp, ct));

    [HttpGet("me")]
    public async Task<IActionResult> Me(CancellationToken ct) =>
        Resultado(await accountService.ObtenerPerfilAsync(UsuarioId, ct));
}

[Route("api/usuarios")]
[Authorize(Roles = nameof(RolUsuario.Admin))]
public class UsuariosController(IAccountService accountService) : BaseApiController
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] FiltroUsuarios filtro, CancellationToken ct) =>
        Resultado(await accountService.BuscarAsync(filtro, ct));

    [HttpPost("{id:guid}/aprobar")]
    public async Task<IActionResult> Aprobar(Guid id, AprobarUsuarioRequest request, CancellationToken ct) =>
        Resultado(await accountService.AprobarAsync(id, request, UsuarioId, ct));

    [HttpPost("{id:guid}/rechazar")]
    public async Task<IActionResult> Rechazar(Guid id, DecisionRequest request, CancellationToken ct) =>
        Resultado(await accountService.RechazarAsync(id, request, UsuarioId, ct));

    [HttpPost("{id:guid}/suspender")]
    public async Task<IActionResult> Suspender(Guid id, DecisionRequest request, CancellationToken ct) =>
        Resultado(await accountService.SuspenderAsync(id, request, UsuarioId, ct));

    [HttpPost("{id:guid}/reactivar")]
    public async Task<IActionResult> Reactivar(Guid id, CancellationToken ct) =>
        Resultado(await accountService.ReactivarAsync(id, UsuarioId, ct));
}
