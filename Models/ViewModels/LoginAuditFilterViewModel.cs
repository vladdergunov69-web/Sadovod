using Sadovod.Models.Entities;

namespace Sadovod.Models.ViewModels;

public class LoginAuditFilterViewModel
{
    public string? Login { get; set; }
    public string? Result { get; set; }   // S | F
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
    public int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);

    public List<LoginAuditLog> Entries { get; set; } = new();

    /// <summary>Логины с признаком подозрительной активности (для подсветки строк).</summary>
    public HashSet<string> SuspiciousLogins { get; set; } = new();
}
