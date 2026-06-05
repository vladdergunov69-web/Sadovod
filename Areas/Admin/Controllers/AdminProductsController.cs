using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Sadovod.Data;
using Sadovod.Models.Entities;
using Sadovod.Models.ViewModels;
using Sadovod.Services;

namespace Sadovod.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "AdminOnly")]
public class AdminProductsController : Controller
{
    private readonly SadovodDbContext _db;
    private readonly IImageService _images;
    private readonly ISettingsService _settings;

    public AdminProductsController(SadovodDbContext db, IImageService images, ISettingsService settings)
    {
        _db = db;
        _images = images;
        _settings = settings;
    }

    public async Task<IActionResult> Index()
    {
        var items = await _db.Products
            .Include(p => p.Category)
            .AsNoTracking()
            .OrderBy(p => p.Name)
            .ToListAsync();
        return View(items);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await PopulateCategoriesAsync();
        return View(new ProductEditViewModel());
    }

    [HttpPost]
    public async Task<IActionResult> Create(ProductEditViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateCategoriesAsync(model.CategoryId);
            return View(model);
        }

        string? imageUrl = "/images/products/no-photo.png";
        if (model.Image is not null)
        {
            var (ok, url, error) = await _images.SaveProductImageAsync(model.Image);
            if (!ok) { ModelState.AddModelError(nameof(model.Image), error!); await PopulateCategoriesAsync(model.CategoryId); return View(model); }
            imageUrl = url;
        }

        _db.Products.Add(new Product
        {
            Name = model.Name.Trim(),
            Description = model.Description,
            Price = model.Price,
            Quantity = model.Quantity,
            CategoryId = model.CategoryId,
            Variety = model.Variety,
            ImageUrl = imageUrl
        });
        await _db.SaveChangesAsync();
        TempData["Info"] = "Товар создан.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var p = await _db.Products.FindAsync(id);
        if (p is null) return NotFound();
        await PopulateCategoriesAsync(p.CategoryId);
        return View(new ProductEditViewModel
        {
            Id = p.Id,
            Name = p.Name,
            Description = p.Description,
            Price = p.Price,
            Quantity = p.Quantity,
            CategoryId = p.CategoryId,
            Variety = p.Variety,
            ImageUrl = p.ImageUrl
        });
    }

    [HttpPost]
    public async Task<IActionResult> Edit(ProductEditViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateCategoriesAsync(model.CategoryId);
            return View(model);
        }

        var p = await _db.Products.FindAsync(model.Id);
        if (p is null) return NotFound();

        if (model.Image is not null)
        {
            var (ok, url, error) = await _images.SaveProductImageAsync(model.Image);
            if (!ok)
            {
                ModelState.AddModelError(nameof(model.Image), error!);
                await PopulateCategoriesAsync(model.CategoryId);
                return View(model);
            }
            _images.DeleteIfLocal(p.ImageUrl);
            p.ImageUrl = url;
        }

        p.Name = model.Name.Trim();
        p.Description = model.Description;
        p.Price = model.Price;
        p.Quantity = model.Quantity;
        p.CategoryId = model.CategoryId;
        p.Variety = model.Variety;

        await _db.SaveChangesAsync();
        TempData["Info"] = "Изменения сохранены.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var p = await _db.Products.Include(x => x.Category).AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (p is null) return NotFound();
        return View(p);
    }

    [HttpPost, ActionName("Delete")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var p = await _db.Products.FindAsync(id);
        if (p is null) return NotFound();
        _images.DeleteIfLocal(p.ImageUrl);
        _db.Products.Remove(p);
        await _db.SaveChangesAsync();
        TempData["Info"] = "Товар удалён.";
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateCategoriesAsync(int? selected = null)
    {
        var cats = await _settings.GetCategoriesAsync();
        ViewBag.Categories = new SelectList(cats, "Id", "Name", selected);
    }
}
