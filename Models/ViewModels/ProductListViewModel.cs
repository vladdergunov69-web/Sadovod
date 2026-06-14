using Sadovod.Models.Entities;

namespace Sadovod.Models.ViewModels;

public class ProductListViewModel
{
    public IReadOnlyList<Product> Products { get; set; } = Array.Empty<Product>();
    public IReadOnlyList<Category> Categories { get; set; } = Array.Empty<Category>();
    public string? Search { get; set; }
    public int? CategoryId { get; set; }

    // Расширенные фильтры и сортировка
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public bool InStockOnly { get; set; }
    public string? Sort { get; set; }   // name | price_asc | price_desc | stock_desc

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 12;
    public int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}