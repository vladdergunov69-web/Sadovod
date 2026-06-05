using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Sadovod.Data;
using Sadovod.Models.Entities;

namespace Sadovod.Services;

public interface ISettingsService
{
    Task<ShopSetting> GetAsync();
    Task SaveAsync(ShopSetting model);
    Task<IReadOnlyList<Category>> GetCategoriesAsync();
    void InvalidateAll();
}

public class SettingsService : ISettingsService
{
    private const string SettingsKey = "shop:settings";
    private const string CategoriesKey = "shop:categories";

    private readonly SadovodDbContext _db;
    private readonly IMemoryCache _cache;

    public SettingsService(SadovodDbContext db, IMemoryCache cache)
    {
        _db = db;
        _cache = cache;
    }

    public async Task<ShopSetting> GetAsync()
    {
        if (_cache.TryGetValue<ShopSetting>(SettingsKey, out var cached) && cached is not null)
            return cached;

        var s = await _db.ShopSettings.AsNoTracking().FirstOrDefaultAsync()
                ?? new ShopSetting();
        _cache.Set(SettingsKey, s, TimeSpan.FromMinutes(15));
        return s;
    }

    public async Task SaveAsync(ShopSetting model)
    {
        var s = await _db.ShopSettings.FirstOrDefaultAsync();
        if (s is null)
        {
            _db.ShopSettings.Add(model);
        }
        else
        {
            s.Email = model.Email;
            s.Phone = model.Phone;
            s.Address = model.Address;
            s.AboutUs = model.AboutUs;
        }
        await _db.SaveChangesAsync();
        InvalidateAll();
    }

    public async Task<IReadOnlyList<Category>> GetCategoriesAsync()
    {
        if (_cache.TryGetValue<IReadOnlyList<Category>>(CategoriesKey, out var cached) && cached is not null)
            return cached;

        var list = await _db.Categories.AsNoTracking().OrderBy(c => c.Name).ToListAsync();
        _cache.Set(CategoriesKey, (IReadOnlyList<Category>)list, TimeSpan.FromMinutes(30));
        return list;
    }

    public void InvalidateAll()
    {
        _cache.Remove(SettingsKey);
        _cache.Remove(CategoriesKey);
    }
}
