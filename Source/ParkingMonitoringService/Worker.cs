using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ParkingManagement.Data;
using System;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace ParkingMonitoringService
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfiguration _configuration;

        public Worker(ILogger<Worker> logger, IServiceProvider serviceProvider, IConfiguration configuration)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _configuration = configuration;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("GOPark Worker running at: {time}", DateTimeOffset.Now);
            _ = Task.Run(() => RunNamedPipeServerAsync(stoppingToken), stoppingToken);

            int intervalSeconds = _configuration.GetValue<int>("MonitoringSettings:IntervalSeconds", 30);

            while (!stoppingToken.IsCancellationRequested)
            {
                await CheckDatabaseAsync(stoppingToken);
                await Task.Delay(intervalSeconds * 1000, stoppingToken);
            }
        }

        private async Task CheckDatabaseAsync(CancellationToken stoppingToken)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var now = DateTime.Now;

                // --- Reservation lifecycle updates ---
                // 1. Planned -> Active: reservation window has started
                var toActivate = await dbContext.Reservations
                    .Where(r => r.Status == "Planned"
                        && r.ReservationDate.Date == now.Date
                        && r.StartTime <= now.TimeOfDay
                        && r.EndTime > now.TimeOfDay)
                    .ToListAsync(stoppingToken);

                foreach (var r in toActivate)
                {
                    r.Status = "Active";
                    _logger.LogInformation("Reservation {Id} set to Active", r.Id);
                }

                // 2. Planned/Active -> Expired: window has passed
                var toExpire = await dbContext.Reservations
                    .Where(r => (r.Status == "Planned" || r.Status == "Active")
                        && (r.ReservationDate.Date < now.Date ||
                            (r.ReservationDate.Date == now.Date && r.EndTime < now.TimeOfDay)))
                    .ToListAsync(stoppingToken);

                foreach (var r in toExpire)
                {
                    r.Status = "Expired";
                    _logger.LogInformation("Reservation {Id} set to Expired", r.Id);
                }

                if (toActivate.Count > 0 || toExpire.Count > 0)
                    await dbContext.SaveChangesAsync(stoppingToken);

                // --- Check overdue sessions (> 24h) ---
                var overdueSessions = await dbContext.ParkingSessions
                    .Include(s => s.Vehicle)
                    .Where(s => s.Status == "Active" && EF.Functions.DateDiffHour(s.EntryTime, now) > 24)
                    .ToListAsync(stoppingToken);

                foreach (var session in overdueSessions)
                {
                    _logger.LogWarning("Overdue session: {LicensePlate}", session.Vehicle?.LicensePlate);
                    BroadcastNotification($"Fahrzeug {session.Vehicle?.LicensePlate} parkt seit mehr als 24 Stunden.");
                }

                // --- Upcoming reservations within 15 min ---
                var upcomingThreshold = now.TimeOfDay.Add(TimeSpan.FromMinutes(15));
                var upcoming = await dbContext.Reservations
                    .Include(r => r.Vehicle)
                    .Where(r => r.Status == "Planned"
                             && r.ReservationDate.Date == now.Date
                             && r.StartTime <= upcomingThreshold
                             && r.StartTime > now.TimeOfDay)
                    .ToListAsync(stoppingToken);

                foreach (var res in upcoming)
                    BroadcastNotification($"Reservierung fuer {res.Vehicle?.LicensePlate} beginnt um {res.StartTime:hh\\:mm}.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Worker CheckDatabaseAsync error.");
            }
        }

        private NamedPipeServerStream? _currentServerStream;
        private readonly object _lock = new object();
        private event Action<string>? OnNotificationGenerated;

        private async Task RunNamedPipeServerAsync(CancellationToken stoppingToken)
        {
            OnNotificationGenerated += SendMessageToClient;

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var serverStream = new NamedPipeServerStream("ParkingMonitoringPipe", PipeDirection.Out, 1,
                        PipeTransmissionMode.Message, PipeOptions.Asynchronous);

                    await serverStream.WaitForConnectionAsync(stoppingToken);
                    _logger.LogInformation("WPF client connected.");

                    lock (_lock) { _currentServerStream = serverStream; }

                    while (serverStream.IsConnected && !stoppingToken.IsCancellationRequested)
                        await Task.Delay(1000, stoppingToken);

                    lock (_lock) { _currentServerStream = null; serverStream.Dispose(); }
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Named Pipe Server error.");
                    await Task.Delay(2000, stoppingToken);
                }
            }
        }

        private void BroadcastNotification(string message) => OnNotificationGenerated?.Invoke(message);

        private void SendMessageToClient(string message)
        {
            lock (_lock)
            {
                if (_currentServerStream != null && _currentServerStream.IsConnected)
                {
                    try
                    {
                        var json = JsonSerializer.Serialize(new { Type = "Notification", Message = message });
                        var bytes = Encoding.UTF8.GetBytes(json + "\n");
                        _currentServerStream.Write(bytes, 0, bytes.Length);
                        _currentServerStream.Flush();
                    }
                    catch (Exception ex) { _logger.LogError(ex, "Failed to send pipe message."); }
                }
            }
        }
    }
}
