using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Sadovod.Data;
using Sadovod.Models.ViewModels;

namespace Sadovod.Services;

public interface ICartService
{
    Task<CartViewModel> GetCartAsync(HttpContext ctx);
    Task AddAsync(HttpContext ctx, int productId, int qty = 1);
    void Remove(HttpContext ctx, int productId);
    Task UpdateAsync(HttpContext ctx, int productId, int qty);
    void Clear(HttpContext ctx);
    Dictionary<int, int> GetRaw(HttpContext ctx);
}

public class CartService : ICartService
{
    private const string Key = "Cart";
    private readonly SadovodDbContext _db;
    public CartService(SadovodDbContext db) => _db = db;

    public Dictionary<int, int> GetRaw(HttpContext ctx)
    {
        var json = ctx.Session.GetString(Key);
        if (string.IsNullOrEmpty(json)) return new();
        try
        {
            return JsonSerializer.Deserialize<Dictionary<int, int>>(json) ?? new();
        }
        catch { return new(); }
    }

    private void Save(HttpContext ctx, Dictionary<int, int> dict) =>
        ctx.Session.SetString(Key, JsonSerializer.Serialize(dict));

    public async Task<CartViewModel> GetCartAsync(HttpContext ctx)
    {
        var raw = GetRaw(ctx);
        var vm = new CartViewModel();
        if (raw.Count == 0) return vm;

        var ids = raw.Keys.ToList();
        var products = await _db.Products.Where(p => ids.Contains(p.Id)).AsNoTracking().ToListAsync();
        foreach (var p in products)
        {
            var q = raw[p.Id];
            if (q > p.Quantity) q = p.Quantity;
            if (q <= 0) continue;
            vm.Items.Add(new CartItemViewModel
            {
                ProductId = p.Id,
                Name = p.Name,
                ImageUrl = p.ImageUrl,
                UnitPrice = p.Price,
                Quantity = q,
                Stock = p.Quantity
            });
        }
        return vm;
    }

    public async Task AddAsync(HttpContext ctx, int productId, int qty = 1)
    {
        if (qty <= 0) qty = 1;
        var product = await _db.Products.FindAsync(productId);
        if (product is null || product.Quantity <= 0) return;

        var raw = GetRaw(ctx);
        raw.TryGetValue(productId, out var current);
        var newQty = Math.Min(current + qty, product.Quantity);
        raw[productId] = newQty;
        Save(ctx, raw);
    }

    public void Remove(HttpContext ctx, int productId)
    {
        var raw = GetRaw(ctx);
        if (raw.Remove(productId)) Save(ctx, raw);
    }

    public async Task UpdateAsync(HttpContext ctx, int productId, int qty)
    {
        var raw = GetRaw(ctx);
        if (qty <= 0) { raw.Remove(productId); Save(ctx, raw); return; }
        var product = await _db.Products.FindAsync(productId);
        if (product is null) { raw.Remove(productId); Save(ctx, raw); return; }
        raw[productId] = Math.Min(qty, product.Quantity);
        Save(ctx, raw);
    }

    public void Clear(HttpContext ctx) => ctx.Session.Remove(Key);
}
