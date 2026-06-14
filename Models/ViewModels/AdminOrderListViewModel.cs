using Sadovod.Models.Entities;

namespace Sadovod.Models.ViewModels;

public class AdminOrderListViewModel
{
    public IReadOnlyList<Order> Orders { get; set; } = Array.Empty<Order>();
    public IReadOnlyList<OrderStatus> Statuses { get; set; } = Array.Empty<OrderStatus>();

    // Фильтр по статусу (null = все)
    public int? StatusId { get; set; }

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 15;
    public int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}
