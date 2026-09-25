using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PremsCart.Api.Data;

namespace PremsCart.Api.Controllers;

[ApiController]
[Route("api/health")]
public sealed class HealthController(PremsCartDbContext database) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var connected = await database.Database.CanConnectAsync(cancellationToken);
        return connected ? Ok(new { status = "ready", database = "connected" })
            : StatusCode(503, new { status = "unavailable", database = "disconnected" });
    }
}
