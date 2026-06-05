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

    public ProductsController(SadovodDbContext db, ISettingsService settings)
    {
        _db = db;
        _settings = settings;
    }

    public async Task<IActionResult> Index(string? search, int? categoryId, int page = 1)
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

        var total = await q.CountAsync();
        var items = await q
            .OrderBy(p => p.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var vm = new ProductListViewModel
        {
            Products = items,
            Categories = await _settings.GetCategoriesAsync(),
            Search = search,
            CategoryId = categoryId,
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        };
        return View(vm);
    }

    public async Task<IActionResult> Details(int id)
    {
        var product = await _db.Products
            .Include(p => p.Category)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id);
        if (product is null) return NotFound();
        return View(product);
    }
}
