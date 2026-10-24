using System.ComponentModel.DataAnnotations;

namespace AutoRent.Models;

public class Brand
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Название марки обязательно")]
    [StringLength(100, ErrorMessage = "Название марки не может превышать 100 символов")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Описание не может превышать 500 символов")]
    public string? Description { get; set; }

    // Навигационное свойство
    public ICollection<Car> Cars { get; set; } = new List<Car>();
}
