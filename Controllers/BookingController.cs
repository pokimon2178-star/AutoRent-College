using AutoRent.Data;
using AutoRent.Models;
using AutoRent.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AutoRent.Controllers;

[Authorize]
public class BookingController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<BookingController> _logger;

    public BookingController(
        ApplicationDbContext context,
        ILogger<BookingController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET: Booking/Create
    [HttpGet]
    public async Task<IActionResult> Create(int carId)
    {
        try
        {
            var car = await _context.Cars
                .Include(c => c.Brand)
                .Include(c => c.Category)
                .FirstOrDefaultAsync(c => c.Id == carId);

            if (car == null)
            {
                _logger.LogWarning("Car with ID {CarId} not found", carId);
                return NotFound("Автомобиль не найден");
            }

            if (!car.IsAvailable)
            {
                _logger.LogWarning("Car with ID {CarId} is not available", carId);
                return BadRequest("Автомобиль недоступен для аренды");
            }

            var model = new CreateBookingViewModel
            {
                CarId = carId,
                CarName = $"{car.Brand.Name} {car.Model}",
                PricePerDay = car.PricePerDay,
                StartDate = DateTime.Today.AddDays(1),
                EndDate = DateTime.Today.AddDays(2)
            };

            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading booking create form for car {CarId}", carId);
            return StatusCode(500, "Ошибка при загрузке формы бронирования");
        }
    }

    // POST: Booking/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateBookingViewModel model)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid model state for booking creation");
                var carForForm = await _context.Cars.FindAsync(model.CarId);
                if (carForForm != null)
                {
                    model.CarName = $"{carForForm.Brand?.Name} {carForForm.Model}";
                    model.PricePerDay = carForForm.PricePerDay;
                }
                return View(model);
            }

            // Get current user ID
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogError("Could not determine current user ID");
                return Unauthorized("Не удалось определить текущего пользователя");
            }

            // Verify car exists and is available
            var car = await _context.Cars.FirstOrDefaultAsync(c => c.Id == model.CarId);
            if (car == null)
            {
                _logger.LogWarning("Car with ID {CarId} not found during booking", model.CarId);
                ModelState.AddModelError("CarId", "Автомобиль не найден");
                return View(model);
            }

            if (!car.IsAvailable)
            {
                _logger.LogWarning("Car {CarId} is not available during booking", model.CarId);
                ModelState.AddModelError("CarId", "Автомобиль недоступен для аренды");
                return View(model);
            }

            await using var transaction = await _context.Database.BeginTransactionAsync(
                System.Data.IsolationLevel.Serializable);
            var hasConflict = await _context.Bookings.AnyAsync(b =>
                b.CarId == model.CarId &&
                b.Status != "Cancelled" &&
                b.StartDate < model.EndDate &&
                b.EndDate > model.StartDate);

            if (hasConflict)
            {
                _logger.LogWarning(
                    "Booking conflict detected for car {CarId} from {StartDate} to {EndDate}",
                    model.CarId, model.StartDate, model.EndDate);
                ModelState.AddModelError("DateRange",
                    "Автомобиль уже забронирован на эти даты. Пожалуйста, выберите другие даты.");
                return View(model);
            }

            // Calculate total price
            var days = (model.EndDate - model.StartDate).Days;
            var totalPrice = days * car.PricePerDay;

            // Create booking
            var booking = new Booking
            {
                CarId = model.CarId,
                UserId = userId,
                StartDate = model.StartDate,
                EndDate = model.EndDate,
                TotalPrice = totalPrice,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };

            await _context.Bookings.AddAsync(booking);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation(
                "Booking {BookingId} created for user {UserId} on car {CarId}",
                booking.Id, userId, model.CarId);

            // Redirect to payment
            return RedirectToAction("Pay", "Payment", new { bookingId = booking.Id });
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Database error during booking creation");
            ModelState.AddModelError("", "Ошибка при сохранении бронирования. Попробуйте еще раз.");
            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during booking creation");
            return StatusCode(500, "Неожиданная ошибка при создании бронирования");
        }
    }

    // GET: Booking/MyBookings
    [HttpGet]
    public async Task<IActionResult> MyBookings()
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogError("Could not determine current user ID");
                return Unauthorized();
            }

            var bookings = await _context.Bookings
                .Include(b => b.Car)
                    .ThenInclude(c => c.Brand)
                .Include(b => b.Car)
                    .ThenInclude(c => c.Category)
                .Include(b => b.Payment)
                .Where(b => b.UserId == userId)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();

            _logger.LogInformation("Retrieved {BookingCount} bookings for user {UserId}",
                bookings.Count, userId);

            return View(bookings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user bookings");
            return StatusCode(500, "Ошибка при получении бронирований");
        }
    }

    // GET: Booking/Details/5
    [HttpGet]
    public async Task<IActionResult> Details(int? id)
    {
        try
        {
            if (id == null)
                return NotFound();

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogError("Could not determine current user ID");
                return Unauthorized();
            }

            var booking = await _context.Bookings
                .Include(b => b.Car)
                    .ThenInclude(c => c.Brand)
                .Include(b => b.Car)
                    .ThenInclude(c => c.Category)
                .Include(b => b.Payment)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (booking == null)
            {
                _logger.LogWarning("Booking {BookingId} not found", id);
                return NotFound();
            }

            // Check authorization: user can only view own bookings
            if (booking.UserId != userId)
            {
                _logger.LogWarning(
                    "Unauthorized access attempt by user {UserId} to booking {BookingId}",
                    userId, id);
                return Forbid();
            }

            return View(booking);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving booking details for ID {BookingId}", id);
            return StatusCode(500, "Ошибка при получении деталей бронирования");
        }
    }

    // POST: Booking/Cancel/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogError("Could not determine current user ID");
                return Unauthorized();
            }

            var booking = await _context.Bookings.FindAsync(id);

            if (booking == null)
            {
                _logger.LogWarning("Booking {BookingId} not found for cancellation", id);
                return NotFound();
            }

            // Check authorization
            if (booking.UserId != userId)
            {
                _logger.LogWarning(
                    "Unauthorized cancellation attempt by user {UserId} on booking {BookingId}",
                    userId, id);
                return Forbid();
            }

            // Check if cancellation is allowed
            if (booking.Status == "Cancelled")
            {
                _logger.LogWarning("Booking {BookingId} is already cancelled", id);
                ModelState.AddModelError("", "Бронирование уже отменено");
                return RedirectToAction(nameof(Details), new { id });
            }

            if (booking.Status == "Completed")
            {
                _logger.LogWarning("Cannot cancel completed booking {BookingId}", id);
                ModelState.AddModelError("", "Нельзя отменить завершенное бронирование");
                return RedirectToAction(nameof(Details), new { id });
            }

            if (booking.StartDate <= DateTime.Today)
            {
                _logger.LogWarning("Cannot cancel booking {BookingId} that has already started", id);
                ModelState.AddModelError("", "Нельзя отменить бронирование, которое уже началось");
                return RedirectToAction(nameof(Details), new { id });
            }

            booking.Status = "Cancelled";
            _context.Bookings.Update(booking);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Booking {BookingId} cancelled by user {UserId}",
                id, userId);

            return RedirectToAction(nameof(MyBookings));
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Database error during booking cancellation");
            return StatusCode(500, "Ошибка при отмене бронирования");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during booking cancellation for ID {BookingId}", id);
            return StatusCode(500, "Неожиданная ошибка при отмене бронирования");
        }
    }
}
