using Microsoft.EntityFrameworkCore;
using ParkingManagement.Data.Models;
using System.Linq;
using System.Threading.Tasks;

namespace ParkingManagement.Data.Database
{
    public static class DbInitializer
    {
        public static async Task InitializeAsync(AppDbContext context)
        {
            // Apply migrations automatically
            await context.Database.MigrateAsync();

            // Seed Users
            if (!await context.Users.AnyAsync())
            {
                context.Users.AddRange(
                    new User { Username = "admin", PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"), Role = "Administrator" },
                    new User { Username = "employee", PasswordHash = BCrypt.Net.BCrypt.HashPassword("Employee123!"), Role = "Employee" }
                );
                await context.SaveChangesAsync();
            }

            // Seed ParkingRateSettings
            if (!await context.ParkingRateSettings.AnyAsync())
            {
                context.ParkingRateSettings.Add(new ParkingRateSetting
                {
                    HourlyRate = 2.00m,
                    MinimumCharge = 1.00m,
                    GracePeriodMinutes = 15,
                    MaximumDailyRate = 20.00m
                });
                await context.SaveChangesAsync();
            }

            // Seed Parking Spaces
            if (!await context.ParkingSpaces.AnyAsync())
            {
                var spaces = new System.Collections.Generic.List<ParkingSpace>();
                string[] levels = { "A", "B", "C", "D", "E" };
                foreach (var level in levels)
                {
                    for (int i = 1; i <= 10; i++)
                    {
                        spaces.Add(new ParkingSpace
                        {
                            SpaceNumber = $"{level}{i:D2}",
                            Status = "Free"
                        });
                    }
                }
                context.ParkingSpaces.AddRange(spaces);
                await context.SaveChangesAsync();
            }
        }
    }
}
