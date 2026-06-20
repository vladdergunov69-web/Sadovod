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
    Task<(bool ok, string? error, User? user, long? auditId)> ValidateAsync(string login, string password);
    Task SignInAsync(HttpContext http, User user, bool rememberMe, long? auditId = null);
    Task SignOutAsync(HttpContext http);
}

public class AuthService : IAuthService
{
    private readonly SadovodDbContext _db;
    private readonly IHttpContextAccessor _http;
    private readonly ILogger<AuthService> _log;

    public AuthService(SadovodDbContext db, IHttpContextAccessor http, ILogger<AuthService> log)
    {
        _db = db;
        _http = http;
        _log = log;
    }

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

    public async Task<(bool ok, string? error, User? user, long? auditId)> ValidateAsync(string login, string password)
    {
        login = login.Trim().ToLowerInvariant();
        var user = await _db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Login == login);

        // Запись в журнал — ДО выдачи куки (контроллер вызывает SignInAsync уже после).
        // Пароль НЕ передаётся и НЕ журналируется ни в каком виде.
        if (user is null)
        {
            // Неуспех, логина нет в системе → UserId не заполняется.
            await WriteLoginAuditAsync(login, 'F', null);
            return (false, "Неверный логин или пароль.", null, null);
        }
        if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
        {
            // Неуспех, но логин существует → UserId заполняется.
            await WriteLoginAuditAsync(login, 'F', user.Id);
            return (false, "Неверный логин или пароль.", null, null);
        }

        // Успех → UserId заполняется, Id записи пробрасывается в claim для привязки выхода.
        var auditId = await WriteLoginAuditAsync(login, 'S', user.Id);
        return (true, null, user, auditId);
    }

    public async Task SignInAsync(HttpContext http, User user, bool rememberMe, long? auditId = null)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Login),
            new("FullName", user.FullName),
            new(ClaimTypes.Role, user.Role?.Name ?? "Пользователь")
        };
        if (auditId.HasValue)
            claims.Add(new Claim("LoginAuditId", auditId.Value.ToString()));

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        var props = new AuthenticationProperties
        {
            IsPersistent = rememberMe,
            ExpiresUtc = DateTimeOffset.UtcNow.AddDays(rememberMe ? 30 : 1)
        };
        await http.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, props);
    }

    public async Task SignOutAsync(HttpContext http)
    {
        // Отметка времени выхода в соответствующей записи журнала.
        // Сбой журналирования не должен помешать выходу — оборачиваем в try/catch.
        try
        {
            var auditIdClaim = http.User.FindFirst("LoginAuditId")?.Value;
            if (long.TryParse(auditIdClaim, out var auditId))
            {
                // LogoutAt = GETDATE() — часы БД, как и LoginAt (не зависит от таймзоны web-контейнера).
                await _db.Database.ExecuteSqlInterpolatedAsync(
                    $"UPDATE dbo.LoginAuditLog SET LogoutAt = GETDATE() WHERE Id = {auditId} AND LogoutAt IS NULL");
            }
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Не удалось отметить время выхода (LogoutAt) в LoginAuditLog.");
        }

        await http.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    }

    /// <summary>
    /// Записывает попытку входа в LoginAuditLog. Полностью изолирована в try/catch:
    /// ошибка журналирования не прерывает аутентификацию. Возвращает Id записи или null.
    /// LoginAt проставляет БД (GETDATE()). Пароль не сохраняется никогда.
    /// </summary>
    private async Task<long?> WriteLoginAuditAsync(string login, char result, int? userId)
    {
        try
        {
            var ip = _http.HttpContext?.Connection.RemoteIpAddress?.ToString();
            if (ip is { Length: > 45 }) ip = ip[..45];

            var entry = new LoginAuditLog
            {
                Login = login,
                UserId = userId,
                Result = result.ToString(),
                IpAddress = ip
                // LoginAt — значение по умолчанию GETDATE() на стороне БД.
            };
            _db.LoginAuditLogs.Add(entry);
            await _db.SaveChangesAsync();
            return entry.Id;
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Не удалось записать попытку входа в LoginAuditLog (login={Login}, result={Result}).",
                login, result);
            return null;
        }
    }
}
