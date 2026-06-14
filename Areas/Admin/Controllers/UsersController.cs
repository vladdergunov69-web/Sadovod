using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sadovod.Data;
using Sadovod.Models.ViewModels;

namespace Sadovod.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "AdminOnly")]
public class UsersController : Controller
{
    private readonly SadovodDbContext _db;
    public UsersController(SadovodDbContext db) => _db = db;

    public async Task<IActionResult> Index(int page = 1)
    {
        const int pageSize = 20;
        if (page < 1) page = 1;

        var q = _db.Users.Include(u => u.Role).AsNoTracking().AsQueryable();

        var total = await q.CountAsync();
        var users = await q
            .OrderBy(u => u.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var vm = new UserListViewModel
        {
            Users = users,
            Roles = await _db.Roles.AsNoTracking().OrderBy(r => r.Id).ToListAsync(),
            CurrentUserId = CurrentUserId(),
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        };
        return View(vm);
    }

    [HttpPost]
    public async Task<IActionResult> ChangeRole(int id, int roleId, int? page)
    {
        // Защита от блокировки: администратор не может изменить роль самому себе
        if (id == CurrentUserId())
        {
            TempData["Info"] = "Нельзя изменить роль собственной учётной записи.";
            return RedirectToAction(nameof(Index), new { page });
        }

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id);
        var role = await _db.Roles.FirstOrDefaultAsync(r => r.Id == roleId);
        if (user is null || role is null)
        {
            TempData["Info"] = "Пользователь или роль не найдены.";
            return RedirectToAction(nameof(Index), new { page });
        }

        if (user.RoleId != roleId)
        {
            user.RoleId = roleId;
            await _db.SaveChangesAsync(); // изменение фиксируется триггером trg_Users_Update
            TempData["Info"] = $"Пользователю «{user.Login}» назначена роль «{role.Name}».";
        }

        return RedirectToAction(nameof(Index), new { page });
    }

    private int CurrentUserId()
    {
        var id = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(id, out var v) ? v : 0;
    }
}
