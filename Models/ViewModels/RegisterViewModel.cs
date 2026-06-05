using System.ComponentModel.DataAnnotations;

namespace Sadovod.Models.ViewModels;

public class RegisterViewModel
{
    [Required(ErrorMessage = "Укажите email")]
    [EmailAddress(ErrorMessage = "Некорректный email")]
    [Display(Name = "Email (логин)")]
    [StringLength(100)]
    public string Login { get; set; } = string.Empty;

    [Required(ErrorMessage = "Введите ФИО")]
    [Display(Name = "ФИО")]
    [StringLength(150, MinimumLength = 3)]
    public string FullName { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Некорректный телефон")]
    [Display(Name = "Телефон")]
    [StringLength(20)]
    public string? Phone { get; set; }

    [Required(ErrorMessage = "Придумайте пароль")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Минимум 6 символов")]
    [DataType(DataType.Password)]
    [Display(Name = "Пароль")]
    public string Password { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "Пароли не совпадают")]
    [Display(Name = "Повтор пароля")]
    public string ConfirmPassword { get; set; } = string.Empty;
}