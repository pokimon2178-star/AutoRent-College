using System.ComponentModel.DataAnnotations;

namespace AutoRent.Models.Validation;

/// <summary>
/// Валидатор для проверки диапазона дат бронирования
/// </summary>
public class DateRangeAttribute : ValidationAttribute
{
    private const int MaxRentalDays = 90;
    private const int MinRentalDays = 1;

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (validationContext.ObjectInstance is not AutoRent.Models.ViewModels.CreateBookingViewModel booking)
        {
            return ValidationResult.Success;
        }

        DateTime startDate = booking.StartDate.Date;
        DateTime endDate = booking.EndDate.Date;

        // Проверка, что дата окончания больше даты начала
        if (endDate <= startDate)
        {
            return new ValidationResult("Дата окончания должна быть позже даты начала");
        }

        // Проверка, что дата начала не в прошлом
        // Используем UtcNow для корректной работы с часовыми поясами
        if (startDate < DateTime.Today)
        {
            return new ValidationResult("Дата начала бронирования не может быть в прошлом");
        }

        // Проверка минимальной длительности аренды
        var rentalDays = (endDate - startDate).Days;
        if (rentalDays < MinRentalDays)
        {
            return new ValidationResult($"Минимальная длительность аренды — {MinRentalDays} день");
        }

        // Проверка максимальной длительности аренды
        if (rentalDays > MaxRentalDays)
        {
            return new ValidationResult($"Максимальная длительность аренды — {MaxRentalDays} дней");
        }

        return ValidationResult.Success;
    }
}
