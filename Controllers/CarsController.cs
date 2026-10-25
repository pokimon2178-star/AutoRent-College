using AutoRent.Data;
using AutoRent.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace AutoRent.Controllers;

public class CarsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<CarsController> _logger;

    public CarsController(ApplicationDbContext context, ILogger<CarsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET: Cars
    public async Task<IActionResult> Index(
        string? search,
        int? brandId,
        int? categoryId,
        decimal? minPrice,
        decimal? maxPrice,
        string? transmission,
        string? sortBy)
    {
        // Валидация параметров
        if (minPrice.HasValue && minPrice.Value < 0)
            minPrice = 0;

        if (maxPrice.HasValue && maxPrice.Value < 0)
            maxPrice = null;

        // Базовый запрос с включением связанных данных
        var query = _context.Cars
            .Include(c => c.Brand)
            .Include(c => c.Category)
            .Where(c => c.IsAvailable)
            .AsQueryable();

        // Применение фильтров
        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchPattern = $"%{EscapeLikePattern(search.Trim())}%";
            query = query.Where(c =>
                EF.Functions.Like(c.Model, searchPattern, "\\") ||
                EF.Functions.Like(c.Brand.Name, searchPattern, "\\"));
        }

        if (brandId.HasValue)
        {
            query = query.Where(c => c.BrandId == brandId.Value);
        }

        if (categoryId.HasValue)
        {
            query = query.Where(c => c.CategoryId == categoryId.Value);
        }

        if (minPrice.HasValue)
        {
            query = query.Where(c => c.PricePerDay >= minPrice.Value);
        }

        if (maxPrice.HasValue)
        {
            query = query.Where(c => c.PricePerDay <= maxPrice.Value);
        }

        if (!string.IsNullOrWhiteSpace(transmission))
        {
            query = query.Where(c => c.Transmission == transmission);
        }

        // Сортировка
        query = sortBy switch
        {
            "price_asc" => query.OrderBy(c => (double)c.PricePerDay),
            "price_desc" => query.OrderByDescending(c => (double)c.PricePerDay),
            "name_asc" => query.OrderBy(c => c.Brand.Name).ThenBy(c => c.Model),
            "name_desc" => query.OrderByDescending(c => c.Brand.Name).ThenByDescending(c => c.Model),
            _ => query.OrderBy(c => c.Brand.Name).ThenBy(c => c.Model)
        };

        var cars = await query.ToListAsync();

        // Загрузка данных для фильтров
        ViewBag.Brands = await _context.Brands.OrderBy(b => b.Name).ToListAsync();
        ViewBag.Categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();
        ViewBag.Transmissions = await _context.Cars
            .Select(c => c.Transmission)
            .Distinct()
            .OrderBy(t => t)
            .ToListAsync();

        // Сохранение текущих значений фильтров для отображения в форме
        ViewBag.CurrentSearch = search;
        ViewBag.CurrentBrandId = brandId;
        ViewBag.CurrentCategoryId = categoryId;
        ViewBag.CurrentMinPrice = minPrice;
        ViewBag.CurrentMaxPrice = maxPrice;
        ViewBag.CurrentTransmission = transmission;
        ViewBag.CurrentSortBy = sortBy;

        return View(cars);
    }

    // GET: Cars/FilterPartial - для AJAX-запросов
    public async Task<IActionResult> FilterPartial(
        string? search,
        int? brandId,
        int? categoryId,
        decimal? minPrice,
        decimal? maxPrice,
        string? transmission,
        string? sortBy)
    {
        // Валидация параметров
        if (minPrice.HasValue && minPrice.Value < 0)
            minPrice = 0;

        if (maxPrice.HasValue && maxPrice.Value < 0)
            maxPrice = null;

        // Базовый запрос с включением связанных данных
        var query = _context.Cars
            .Include(c => c.Brand)
            .Include(c => c.Category)
            .Where(c => c.IsAvailable)
            .AsQueryable();

        // Применение фильтров
        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchPattern = $"%{EscapeLikePattern(search.Trim())}%";
            query = query.Where(c =>
                EF.Functions.Like(c.Model, searchPattern, "\\") ||
                EF.Functions.Like(c.Brand.Name, searchPattern, "\\"));
        }

        if (brandId.HasValue)
        {
            query = query.Where(c => c.BrandId == brandId.Value);
        }

        if (categoryId.HasValue)
        {
            query = query.Where(c => c.CategoryId == categoryId.Value);
        }

        if (minPrice.HasValue)
        {
            query = query.Where(c => c.PricePerDay >= minPrice.Value);
        }

        if (maxPrice.HasValue)
        {
            query = query.Where(c => c.PricePerDay <= maxPrice.Value);
        }

        if (!string.IsNullOrWhiteSpace(transmission))
        {
            query = query.Where(c => c.Transmission == transmission);
        }

        // Сортировка
        query = sortBy switch
        {
            "price_asc" => query.OrderBy(c => (double)c.PricePerDay),
            "price_desc" => query.OrderByDescending(c => (double)c.PricePerDay),
            "name_asc" => query.OrderBy(c => c.Brand.Name).ThenBy(c => c.Model),
            "name_desc" => query.OrderByDescending(c => c.Brand.Name).ThenByDescending(c => c.Model),
            _ => query.OrderBy(c => c.Brand.Name).ThenBy(c => c.Model)
        };

        var cars = await query.ToListAsync();

        return PartialView("_CarsListPartial", cars);
    }

    // GET: Cars/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var car = await _context.Cars
            .Include(c => c.Brand)
            .Include(c => c.Category)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (car == null)
        {
            return NotFound();
        }

        return View(car);
    }

    private static string EscapeLikePattern(string value) =>
        value.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
}
