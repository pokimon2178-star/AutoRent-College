using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AutoRent.Models;

public class Payment
{
    public int Id { get; set; }

    [Required]
    public int BookingId { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    [Range(0.01, 999999.99, ErrorMessage = "Сумма платежа должна быть положительной")]
    public decimal Amount { get; set; }

    [Required(ErrorMessage = "Статус платежа обязателен")]
    [StringLength(50, ErrorMessage = "Статус не может превышать 50 символов")]
    public string Status { get; set; } = "Pending"; // Pending, Completed, Failed

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [StringLength(200, ErrorMessage = "Ссылка на транзакцию не может превышать 200 символов")]
    public string? TransactionReference { get; set; }

    // Навигационное свойство
    [ForeignKey("BookingId")]
    public Booking Booking { get; set; } = null!;
}
