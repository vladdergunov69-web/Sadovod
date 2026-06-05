using System.ComponentModel.DataAnnotations;

namespace Sadovod.Models.ViewModels;

public class SettingsViewModel
{
    [EmailAddress]
    [Display(Name = "Email магазина")]
    [StringLength(100)]
    public string? Email { get; set; }

    [Phone]
    [Display(Name = "Телефон магазина")]
    [StringLength(20)]
    public string? Phone { get; set; }

    [Display(Name = "Адрес")]
    [StringLength(300)]
    public string? Address { get; set; }

    [Display(Name = "О нас")]
    public string? AboutUs { get; set; }
}
