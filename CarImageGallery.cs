using AutoRent.Models;

namespace AutoRent;

public static class CarImageGallery
{
    private const string Fallback = "/images/cars/car-fallback.svg";

    public static IReadOnlyList<string> For(Car car)
    {
        var brand = car.Brand?.Name?.Trim() ?? "";
        var model = car.Model?.Trim() ?? "";

        var prefix = (brand, model) switch
        {
            ("Toyota", "Camry") => "toyota-camry",
            ("BMW", "3 Series") => "bmw-3-series",
            ("Mercedes-Benz", "GLE") => "mercedes-gle",
            ("Tesla", "Model 3") => "tesla-model-3",
            ("Toyota", "RAV4") => "toyota-rav4",
            ("BMW", "X5") => "bmw-x5",
            _ => null
        };

        if (prefix is not null)
        {
            return Enumerable.Range(1, 6)
                .Select(i => $"/images/cars/{prefix}-{i}.jpg")
                .ToArray();
        }

        if (!string.IsNullOrWhiteSpace(car.ImageUrl))
            return new[] { car.ImageUrl };

        return new[] { Fallback };
    }
}
