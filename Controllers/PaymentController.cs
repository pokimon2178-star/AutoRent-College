using AutoRent.Data;
using AutoRent.Models;
using AutoRent.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AutoRent.Controllers;

[Authorize]
public class PaymentController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<PaymentController> _logger;

    public PaymentController(
        ApplicationDbContext context,
        ILogger<PaymentController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET: Payment/Pay
    [HttpGet]
    public async Task<IActionResult> Pay(int? bookingId)
    {
        try
        {
            if (bookingId == null)
                return BadRequest("Booking ID is required");

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogError("Could not determine current user ID");
                return Unauthorized();
            }

            var booking = await _context.Bookings
                .Include(b => b.Car)
                    .ThenInclude(c => c.Brand)
                .Include(b => b.Payment)
                .FirstOrDefaultAsync(b => b.Id == bookingId);

            if (booking == null)
            {
                _logger.LogWarning("Booking {BookingId} not found", bookingId);
                return NotFound("Бронирование не найдено");
            }

            // Check authorization: user can only pay for own bookings
            if (booking.UserId != userId)
            {
                _logger.LogWarning(
                    "Unauthorized payment attempt by user {UserId} on booking {BookingId}",
                    userId, bookingId);
                return Forbid();
            }

            if (booking.Status is "Cancelled" or "Completed")
                return BadRequest("Нельзя оплатить отменённое или завершённое бронирование.");

            // Check if booking already has a payment
            if (booking.Payment != null && booking.Payment.Status == "Completed")
            {
                _logger.LogWarning(
                    "Payment already processed for booking {BookingId}",
                    bookingId);
                return RedirectToAction("Confirm", new { paymentId = booking.Payment.Id });
            }

            var model = new PaymentViewModel
            {
                BookingId = bookingId.Value,
                Amount = booking.TotalPrice,
                CarInfo = $"{booking.Car.Brand.Name} {booking.Car.Model}",
                StartDate = booking.StartDate,
                EndDate = booking.EndDate,
                Days = (booking.EndDate - booking.StartDate).Days
            };

            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading payment page for booking {BookingId}", bookingId);
            return StatusCode(500, "Ошибка при загрузке страницы оплаты");
        }
    }

    // POST: Payment/Process
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Process(int bookingId)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogError("Could not determine current user ID");
                return Unauthorized();
            }

            await using var transaction = await _context.Database.BeginTransactionAsync(
                System.Data.IsolationLevel.Serializable);
            var booking = await _context.Bookings
                .Include(b => b.Payment)
                .FirstOrDefaultAsync(b => b.Id == bookingId);

            if (booking == null)
            {
                _logger.LogWarning("Booking {BookingId} not found for payment", bookingId);
                return NotFound("Бронирование не найдено");
            }

            // Check authorization
            if (booking.UserId != userId)
            {
                _logger.LogWarning(
                    "Unauthorized payment processing by user {UserId} on booking {BookingId}",
                    userId, bookingId);
                return Forbid();
            }

            // Check if payment already exists
            if (booking.Payment != null && booking.Payment.Status == "Completed")
            {
                _logger.LogWarning(
                    "Payment already processed for booking {BookingId}",
                    bookingId);
                return RedirectToAction("Confirm", new { paymentId = booking.Payment.Id });
            }

            if (booking.Status is "Cancelled" or "Completed")
                return BadRequest("Нельзя оплатить отменённое или завершённое бронирование.");

            // Simulate payment processing
            // In a real application, this would integrate with a payment provider (Stripe, PayPal, etc.)
            // For now, we'll simulate a successful payment

            var payment = new Payment
            {
                BookingId = bookingId,
                Amount = booking.TotalPrice,
                Status = "Completed", // In production: first set to "Pending", then update via webhook
                CreatedAt = DateTime.UtcNow,
                TransactionReference = Guid.NewGuid().ToString()
            };

            // Remove old pending payment if exists
            if (booking.Payment != null && booking.Payment.Status == "Pending")
            {
                _context.Payments.Remove(booking.Payment);
            }

            booking.Status = "Confirmed";
            booking.Payment = payment;

            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation(
                "Payment {PaymentId} processed successfully for booking {BookingId} by user {UserId}",
                payment.Id, bookingId, userId);

            return RedirectToAction("Confirm", new { paymentId = payment.Id });
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Database error during payment processing");
            return StatusCode(500, "Ошибка при обработке платежа");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during payment processing for booking {BookingId}", bookingId);
            return StatusCode(500, "Неожиданная ошибка при обработке платежа");
        }
    }

    // GET: Payment/Confirm/5
    [HttpGet]
    public async Task<IActionResult> Confirm(int? paymentId)
    {
        try
        {
            if (paymentId == null)
                return BadRequest("Payment ID is required");

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogError("Could not determine current user ID");
                return Unauthorized();
            }

            var payment = await _context.Payments
                .Include(p => p.Booking)
                    .ThenInclude(b => b.Car)
                        .ThenInclude(c => c.Brand)
                .FirstOrDefaultAsync(p => p.Id == paymentId);

            if (payment == null)
            {
                _logger.LogWarning("Payment {PaymentId} not found", paymentId);
                return NotFound("Платеж не найден");
            }

            // Check authorization
            if (payment.Booking.UserId != userId)
            {
                _logger.LogWarning(
                    "Unauthorized access to payment {PaymentId} by user {UserId}",
                    paymentId, userId);
                return Forbid();
            }

            return View(payment);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading payment confirmation for ID {PaymentId}", paymentId);
            return StatusCode(500, "Ошибка при загрузке подтверждения платежа");
        }
    }

    // GET: Payment/History
    [HttpGet]
    public async Task<IActionResult> History()
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogError("Could not determine current user ID");
                return Unauthorized();
            }

            var payments = await _context.Payments
                .Include(p => p.Booking)
                    .ThenInclude(b => b.Car)
                        .ThenInclude(c => c.Brand)
                .Where(p => p.Booking.UserId == userId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            _logger.LogInformation(
                "Retrieved {PaymentCount} payments for user {UserId}",
                payments.Count, userId);

            return View(payments);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving payment history");
            return StatusCode(500, "Ошибка при получении истории платежей");
        }
    }
}
