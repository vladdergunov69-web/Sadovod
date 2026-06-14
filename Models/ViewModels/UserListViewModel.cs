using Sadovod.Models.Entities;

namespace Sadovod.Models.ViewModels;

public class UserListViewModel
{
    public IReadOnlyList<User> Users { get; set; } = Array.Empty<User>();
    public IReadOnlyList<Role> Roles { get; set; } = Array.Empty<Role>();

    // Id текущего администратора — чтобы запретить понижение самого себя
    public int CurrentUserId { get; set; }

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}
