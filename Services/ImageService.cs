using Microsoft.AspNetCore.Hosting;

namespace Sadovod.Services;

public interface IImageService
{
    Task<(bool ok, string? url, string? error)> SaveProductImageAsync(IFormFile file);
    void DeleteIfLocal(string? url);
}

public class ImageService : IImageService
{
    private static readonly HashSet<string> AllowedExt =
        new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".webp" };
    private const long MaxBytes = 5 * 1024 * 1024;
    private const string Folder = "images/products";
    private const string PlaceholderUrl = "/images/products/no-photo.png";

    private readonly IWebHostEnvironment _env;
    public ImageService(IWebHostEnvironment env) => _env = env;

    public async Task<(bool ok, string? url, string? error)> SaveProductImageAsync(IFormFile file)
    {
        if (file is null || file.Length == 0)
            return (false, null, "Файл не выбран.");
        if (file.Length > MaxBytes)
            return (false, null, "Файл больше 5 МБ.");

        var ext = Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(ext) || !AllowedExt.Contains(ext))
            return (false, null, "Допустимы только jpg, jpeg, png, webp.");

        var dir = Path.Combine(_env.WebRootPath, Folder);
        Directory.CreateDirectory(dir);
        var fileName = $"{Guid.NewGuid():N}{ext.ToLowerInvariant()}";
        var path = Path.Combine(dir, fileName);

        await using (var fs = File.Create(path))
            await file.CopyToAsync(fs);

        return (true, $"/{Folder}/{fileName}", null);
    }

    public void DeleteIfLocal(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return;
        if (string.Equals(url, PlaceholderUrl, StringComparison.OrdinalIgnoreCase)) return;
        if (!url.StartsWith("/")) return;

        var rel = url.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var path = Path.Combine(_env.WebRootPath, rel);
        try { if (File.Exists(path)) File.Delete(path); }
        catch { /* swallow */ }
    }
}
