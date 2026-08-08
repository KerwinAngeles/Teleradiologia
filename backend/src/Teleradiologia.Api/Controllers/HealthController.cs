using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Teleradiologia.Api.Data;

namespace Teleradiologia.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController(AppDbContext db) : ControllerBase
{
    /// <summary>
    /// Verifica que la API está viva y que puede conectarse a la base de datos.
    /// Útil para docker-compose healthchecks y para confirmar el scaffold end-to-end.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var canConnectToDb = await db.Database.CanConnectAsync(ct);

        return Ok(new
        {
            status = "ok",
            database = canConnectToDb ? "connected" : "unreachable",
            timestamp = DateTimeOffset.UtcNow,
        });
    }
}
