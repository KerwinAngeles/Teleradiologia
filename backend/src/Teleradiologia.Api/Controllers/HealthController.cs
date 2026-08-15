using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Teleradiologia.Application.Abstractions;

namespace Teleradiologia.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public class HealthController(IDatabaseHealthCheck databaseHealthCheck) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var canConnectToDb = await databaseHealthCheck.CanConnectAsync(ct);

        return Ok(new
        {
            status = "ok",
            database = canConnectToDb ? "connected" : "unreachable",
            timestamp = DateTimeOffset.UtcNow,
        });
    }
}
