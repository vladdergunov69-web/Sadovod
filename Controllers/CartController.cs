using Microsoft.AspNetCore.Mvc;
using Sadovod.Services;

namespace Sadovod.Controllers;

public class CartController : Controller
{
    private readonly ICartService _cart;
    public CartController(ICartService cart) => _cart = cart;

    public async Task<IActionResult> Index()
    {
        var vm = await _cart.GetCartAsync(HttpContext);
        return View(vm);
    }

    [HttpPost]
    public async Task<IActionResult> AddToCart(int productId, int qty = 1, string? returnUrl = null)
    {
        await _cart.AddAsync(HttpContext, productId, qty);
        TempData["Info"] = "Товар добавлен в корзину.";
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)) return Redirect(returnUrl);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public IActionResult RemoveFromCart(int productId)
    {
        _cart.Remove(HttpContext, productId);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> UpdateQuantity(int productId, int qty)
    {
        await _cart.UpdateAsync(HttpContext, productId, qty);
        return RedirectToAction(nameof(Index));
    }
}
