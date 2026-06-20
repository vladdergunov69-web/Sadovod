namespace Sadovod.Models.Entities;

public class LoginAuditLog
{
    public long Id { get; set; }
    public string Login { get; set; } = null!;
    public int? UserId { get; set; }
    public string Result { get; set; } = null!;   // "S" = Success, "F" = Failed
    public string? IpAddress { get; set; }
    public DateTime LoginAt { get; set; }
    public DateTime? LogoutAt { get; set; }

    public User? User { get; set; }
}
