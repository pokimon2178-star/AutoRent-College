using System.ComponentModel.DataAnnotations;

namespace AutoRent.Models.Validation;

/// <summary>
/// Валидатор для проверки диапазона дат бронирования
/// </summary>
public class DateRangeAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        var booking = (validationContext.ObjectInstance as dynamic);

        if (booking == null)
        {
            return ValidationResult.Success;
        }

        DateTime startDate = booking.StartDate;
        DateTime endDate = booking.EndDate;

        // Проверка, что дата окончания больше даты начала
        if (endDate <= startDate)
        {
            return new ValidationResult("Дата окончания должна быть позже даты начала");
        }

        // Проверка, что дата начала не в прошлом (с учетом текущей даты)
        if (startDate.Date < DateTime.Today)
        {
            return new ValidationResult("Дата начала бронирования не может быть в прошлом");
        }

        return ValidationResult.Success;
    }
}
