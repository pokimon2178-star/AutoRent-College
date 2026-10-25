using System.ComponentModel.DataAnnotations;
using AutoRent.Models.Validation;

namespace AutoRent.Models.ViewModels;

[DateRange]
public class CreateBookingViewModel
{
    public int CarId { get; set; }

    public string? CarName { get; set; }

    public decimal PricePerDay { get; set; }

    [Required(ErrorMessage = "Дата начала бронирования обязательна")]
    [DataType(DataType.Date)]
    public DateTime StartDate { get; set; }

    [Required(ErrorMessage = "Дата окончания бронирования обязательна")]
    [DataType(DataType.Date)]
    public DateTime EndDate { get; set; }

    public decimal CalculatedTotalPrice
    {
        get
        {
            var days = (EndDate - StartDate).Days;
            return days > 0 ? days * PricePerDay : 0;
        }
    }
}
