using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sadovod.Data;
using Sadovod.Services;

namespace Sadovod.Controllers;

public class HomeController : Controller
{
    private readonly SadovodDbContext _db;
    private readonly ISettingsService _settings;

    public HomeController(SadovodDbContext db, ISettingsService settings)
    {
        _db = db;
        _settings = settings;
    }

    public async Task<IActionResult> Index()
    {
        var top = await _db.Products
            .Include(p => p.Category)
            .Where(p => p.Quantity > 0)
            .OrderByDescending(p => p.Quantity)
            .Take(4)
            .AsNoTracking()
            .ToListAsync();
        return View(top);
    }

    public async Task<IActionResult> About()
    {
        var s = await _settings.GetAsync();
        return View(s);
    }

    public async Task<IActionResult> Contacts()
    {
        var s = await _settings.GetAsync();
        return View(s);
    }

    [Route("Home/Error/{code:int?}")]
    public IActionResult Error(int? code)
    {
        ViewBag.StatusCode = code ?? 500;
        return View();
    }
}
