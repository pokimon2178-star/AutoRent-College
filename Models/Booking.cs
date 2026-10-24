using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AutoRent.Models.Validation;
using Microsoft.AspNetCore.Identity;

namespace AutoRent.Models;

[DateRange]
public class Booking
{
    public int Id { get; set; }

    [Required]
    public int CarId { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    [Required(ErrorMessage = "Дата начала бронирования обязательна")]
    [DataType(DataType.Date)]
    public DateTime StartDate { get; set; }

    [Required(ErrorMessage = "Дата окончания бронирования обязательна")]
    [DataType(DataType.Date)]
    public DateTime EndDate { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    [Range(0.01, 999999.99, ErrorMessage = "Общая стоимость должна быть положительной")]
    public decimal TotalPrice { get; set; }

    [Required(ErrorMessage = "Статус бронирования обязателен")]
    [StringLength(50, ErrorMessage = "Статус не может превышать 50 символов")]
    public string Status { get; set; } = "Pending"; // Pending, Confirmed, Cancelled, Completed

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Навигационные свойства
    [ForeignKey("CarId")]
    public Car Car { get; set; } = null!;

    [ForeignKey("UserId")]
    public IdentityUser User { get; set; } = null!;

    public Payment? Payment { get; set; }
}
