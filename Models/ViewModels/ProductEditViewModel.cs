using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Sadovod.Models.ViewModels;

public class ProductEditViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Укажите название")]
    [StringLength(200)]
    [Display(Name = "Название")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Описание")]
    public string? Description { get; set; }

    [Required]
    [Range(0, 1_000_000)]
    [Display(Name = "Цена, ₽")]
    [DataType(DataType.Currency)]
    public decimal Price { get; set; }

    [Required]
    [Range(0, 1_000_000)]
    [Display(Name = "Количество")]
    public int Quantity { get; set; }

    [Required(ErrorMessage = "Выберите категорию")]
    [Display(Name = "Категория")]
    public int? CategoryId { get; set; }

    [StringLength(100)]
    [Display(Name = "Сорт / разновидность")]
    public string? Variety { get; set; }

    [Display(Name = "Текущее изображение")]
    public string? ImageUrl { get; set; }

    [Display(Name = "Загрузить новое изображение")]
    public IFormFile? Image { get; set; }
}