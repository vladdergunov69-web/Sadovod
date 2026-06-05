using Sadovod.Models.Entities;

namespace Sadovod.Models.ViewModels;

public class AuditLogFilterViewModel
{
    public string? TableName { get; set; }
    public string? Operation { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
    public int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);

    public List<AuditLog> Entries { get; set; } = new();
    public List<string> AvailableTables { get; set; } = new();
}
