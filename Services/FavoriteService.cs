using Microsoft.EntityFrameworkCore;
using Sadovod.Data;
using Sadovod.Models.Entities;

namespace Sadovod.Services;

public interface IFavoriteService
{
    Task<bool> AddAsync(int userId, int productId);
    Task<bool> RemoveAsync(int userId, int productId);
    Task<bool> IsFavoriteAsync(int userId, int productId);
    Task<HashSet<int>> GetProductIdsAsync(int userId);
    Task<int> GetCountAsync(int userId);
    Task<IReadOnlyList<Product>> GetFavoritesAsync(int userId);
}

public class FavoriteService : IFavoriteService
{
    private readonly SadovodDbContext _db;
    public FavoriteService(SadovodDbContext db) => _db = db;

    /// <summary>Добавляет товар в избранное. Возвращает true, если товар отмечен (был или стал).</summary>
    public async Task<bool> AddAsync(int userId, int productId)
    {
        // Товар должен существовать (защита от подбора несуществующего productId).
        if (!await _db.Products.AnyAsync(p => p.Id == productId))
            return false;

        var exists = await _db.Favorites.AnyAsync(f => f.UserId == userId && f.ProductId == productId);
        if (exists) return true;

        _db.Favorites.Add(new Favorite { UserId = userId, ProductId = productId });
        await _db.SaveChangesAsync();
        return true;
    }

    /// <summary>Убирает товар из избранного. Возвращает true, если после операции товар НЕ в избранном.</summary>
    public async Task<bool> RemoveAsync(int userId, int productId)
    {
        var fav = await _db.Favorites
            .FirstOrDefaultAsync(f => f.UserId == userId && f.ProductId == productId);
        if (fav is not null)
        {
            _db.Favorites.Remove(fav);
            await _db.SaveChangesAsync();
        }
        return true;
    }

    public Task<bool> IsFavoriteAsync(int userId, int productId) =>
        _db.Favorites.AnyAsync(f => f.UserId == userId && f.ProductId == productId);

    public async Task<HashSet<int>> GetProductIdsAsync(int userId)
    {
        var ids = await _db.Favorites
            .Where(f => f.UserId == userId)
            .Select(f => f.ProductId)
            .ToListAsync();
        return ids.ToHashSet();
    }

    public Task<int> GetCountAsync(int userId) =>
        _db.Favorites.CountAsync(f => f.UserId == userId);

    /// <summary>
    /// Возвращает избранные товары пользователя одним SQL-запросом:
    /// JOIN Favorites → Products → Categories через прямые навигации
    /// (Favorite.Product, Product.Category). Обратные навигации в Product/User
    /// не нужны. Сортировка по дате добавления (новые сверху).
    /// </summary>
    public async Task<IReadOnlyList<Product>> GetFavoritesAsync(int userId)
    {
        var favorites = await _db.Favorites
            .Where(f => f.UserId == userId)
            .OrderByDescending(f => f.AddedAt)
            .Include(f => f.Product!)
                .ThenInclude(p => p.Category)
            .AsNoTracking()
            .ToListAsync();

        // Проекция в товары — в памяти, чтобы Include не игнорировался EF.
        return favorites.Select(f => f.Product!).ToList();
    }
}
