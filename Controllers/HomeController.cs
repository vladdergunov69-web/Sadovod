using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sadovod.Data;
using Sadovod.Services;

namespace Sadovod.Controllers;

public class HomeController : Controller
{
    private readonly SadovodDbContext _db;
    private readonly ISettingsService _settings;
    private readonly IFavoriteService _fav;

    public HomeController(SadovodDbContext db, ISettingsService settings, IFavoriteService fav)
    {
        _db = db;
        _settings = settings;
        _fav = fav;
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
        ViewData["FavoriteIds"] = await CurrentFavoritesAsync();
        return View(top);
    }

    private async Task<HashSet<int>> CurrentFavoritesAsync()
    {
        if (User.Identity?.IsAuthenticated != true) return new();
        var id = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(id, out var userId)) return new();
        return await _fav.GetProductIdsAsync(userId);
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
