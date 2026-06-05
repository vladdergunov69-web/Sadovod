namespace Sadovod.Models.Entities;

public class AuditLog
{
    public long Id { get; set; }
    public string TableName { get; set; } = null!;
    public string Operation { get; set; } = null!;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public int? UserId { get; set; }
    public DateTime ChangeDate { get; set; }

    public User? User { get; set; }
}