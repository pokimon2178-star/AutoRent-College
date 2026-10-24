using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AutoRent.Models;

public class Car
{
    public int Id { get; set; }

    [Required]
    public int BrandId { get; set; }

    [Required]
    public int CategoryId { get; set; }

    [Required(ErrorMessage = "Модель автомобиля обязательна")]
    [StringLength(100, ErrorMessage = "Модель не может превышать 100 символов")]
    public string Model { get; set; } = string.Empty;

    [Required(ErrorMessage = "Год выпуска обязателен")]
    [Range(1900, 2100, ErrorMessage = "Год выпуска должен быть между 1900 и 2100")]
    public int Year { get; set; }

    [StringLength(1000, ErrorMessage = "Описание не может превышать 1000 символов")]
    public string? Description { get; set; }

    [StringLength(500, ErrorMessage = "URL изображения не может превышать 500 символов")]
    public string? ImageUrl { get; set; }

    [Required(ErrorMessage = "Цена за день обязательна")]
    [Column(TypeName = "decimal(18,2)")]
    [Range(0.01, 999999.99, ErrorMessage = "Цена должна быть положительной и не превышать 999999.99")]
    public decimal PricePerDay { get; set; }

    [Required(ErrorMessage = "Тип трансмиссии обязателен")]
    [StringLength(50, ErrorMessage = "Тип трансмиссии не может превышать 50 символов")]
    public string Transmission { get; set; } = string.Empty; // Например: "Automatic", "Manual"

    [Required(ErrorMessage = "Тип топлива обязателен")]
    [StringLength(50, ErrorMessage = "Тип топлива не может превышать 50 символов")]
    public string FuelType { get; set; } = string.Empty; // Например: "Petrol", "Diesel", "Electric", "Hybrid"

    [Required(ErrorMessage = "Количество мест обязательно")]
    [Range(1, 20, ErrorMessage = "Количество мест должно быть между 1 и 20")]
    public int Seats { get; set; }

    public bool IsAvailable { get; set; } = true;

    // Навигационные свойства
    [ForeignKey("BrandId")]
    public Brand Brand { get; set; } = null!;

    [ForeignKey("CategoryId")]
    public Category Category { get; set; } = null!;

    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}
