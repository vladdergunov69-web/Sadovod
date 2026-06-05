namespace Sadovod.Models.ViewModels;

public class DashboardViewModel
{
    public int ProductsCount { get; set; }
    public int UsersCount { get; set; }
    public int OrdersCount { get; set; }
    public decimal CompletedRevenue { get; set; }
    public List<DailySalesPoint> SalesByDay { get; set; } = new();
}

public class DailySalesPoint
{
    public DateTime Day { get; set; }
    public decimal Amount { get; set; }
}