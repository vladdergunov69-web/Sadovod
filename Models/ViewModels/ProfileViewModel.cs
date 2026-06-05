using System.ComponentModel.DataAnnotations;

namespace Sadovod.Models.ViewModels;

public class ProfileViewModel
{
    public int Id { get; set; }

    [Display(Name = "Логин")]
    public string Login { get; set; } = string.Empty;

    [Required(ErrorMessage = "Введите ФИО")]
    [Display(Name = "ФИО")]
    [StringLength(150, MinimumLength = 3)]
    public string FullName { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Некорректный телефон")]
    [Display(Name = "Телефон")]
    [StringLength(20)]
    public string? Phone { get; set; }
}