using AutoRent.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AutoRent.Data;

public class ApplicationDbContext : IdentityDbContext<IdentityUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Brand> Brands { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Car> Cars { get; set; }
    public DbSet<Booking> Bookings { get; set; }
    public DbSet<Payment> Payments { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Конфигурация связей Car -> Brand
        builder.Entity<Car>()
            .HasOne(c => c.Brand)
            .WithMany(b => b.Cars)
            .HasForeignKey(c => c.BrandId)
            .OnDelete(DeleteBehavior.Restrict);

        // Конфигурация связей Car -> Category
        builder.Entity<Car>()
            .HasOne(c => c.Category)
            .WithMany(cat => cat.Cars)
            .HasForeignKey(c => c.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        // Конфигурация связей Booking -> Car
        builder.Entity<Booking>()
            .HasOne(b => b.Car)
            .WithMany(c => c.Bookings)
            .HasForeignKey(b => b.CarId)
            .OnDelete(DeleteBehavior.Restrict);

        // Конфигурация связей Booking -> User (Identity)
        builder.Entity<Booking>()
            .HasOne(b => b.User)
            .WithMany()
            .HasForeignKey(b => b.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Конфигурация связей Payment -> Booking (один к одному)
        builder.Entity<Payment>()
            .HasOne(p => p.Booking)
            .WithOne(b => b.Payment)
            .HasForeignKey<Payment>(p => p.BookingId)
            .OnDelete(DeleteBehavior.Cascade);

        // Индексы для оптимизации запросов
        builder.Entity<Brand>()
            .HasIndex(b => b.Name);

        builder.Entity<Category>()
            .HasIndex(c => c.Name);

        builder.Entity<Car>()
            .HasIndex(c => c.IsAvailable);

        builder.Entity<Booking>()
            .HasIndex(b => b.UserId);

        builder.Entity<Booking>()
            .HasIndex(b => b.Status);

        builder.Entity<Booking>()
            .HasIndex(b => new { b.CarId, b.StartDate, b.EndDate });
    }
}
