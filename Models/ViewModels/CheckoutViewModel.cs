using System.ComponentModel.DataAnnotations;

namespace Sadovod.Models.ViewModels;

public class CheckoutViewModel
{
    [Required(ErrorMessage = "Укажите ФИО")]
    [Display(Name = "ФИО получателя")]
    [StringLength(150)]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Укажите телефон")]
    [Phone]
    [Display(Name = "Контактный телефон")]
    [StringLength(20)]
    public string Phone { get; set; } = string.Empty;

    [Display(Name = "Комментарий к заказу")]
    [StringLength(500)]
    public string? Comment { get; set; }

    public CartViewModel Cart { get; set; } = new();
}
