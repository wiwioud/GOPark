using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ParkingManagement.Data;
using ParkingManagement.Data.Models;
using System;
using System.Threading.Tasks;

namespace ParkingManagement.UI.Services
{
    public class ParkingFeeCalculator
    {
        private readonly IServiceProvider _serviceProvider;
        private ParkingRateSetting? _cachedSettings;

        public ParkingFeeCalculator(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        // =========================================================
        // REFRESH SETTINGS
        // =========================================================

        public async Task RefreshSettingsAsync()
        {
            using var db = _serviceProvider
                .GetRequiredService<IDbContextFactory<AppDbContext>>()
                .CreateDbContext();

            _cachedSettings =
                await db.ParkingRateSettings.FirstOrDefaultAsync();
        }

        // =========================================================
        // CALCULATE PARKING FEE
        // =========================================================

        public async Task<decimal> CalculateFeeAsync(TimeSpan duration)
        {
            if (_cachedSettings == null)
            {
                await RefreshSettingsAsync();
            }

            var settings = _cachedSettings ?? new ParkingRateSetting
            {
                HourlyRate = 2.00m,
                MinimumCharge = 1.00m,
                GracePeriodMinutes = 15,
                MaximumDailyRate = 20.00m
            };

            // Invalid or zero duration should never produce a fee.
            if (duration <= TimeSpan.Zero)
            {
                return 0m;
            }

            // Parking is free during the configured grace period.
            if (duration.TotalMinutes <= settings.GracePeriodMinutes)
            {
                return 0m;
            }

            // =====================================================
            // FULL DAYS
            // =====================================================

            int fullDays =
                (int)Math.Floor(duration.TotalDays);

            decimal totalFee =
                fullDays * settings.MaximumDailyRate;

            // =====================================================
            // REMAINING HOURS
            // =====================================================

            decimal remainingHours =
                (decimal)(
                    duration.TotalHours -
                    (fullDays * 24));

            if (remainingHours > 0)
            {
                // Every started hour is charged as a full hour.
                decimal remainderFee =
                    Math.Ceiling(remainingHours) *
                    settings.HourlyRate;

                // The remainder of a day must never cost more
                // than the configured maximum daily rate.
                if (remainderFee > settings.MaximumDailyRate)
                {
                    remainderFee =
                        settings.MaximumDailyRate;
                }

                totalFee += remainderFee;
            }

            // =====================================================
            // MINIMUM CHARGE
            // =====================================================

            if (totalFee < settings.MinimumCharge)
            {
                totalFee =
                    settings.MinimumCharge;
            }

            return totalFee;
        }

        // =========================================================
        // FORMAT DURATION
        // =========================================================

        public string FormatDuration(TimeSpan duration)
        {
            if (duration.TotalMinutes < 60)
            {
                return $"{(int)duration.TotalMinutes} Min.";
            }

            if (duration.TotalHours < 24)
            {
                return
                    $"{(int)duration.TotalHours} Std. " +
                    $"{duration.Minutes} Min.";
            }

            return
                $"{(int)duration.TotalDays} Tg. " +
                $"{duration.Hours} Std.";
        }
    }
}