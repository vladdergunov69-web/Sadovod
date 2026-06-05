using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sadovod.Models.Entities;
using Sadovod.Models.ViewModels;
using Sadovod.Services;

namespace Sadovod.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "AdminOnly")]
public class SettingsController : Controller
{
    private readonly ISettingsService _settings;
    public SettingsController(ISettingsService settings) => _settings = settings;

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var s = await _settings.GetAsync();
        return View(new SettingsViewModel
        {
            Email = s.Email,
            Phone = s.Phone,
            Address = s.Address,
            AboutUs = s.AboutUs
        });
    }

    [HttpPost]
    public async Task<IActionResult> Save(SettingsViewModel model)
    {
        if (!ModelState.IsValid) return View(nameof(Index), model);
        await _settings.SaveAsync(new ShopSetting
        {
            Email = model.Email,
            Phone = model.Phone,
            Address = model.Address,
            AboutUs = model.AboutUs
        });
        TempData["Info"] = "Настройки сохранены.";
        return RedirectToAction(nameof(Index));
    }
}
