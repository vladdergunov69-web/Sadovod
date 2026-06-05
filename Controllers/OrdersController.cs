using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sadovod.Data;
using Sadovod.Models.Entities;
using Sadovod.Models.ViewModels;
using Sadovod.Services;

namespace Sadovod.Controllers;

[Authorize]
public class OrdersController : Controller
{
    private readonly SadovodDbContext _db;
    private readonly ICartService _cart;

    public OrdersController(SadovodDbContext db, ICartService cart)
    {
        _db = db;
        _cart = cart;
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var cart = await _cart.GetCartAsync(HttpContext);
        if (cart.Items.Count == 0)
        {
            TempData["Info"] = "Корзина пуста.";
            return RedirectToAction("Index", "Cart");
        }

        var user = await _db.Users.FindAsync(CurrentUserId());
        return View(new CheckoutViewModel
        {
            Cart = cart,
            FullName = user?.FullName ?? string.Empty,
            Phone = user?.Phone ?? string.Empty
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create(CheckoutViewModel model)
    {
        model.Cart = await _cart.GetCartAsync(HttpContext);
        if (model.Cart.Items.Count == 0)
        {
            TempData["Info"] = "Корзина пуста.";
            return RedirectToAction("Index", "Cart");
        }

        if (!ModelState.IsValid) return View(model);

        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            var ids = model.Cart.Items.Select(i => i.ProductId).ToList();
            var products = await _db.Products.Where(p => ids.Contains(p.Id)).ToListAsync();

            foreach (var item in model.Cart.Items)
            {
                var p = products.FirstOrDefault(x => x.Id == item.ProductId);
                if (p is null || p.Quantity < item.Quantity)
                {
                    ModelState.AddModelError(string.Empty, $"Недостаточно остатков для «{item.Name}».");
                    return View(model);
                }
            }

            var order = new Order
            {
                UserId = CurrentUserId(),
                OrderDate = DateTime.Now,
                StatusId = 1,
                TotalAmount = model.Cart.Total
            };
            _db.Orders.Add(order);
            await _db.SaveChangesAsync();

            foreach (var item in model.Cart.Items)
            {
                var p = products.First(x => x.Id == item.ProductId);
                p.Quantity -= item.Quantity;
                _db.OrderItems.Add(new OrderItem
                {
                    OrderId = order.Id,
                    ProductId = p.Id,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice
                });
            }
            await _db.SaveChangesAsync();
            await tx.CommitAsync();
            _cart.Clear(HttpContext);

            TempData["Info"] = $"Заказ №{order.Id} создан.";
            return RedirectToAction(nameof(MyOrders));
        }
        catch
        {
            await tx.RollbackAsync();
            ModelState.AddModelError(string.Empty, "Не удалось создать заказ. Попробуйте ещё раз.");
            return View(model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> MyOrders()
    {
        var id = CurrentUserId();
        var orders = await _db.Orders
            .Include(o => o.Status)
            .Include(o => o.Items).ThenInclude(i => i.Product)
            .Where(o => o.UserId == id)
            .OrderByDescending(o => o.OrderDate)
            .AsNoTracking()
            .ToListAsync();
        return View(orders);
    }

    private int CurrentUserId()
    {
        var id = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(id, out var v) ? v : 0;
    }
}
