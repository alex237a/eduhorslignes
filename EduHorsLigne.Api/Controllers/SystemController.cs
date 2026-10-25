using EduHorsLigne.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduHorsLigne.Api.Controllers;

[ApiController, Route("api"), AllowAnonymous]
public class SystemController(AppDbContext db) : ControllerBase
{
    public const string ApiVersion = "1.0.0";

    [HttpGet("health")]
    public async Task<IActionResult> Health(CancellationToken ct)
    {
        var dbOk = await db.Database.CanConnectAsync(ct);
        var body = new { status = dbOk ? "ok" : "degrade", baseDeDonnees = dbOk, heureUtc = DateTime.UtcNow };
        return dbOk ? Ok(body) : StatusCode(503, body);
    }

    [HttpGet("version")]
    public IActionResult Version() => Ok(new { api = ApiVersion, formatPaquet = 1, heureUtc = DateTime.UtcNow });
}
