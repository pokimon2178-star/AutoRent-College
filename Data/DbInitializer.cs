using AutoRent.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AutoRent.Data;

public static class DbInitializer
{
    public static async Task InitializeAsync(IServiceProvider serviceProvider)
    {
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var context = serviceProvider.GetRequiredService<ApplicationDbContext>();

        // Определяем роли системы
        string[] roleNames = { "Admin", "Manager", "Customer" };

        foreach (var roleName in roleNames)
        {
            var roleExist = await roleManager.RoleExistsAsync(roleName);
            if (!roleExist)
            {
                // Создаём роль, если она не существует
                await roleManager.CreateAsync(new IdentityRole(roleName));
            }
        }

        // Seed данных для брендов
        await SeedBrandsAsync(context);

        // Seed данных для категорий
        await SeedCategoriesAsync(context);

        // Seed данных для автомобилей
        await SeedCarsAsync(context);
    }

    private static async Task SeedBrandsAsync(ApplicationDbContext context)
    {
        // Проверяем, есть ли уже бренды
        if (await context.Brands.AnyAsync())
        {
            return; // Бренды уже существуют
        }

        var brands = new[]
        {
            new Brand { Name = "Toyota", Description = "Японский производитель автомобилей, известный надёжностью" },
            new Brand { Name = "BMW", Description = "Немецкий производитель премиум-автомобилей" },
            new Brand { Name = "Mercedes-Benz", Description = "Немецкий производитель люксовых автомобилей" },
            new Brand { Name = "Volkswagen", Description = "Немецкий производитель массовых автомобилей" },
            new Brand { Name = "Ford", Description = "Американский производитель автомобилей" },
            new Brand { Name = "Tesla", Description = "Американский производитель электромобилей" },
            new Brand { Name = "Hyundai", Description = "Южнокорейский производитель автомобилей" },
            new Brand { Name = "Audi", Description = "Немецкий производитель премиум-автомобилей" }
        };

        await context.Brands.AddRangeAsync(brands);
        await context.SaveChangesAsync();
    }

    private static async Task SeedCategoriesAsync(ApplicationDbContext context)
    {
        // Проверяем, есть ли уже категории
        if (await context.Categories.AnyAsync())
        {
            return; // Категории уже существуют
        }

        var categories = new[]
        {
            new Category { Name = "Седан", Description = "Комфортабельный городской автомобиль с багажником" },
            new Category { Name = "Внедорожник", Description = "Автомобиль повышенной проходимости для любых дорог" },
            new Category { Name = "Хэтчбек", Description = "Компактный городской автомобиль" },
            new Category { Name = "Универсал", Description = "Автомобиль с увеличенным багажным отделением" },
            new Category { Name = "Кабриолет", Description = "Автомобиль с откидным верхом" },
            new Category { Name = "Купе", Description = "Спортивный двухдверный автомобиль" },
            new Category { Name = "Минивэн", Description = "Семейный автомобиль с большим количеством мест" },
            new Category { Name = "Электромобиль", Description = "Экологичный автомобиль на электрической тяге" }
        };

        await context.Categories.AddRangeAsync(categories);
        await context.SaveChangesAsync();
    }

    private static async Task SeedCarsAsync(ApplicationDbContext context)
    {
        // Проверяем, есть ли уже автомобили
        if (await context.Cars.AnyAsync())
        {
            return; // Автомобили уже существуют
        }

        // Получаем ID брендов и категорий
        var toyotaId = (await context.Brands.FirstOrDefaultAsync(b => b.Name == "Toyota"))?.Id ?? 1;
        var bmwId = (await context.Brands.FirstOrDefaultAsync(b => b.Name == "BMW"))?.Id ?? 2;
        var mercedesId = (await context.Brands.FirstOrDefaultAsync(b => b.Name == "Mercedes-Benz"))?.Id ?? 3;
        var teslaId = (await context.Brands.FirstOrDefaultAsync(b => b.Name == "Tesla"))?.Id ?? 6;

        var sedanId = (await context.Categories.FirstOrDefaultAsync(c => c.Name == "Седан"))?.Id ?? 1;
        var suvId = (await context.Categories.FirstOrDefaultAsync(c => c.Name == "Внедорожник"))?.Id ?? 2;
        var electricId = (await context.Categories.FirstOrDefaultAsync(c => c.Name == "Электромобиль"))?.Id ?? 8;

        var cars = new[]
        {
            new Car
            {
                BrandId = toyotaId,
                CategoryId = sedanId,
                Model = "Camry",
                Year = 2023,
                Description = "Надёжный и комфортабельный седан среднего класса",
                ImageUrl = "/images/cars/toyota-camry.jpg",
                PricePerDay = 3500m,
                Transmission = "Automatic",
                FuelType = "Petrol",
                Seats = 5,
                IsAvailable = true
            },
            new Car
            {
                BrandId = bmwId,
                CategoryId = sedanId,
                Model = "3 Series",
                Year = 2023,
                Description = "Спортивный премиум-седан с отличной управляемостью",
                ImageUrl = "/images/cars/bmw-3-series.jpg",
                PricePerDay = 5500m,
                Transmission = "Automatic",
                FuelType = "Diesel",
                Seats = 5,
                IsAvailable = true
            },
            new Car
            {
                BrandId = mercedesId,
                CategoryId = suvId,
                Model = "GLE",
                Year = 2023,
                Description = "Роскошный внедорожник с высоким уровнем комфорта",
                ImageUrl = "/images/cars/mercedes-gle.jpg",
                PricePerDay = 7500m,
                Transmission = "Automatic",
                FuelType = "Diesel",
                Seats = 7,
                IsAvailable = true
            },
            new Car
            {
                BrandId = teslaId,
                CategoryId = electricId,
                Model = "Model 3",
                Year = 2023,
                Description = "Современный электромобиль с автопилотом",
                ImageUrl = "/images/cars/tesla-model-3.jpg",
                PricePerDay = 6000m,
                Transmission = "Automatic",
                FuelType = "Electric",
                Seats = 5,
                IsAvailable = true
            },
            new Car
            {
                BrandId = toyotaId,
                CategoryId = suvId,
                Model = "RAV4",
                Year = 2022,
                Description = "Популярный компактный кроссовер для города и путешествий",
                ImageUrl = "/images/cars/toyota-rav4.jpg",
                PricePerDay = 4000m,
                Transmission = "Automatic",
                FuelType = "Hybrid",
                Seats = 5,
                IsAvailable = true
            },
            new Car
            {
                BrandId = bmwId,
                CategoryId = suvId,
                Model = "X5",
                Year = 2023,
                Description = "Премиальный внедорожник с продвинутыми технологиями",
                ImageUrl = "/images/cars/bmw-x5.jpg",
                PricePerDay = 8000m,
                Transmission = "Automatic",
                FuelType = "Petrol",
                Seats = 7,
                IsAvailable = true
            }
        };

        await context.Cars.AddRangeAsync(cars);
        await context.SaveChangesAsync();
    }
}
