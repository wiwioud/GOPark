using ParkingManagement.Data;
using ParkingManagement.Data.Models;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;

namespace ParkingManagement.UI.ViewModels
{
    public class ReservationEditViewModel : ObservableObject
    {
        private readonly Window _window;
        private readonly MainViewModel _mainViewModel;
        private readonly User? _currentUser;

        public ObservableCollection<Vehicle> Vehicles { get; set; } = new();
        public ObservableCollection<ParkingSpace> ParkingSpaces { get; set; } = new();

        private Vehicle? _selectedVehicle;
        public Vehicle? SelectedVehicle { get => _selectedVehicle; set => SetProperty(ref _selectedVehicle, value); }

        private ParkingSpace? _selectedSpace;
        public ParkingSpace? SelectedSpace { get => _selectedSpace; set => SetProperty(ref _selectedSpace, value); }

        // Single date for reservation (same-day reservations)
        public DateTime ReservationDate { get; set; } = DateTime.Today;
        public string StartTime { get; set; } = DateTime.Now.AddHours(1).ToString("HH:mm");
        public string EndTime { get; set; } = DateTime.Now.AddHours(3).ToString("HH:mm");
        public string Notes { get; set; } = string.Empty;

        private string _errorMessage = string.Empty;
        public string ErrorMessage
        {
            get => _errorMessage;
            set { SetProperty(ref _errorMessage, value); OnPropertyChanged(nameof(HasError)); }
        }
        public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }
        public bool WasSaved { get; private set; } = false;

        public ReservationEditViewModel(Window window, MainViewModel mainViewModel)
        {
            _window = window;
            _mainViewModel = mainViewModel;
            _currentUser = mainViewModel.CurrentUser;

            SaveCommand = new RelayCommand(async _ => await SaveAsync());
            CancelCommand = new RelayCommand(_ => _window.Close());

            _ = LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            using var db = App.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>().CreateDbContext();
            var vs = await db.Vehicles.OrderBy(v => v.LicensePlate).ToListAsync();
            var ps = await db.ParkingSpaces
                .Where(p => p.Status != "Out of Order")
                .OrderBy(p => p.SpaceNumber)
                .ToListAsync();

            Application.Current.Dispatcher.Invoke(() =>
            {
                Vehicles = new ObservableCollection<Vehicle>(vs);
                ParkingSpaces = new ObservableCollection<ParkingSpace>(ps);
                OnPropertyChanged(nameof(Vehicles));
                OnPropertyChanged(nameof(ParkingSpaces));
            });
        }

        private async Task SaveAsync()
        {
            ErrorMessage = string.Empty;
            if (SelectedVehicle == null) { ErrorMessage = "Bitte Fahrzeug auswaehlen."; return; }
            if (SelectedSpace == null) { ErrorMessage = "Bitte Parkplatz auswaehlen."; return; }
            if (!TimeSpan.TryParse(StartTime, out var sTime)) { ErrorMessage = "Ungueltige Startzeit (HH:mm)."; return; }
            if (!TimeSpan.TryParse(EndTime, out var eTime)) { ErrorMessage = "Ungueltige Endzeit (HH:mm)."; return; }
            if (eTime <= sTime) { ErrorMessage = "Endzeit muss nach der Startzeit liegen."; return; }

            var resDate = ReservationDate.Date;
            var startDateTime = resDate.Add(sTime);
            if (startDateTime < DateTime.Now.AddMinutes(-30))
            {
                ErrorMessage = "Die Reservierung kann nicht in der Vergangenheit liegen.";
                return;
            }

            try
            {
                using var db = App.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>().CreateDbContext();

                // Check overlap
                var overlap = await db.Reservations.AnyAsync(r =>
                    r.ParkingSpaceId == SelectedSpace.Id &&
                    r.Status != "Cancelled" && r.Status != "Completed" && r.Status != "Expired" &&
                    r.ReservationDate == resDate &&
                    r.StartTime < eTime &&
                    r.EndTime > sTime);

                if (overlap)
                {
                    ErrorMessage = "Der Parkplatz ist im gewaehlten Zeitraum bereits reserviert.";
                    return;
                }

                var res = new Reservation
                {
                    VehicleId = SelectedVehicle.Id,
                    ParkingSpaceId = SelectedSpace.Id,
                    ReservationDate = resDate,
                    StartTime = sTime,
                    EndTime = eTime,
                    Status = "Planned",
                    Notes = Notes ?? string.Empty
                };

                db.Reservations.Add(res);

                db.AuditLogs.Add(new AuditLog
                {
                    Timestamp = DateTime.Now,
                    UserId = _currentUser?.Id,
                    Action = "Create",
                    EntityType = "Reservation",
                    Details = $"Reservierung erstellt: {SelectedVehicle.LicensePlate} auf {SelectedSpace.SpaceNumber} am {resDate:dd.MM.yyyy} {sTime:hh\\:mm}-{eTime:hh\\:mm}"
                });

                await db.SaveChangesAsync();

                WasSaved = true;
                _window.Close();
                _mainViewModel.ShowToast("Reservierung wurde erfolgreich erstellt.");
            }
            catch (Exception ex)
            {
                ErrorMessage = "Fehler beim Speichern: " + (ex.InnerException?.Message ?? ex.Message);
            }
        }
    }
}
