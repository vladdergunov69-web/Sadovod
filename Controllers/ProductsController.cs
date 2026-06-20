using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sadovod.Data;
using Sadovod.Models.ViewModels;
using Sadovod.Services;

namespace Sadovod.Controllers;

public class ProductsController : Controller
{
    private readonly SadovodDbContext _db;
    private readonly ISettingsService _settings;
    private readonly IFavoriteService _fav;

    public ProductsController(SadovodDbContext db, ISettingsService settings, IFavoriteService fav)
    {
        _db = db;
        _settings = settings;
        _fav = fav;
    }

    public async Task<IActionResult> Index(string? search, int? categoryId,
        decimal? minPrice, decimal? maxPrice, bool inStockOnly = false,
        string? sort = null, int page = 1)
    {
        const int pageSize = 12;
        if (page < 1) page = 1;

        var q = _db.Products.Include(p => p.Category).AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            q = q.Where(p => EF.Functions.Like(p.Name, $"%{s}%"));
        }
        if (categoryId.HasValue && categoryId.Value > 0)
            q = q.Where(p => p.CategoryId == categoryId.Value);

        // Фильтр по диапазону цены
        if (minPrice.HasValue)
            q = q.Where(p => p.Price >= minPrice.Value);
        if (maxPrice.HasValue)
            q = q.Where(p => p.Price <= maxPrice.Value);

        // Фильтр «только в наличии»
        if (inStockOnly)
            q = q.Where(p => p.Quantity > 0);

        // Динамическая сортировка
        q = sort switch
        {
            "price_asc" => q.OrderBy(p => p.Price),
            "price_desc" => q.OrderByDescending(p => p.Price),
            "stock_desc" => q.OrderByDescending(p => p.Quantity),
            _ => q.OrderBy(p => p.Name)
        };

        var total = await q.CountAsync();
        var items = await q
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var vm = new ProductListViewModel
        {
            Products = items,
            Categories = await _settings.GetCategoriesAsync(),
            Search = search,
            CategoryId = categoryId,
            MinPrice = minPrice,
            MaxPrice = maxPrice,
            InStockOnly = inStockOnly,
            Sort = sort,
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        };
        ViewData["FavoriteIds"] = await CurrentFavoritesAsync();
        return View(vm);
    }

    public async Task<IActionResult> Details(int id)
    {
        var product = await _db.Products
            .Include(p => p.Category)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id);
        if (product is null) return NotFound();
        ViewData["FavoriteIds"] = await CurrentFavoritesAsync();
        return View(product);
    }

    private async Task<HashSet<int>> CurrentFavoritesAsync()
    {
        if (User.Identity?.IsAuthenticated != true) return new();
        var id = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(id, out var userId)) return new();
        return await _fav.GetProductIdsAsync(userId);
    }
}
