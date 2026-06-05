namespace Sadovod.Models.Entities;

public class Order
{
    public int Id { get; set; }
    public int? UserId { get; set; }
    public DateTime OrderDate { get; set; }
    public int StatusId { get; set; }
    public decimal TotalAmount { get; set; }

    public User? User { get; set; }
    public OrderStatus? Status { get; set; }
    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
}