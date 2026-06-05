using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Sadovod.Models.ViewModels;
using Sadovod.Services;

namespace Sadovod.Controllers;

[Route("api/push")]
public class PushController : Controller
{
    private readonly IPushNotificationService _push;
    public PushController(IPushNotificationService push) => _push = push;

    [HttpGet("key")]
    public IActionResult PublicKey() => Json(new { publicKey = _push.PublicKey });

    [HttpPost("subscribe")]
    public async Task<IActionResult> Subscribe([FromBody] PushSubscriptionDto dto)
    {
        if (dto is null || string.IsNullOrWhiteSpace(dto.Endpoint)
                       || string.IsNullOrWhiteSpace(dto.Keys?.P256dh)
                       || string.IsNullOrWhiteSpace(dto.Keys?.Auth))
            return BadRequest();

        int? userId = null;
        if (User.Identity?.IsAuthenticated == true
            && int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id))
            userId = id;

        await _push.SaveSubscriptionAsync(userId, dto.Endpoint, dto.Keys.P256dh, dto.Keys.Auth);
        return Ok();
    }
}
