using System.ComponentModel.DataAnnotations;

namespace Sadovod.Models.ViewModels;

public class PushSendViewModel
{
    [Required(ErrorMessage = "Введите заголовок")]
    [StringLength(120)]
    [Display(Name = "Заголовок")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Введите текст сообщения")]
    [StringLength(500)]
    [Display(Name = "Текст уведомления")]
    public string Body { get; set; } = string.Empty;

    [Url]
    [Display(Name = "Ссылка при клике")]
    public string? Url { get; set; }

    public int SubscribersCount { get; set; }
    public string PublicKey { get; set; } = string.Empty;
}

public class PushSubscriptionDto
{
    public string Endpoint { get; set; } = string.Empty;
    public PushKeys Keys { get; set; } = new();
}

public class PushKeys
{
    public string P256dh { get; set; } = string.Empty;
    public string Auth { get; set; } = string.Empty;
}
