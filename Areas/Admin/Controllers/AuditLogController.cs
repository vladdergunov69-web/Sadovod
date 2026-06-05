using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sadovod.Data;
using Sadovod.Models.ViewModels;

namespace Sadovod.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "AdminOnly")]
public class AuditLogController : Controller
{
    private readonly SadovodDbContext _db;
    public AuditLogController(SadovodDbContext db) => _db = db;

    public async Task<IActionResult> Index(AuditLogFilterViewModel filter)
    {
        if (filter.Page < 1) filter.Page = 1;

        var q = _db.AuditLogs.Include(a => a.User).AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.TableName))
            q = q.Where(a => a.TableName == filter.TableName);
        if (!string.IsNullOrWhiteSpace(filter.Operation))
            q = q.Where(a => a.Operation == filter.Operation);
        if (filter.DateFrom.HasValue)
            q = q.Where(a => a.ChangeDate >= filter.DateFrom.Value);
        if (filter.DateTo.HasValue)
        {
            var to = filter.DateTo.Value.Date.AddDays(1);
            q = q.Where(a => a.ChangeDate < to);
        }

        filter.TotalCount = await q.CountAsync();
        filter.Entries = await q
            .OrderByDescending(a => a.ChangeDate)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        filter.AvailableTables = await _db.AuditLogs
            .Select(a => a.TableName).Distinct().OrderBy(x => x).ToListAsync();
        return View(filter);
    }
}
