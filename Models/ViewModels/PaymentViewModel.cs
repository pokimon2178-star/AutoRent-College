using System.ComponentModel.DataAnnotations;

namespace AutoRent.Models.ViewModels;

public class PaymentViewModel
{
    public int BookingId { get; set; }

    [DisplayFormat(DataFormatString = "{0:C2}")]
    public decimal Amount { get; set; }

    public string? CarInfo { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public int Days { get; set; }
}
