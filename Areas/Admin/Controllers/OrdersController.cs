using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sadovod.Data;
using Sadovod.Models.ViewModels;
using Sadovod.Services;

namespace Sadovod.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "AdminOnly")]
public class OrdersController : Controller
{
    private readonly SadovodDbContext _db;
    private readonly IPushNotificationService _push;

    public OrdersController(SadovodDbContext db, IPushNotificationService push)
    {
        _db = db;
        _push = push;
    }

    public async Task<IActionResult> Index(int? statusId, int page = 1)
    {
        const int pageSize = 15;
        if (page < 1) page = 1;

        var q = _db.Orders
            .Include(o => o.User)
            .Include(o => o.Status)
            .Include(o => o.Items).ThenInclude(i => i.Product)
            .AsNoTracking()
            .AsQueryable();

        if (statusId.HasValue && statusId.Value > 0)
            q = q.Where(o => o.StatusId == statusId.Value);

        var total = await q.CountAsync();
        var orders = await q
            .OrderByDescending(o => o.OrderDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var vm = new AdminOrderListViewModel
        {
            Orders = orders,
            Statuses = await _db.OrderStatuses.AsNoTracking().OrderBy(s => s.Id).ToListAsync(),
            StatusId = statusId,
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        };
        return View(vm);
    }

    [HttpPost]
    public async Task<IActionResult> ChangeStatus(int id, int statusId, int? page, int? filterStatusId)
    {
        var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == id);
        var status = await _db.OrderStatuses.FirstOrDefaultAsync(s => s.Id == statusId);

        if (order is null || status is null)
        {
            TempData["Info"] = "Заказ или статус не найден.";
            return RedirectToAction(nameof(Index), new { statusId = filterStatusId, page });
        }

        if (order.StatusId != statusId)
        {
            order.StatusId = statusId;
            await _db.SaveChangesAsync();

            // Адресный push покупателю (если у него есть подписка)
            await _push.SendToUserAsync(
                order.UserId,
                "Статус заказа изменён",
                $"Заказ №{order.Id}: новый статус — «{status.Name}».",
                "/Orders/MyOrders");

            TempData["Info"] = $"Статус заказа №{order.Id} изменён на «{status.Name}».";
        }

        return RedirectToAction(nameof(Index), new { statusId = filterStatusId, page });
    }
}
