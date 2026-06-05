using System.ComponentModel.DataAnnotations;

namespace Sadovod.Models.ViewModels;

public class LoginViewModel
{
    [Required(ErrorMessage = "Введите email или логин")]
    [Display(Name = "Логин / Email")]
    [StringLength(100)]
    public string Login { get; set; } = string.Empty;

    [Required(ErrorMessage = "Введите пароль")]
    [DataType(DataType.Password)]
    [Display(Name = "Пароль")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Запомнить меня")]
    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }
}