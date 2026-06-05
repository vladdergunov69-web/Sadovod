using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Sadovod.Data;
using Sadovod.Models.Entities;

namespace Sadovod.Services;

public interface IAuthService
{
    Task<(bool ok, string? error)> RegisterAsync(string login, string password, string fullName, string? phone);
    Task<(bool ok, string? error, User? user)> ValidateAsync(string login, string password);
    Task SignInAsync(HttpContext http, User user, bool rememberMe);
    Task SignOutAsync(HttpContext http);
}

public class AuthService : IAuthService
{
    private readonly SadovodDbContext _db;
    public AuthService(SadovodDbContext db) => _db = db;

    public async Task<(bool ok, string? error)> RegisterAsync(string login, string password, string fullName, string? phone)
    {
        login = login.Trim().ToLowerInvariant();
        if (await _db.Users.AnyAsync(u => u.Login == login))
            return (false, "Пользователь с таким email уже зарегистрирован.");

        var user = new User
        {
            Login = login,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            FullName = fullName.Trim(),
            Phone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim(),
            RoleId = 2
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool ok, string? error, User? user)> ValidateAsync(string login, string password)
    {
        login = login.Trim().ToLowerInvariant();
        var user = await _db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Login == login);
        if (user is null) return (false, "Неверный логин или пароль.", null);
        if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            return (false, "Неверный логин или пароль.", null);
        return (true, null, user);
    }

    public async Task SignInAsync(HttpContext http, User user, bool rememberMe)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Login),
            new("FullName", user.FullName),
            new(ClaimTypes.Role, user.Role?.Name ?? "Пользователь")
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        var props = new AuthenticationProperties
        {
            IsPersistent = rememberMe,
            ExpiresUtc = DateTimeOffset.UtcNow.AddDays(rememberMe ? 30 : 1)
        };
        await http.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, props);
    }

    public Task SignOutAsync(HttpContext http) =>
        http.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
}
