namespace Sadovod.Models.Entities;

public class PushSubscription
{
    public int Id { get; set; }
    public int? UserId { get; set; }
    public string Endpoint { get; set; } = null!;
    public string P256dh { get; set; } = null!;
    public string Auth { get; set; } = null!;

    public User? User { get; set; }
}