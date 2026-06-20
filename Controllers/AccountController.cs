using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sadovod.Data;
using Sadovod.Models.ViewModels;
using Sadovod.Services;

namespace Sadovod.Controllers;

public class AccountController : Controller
{
    private readonly IAuthService _auth;
    private readonly SadovodDbContext _db;

    public AccountController(IAuthService auth, SadovodDbContext db)
    {
        _auth = auth;
        _db = db;
    }

    [HttpGet]
    public IActionResult Register() => View(new RegisterViewModel());

    [HttpPost]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var (ok, error) = await _auth.RegisterAsync(model.Login, model.Password, model.FullName, model.Phone);
        if (!ok)
        {
            ModelState.AddModelError(string.Empty, error ?? "Ошибка регистрации");
            return View(model);
        }
        TempData["Info"] = "Регистрация прошла успешно. Войдите в аккаунт.";
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl) => View(new LoginViewModel { ReturnUrl = returnUrl });

    [HttpPost]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var (ok, error, user, auditId) = await _auth.ValidateAsync(model.Login, model.Password);
        if (!ok || user is null)
        {
            ModelState.AddModelError(string.Empty, error ?? "Ошибка входа");
            return View(model);
        }
        await _auth.SignInAsync(HttpContext, user, model.RememberMe, auditId);
        if (!string.IsNullOrWhiteSpace(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            return Redirect(model.ReturnUrl);
        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await _auth.SignOutAsync(HttpContext);
        return RedirectToAction("Index", "Home");
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var id = CurrentUserId();
        var user = await _db.Users.FindAsync(id);
        if (user is null) return NotFound();
        return View(new ProfileViewModel
        {
            Id = user.Id,
            Login = user.Login,
            FullName = user.FullName,
            Phone = user.Phone
        });
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Profile(ProfileViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var id = CurrentUserId();
        var user = await _db.Users.FindAsync(id);
        if (user is null) return NotFound();
        user.FullName = model.FullName.Trim();
        user.Phone = string.IsNullOrWhiteSpace(model.Phone) ? null : model.Phone.Trim();
        await _db.SaveChangesAsync();
        TempData["Info"] = "Профиль обновлён.";
        return RedirectToAction(nameof(Profile));
    }

    private int CurrentUserId()
    {
        var id = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(id, out var v) ? v : 0;
    }
}
