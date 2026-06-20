using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sadovod.Data;
using Sadovod.Models.ViewModels;

namespace Sadovod.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "AdminOnly")]
public class LoginAuditController : Controller
{
    private readonly SadovodDbContext _db;
    public LoginAuditController(SadovodDbContext db) => _db = db;

    public async Task<IActionResult> Index(LoginAuditFilterViewModel filter)
    {
        if (filter.Page < 1) filter.Page = 1;

        var q = _db.LoginAuditLogs.Include(a => a.User).AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Login))
        {
            var login = filter.Login.Trim();
            q = q.Where(a => a.Login.Contains(login));
        }
        if (!string.IsNullOrWhiteSpace(filter.Result))
            q = q.Where(a => a.Result == filter.Result);
        if (filter.DateFrom.HasValue)
            q = q.Where(a => a.LoginAt >= filter.DateFrom.Value);
        if (filter.DateTo.HasValue)
        {
            var to = filter.DateTo.Value.Date.AddDays(1);
            q = q.Where(a => a.LoginAt < to);
        }

        filter.TotalCount = await q.CountAsync();
        filter.Entries = await q
            .OrderByDescending(a => a.LoginAt)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        filter.SuspiciousLogins = await GetSuspiciousLoginsAsync();
        return View(filter);
    }

    /// <summary>
    /// Подозрительная активность (строгий критерий): последние 5 попыток по логину
    /// (упорядочены по LoginAt DESC, при равенстве — по Id DESC) — ВСЕ неуспешные ('F')
    /// и укладываются в последние 15 минут (самая ранняя из пяти не старше 15 мин).
    /// Любой успешный вход среди этих пяти снимает признак.
    /// Сравнение времени — по часам БД (GETDATE), без зависимости от таймзоны web-контейнера.
    /// Пересчитывается на каждый запрос (индексы по Login и LoginAt).
    /// </summary>
    private async Task<HashSet<string>> GetSuspiciousLoginsAsync()
    {
        const string sql = @"
SELECT Login AS Value
FROM (
    SELECT Login, Result, LoginAt,
           ROW_NUMBER() OVER (PARTITION BY Login ORDER BY LoginAt DESC, Id DESC) AS rn
    FROM dbo.LoginAuditLog
) t
WHERE rn <= 5
GROUP BY Login
HAVING COUNT(*) = 5
   AND SUM(CASE WHEN Result = 'F' THEN 1 ELSE 0 END) = 5
   AND MIN(LoginAt) >= DATEADD(MINUTE, -15, GETDATE());";

        var logins = await _db.Database.SqlQueryRaw<string>(sql).ToListAsync();
        return logins.ToHashSet();
    }
}
