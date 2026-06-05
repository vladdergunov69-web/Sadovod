namespace Sadovod.Models.Entities;

public class User
{
    public int Id { get; set; }
    public string Login { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public string FullName { get; set; } = null!;
    public string? Phone { get; set; }
    public int RoleId { get; set; }

    public Role? Role { get; set; }
    public ICollection<Order> Orders { get; set; } = new List<Order>();
    public ICollection<PushSubscription> PushSubscriptions { get; set; } = new List<PushSubscription>();
}