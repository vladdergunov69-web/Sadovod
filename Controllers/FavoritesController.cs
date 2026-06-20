using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sadovod.Services;

namespace Sadovod.Controllers;

[Authorize]
[Route("Favorites")]
public class FavoritesController : Controller
{
    private readonly IFavoriteService _fav;
    public FavoritesController(IFavoriteService fav) => _fav = fav;

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var userId = CurrentUserId();
        var products = await _fav.GetFavoritesAsync(userId);
        return View(products);
    }

    [HttpPost("Add/{productId:int}")]
    public async Task<IActionResult> Add(int productId)
    {
        var userId = CurrentUserId();
        var isFavorite = await _fav.AddAsync(userId, productId);
        var count = await _fav.GetCountAsync(userId);
        return Json(new { isFavorite, count });
    }

    [HttpPost("Remove/{productId:int}")]
    public async Task<IActionResult> Remove(int productId)
    {
        var userId = CurrentUserId();
        await _fav.RemoveAsync(userId, productId);
        var count = await _fav.GetCountAsync(userId);
        return Json(new { isFavorite = false, count });
    }

    private int CurrentUserId()
    {
        var id = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(id, out var v) ? v : 0;
    }
}
