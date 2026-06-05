using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sadovod.Data;
using Sadovod.Models.ViewModels;

namespace Sadovod.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "AdminOnly")]
public class DashboardController : Controller
{
    private readonly SadovodDbContext _db;
    public DashboardController(SadovodDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        var since = DateTime.Today.AddDays(-13);

        var dailyRaw = await _db.Orders
            .Where(o => o.OrderDate >= since && o.StatusId == 3)
            .GroupBy(o => o.OrderDate.Date)
            .Select(g => new { Day = g.Key, Amount = g.Sum(x => x.TotalAmount) })
            .ToListAsync();

        var byDay = dailyRaw.ToDictionary(x => x.Day, x => x.Amount);
        var sales = new List<DailySalesPoint>();
        for (int i = 0; i < 14; i++)
        {
            var d = since.AddDays(i);
            sales.Add(new DailySalesPoint { Day = d, Amount = byDay.TryGetValue(d, out var v) ? v : 0m });
        }

        var vm = new DashboardViewModel
        {
            ProductsCount = await _db.Products.CountAsync(),
            UsersCount = await _db.Users.CountAsync(),
            OrdersCount = await _db.Orders.CountAsync(),
            CompletedRevenue = await _db.Orders.Where(o => o.StatusId == 3).SumAsync(o => (decimal?)o.TotalAmount) ?? 0m,
            SalesByDay = sales
        };
        return View(vm);
    }
}
