using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sadovod.Models.ViewModels;
using Sadovod.Services;

namespace Sadovod.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "AdminOnly")]
public class PushController : Controller
{
    private readonly IPushNotificationService _push;
    public PushController(IPushNotificationService push) => _push = push;

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        return View(new PushSendViewModel
        {
            SubscribersCount = await _push.CountAsync(),
            PublicKey = _push.PublicKey
        });
    }

    [HttpPost]
    public async Task<IActionResult> Send(PushSendViewModel model)
    {
        model.SubscribersCount = await _push.CountAsync();
        model.PublicKey = _push.PublicKey;
        if (!ModelState.IsValid) return View(nameof(Index), model);

        var sent = await _push.SendToAllAsync(model.Title, model.Body, model.Url);
        TempData["Info"] = $"Отправлено уведомлений: {sent}.";
        return RedirectToAction(nameof(Index));
    }
}
