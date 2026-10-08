using ParkingManagement.Data;
using ParkingManagement.Data.Models;
using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;

namespace ParkingManagement.UI.ViewModels
{
    public class VehicleEditViewModel : ObservableObject
    {
        private readonly Window _window;
        private readonly MainViewModel _mainViewModel;
        private readonly Vehicle _vehicle;
        private readonly bool _isNew;

        public string LicensePlate { get => _vehicle.LicensePlate; set { _vehicle.LicensePlate = value; OnPropertyChanged(); } }
        public string Make { get => _vehicle.Make; set { _vehicle.Make = value; OnPropertyChanged(); } }
        public string Model { get => _vehicle.Model; set { _vehicle.Model = value; OnPropertyChanged(); } }
        public string Color { get => _vehicle.Color; set { _vehicle.Color = value; OnPropertyChanged(); } }
        public string OwnerName { get => _vehicle.OwnerName; set { _vehicle.OwnerName = value; OnPropertyChanged(); } }
        public string OwnerPhone { get => _vehicle.OwnerPhone; set { _vehicle.OwnerPhone = value; OnPropertyChanged(); } }
        public string Notes { get => _vehicle.Notes; set { _vehicle.Notes = value; OnPropertyChanged(); } }

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

        public VehicleEditViewModel(Window window, MainViewModel mainViewModel, Vehicle vehicle = null)
        {
            _window = window;
            _mainViewModel = mainViewModel;
            _isNew = vehicle == null;
            _vehicle = vehicle ?? new Vehicle();
            
            SaveCommand = new RelayCommand(async _ => await SaveAsync());
            CancelCommand = new RelayCommand(_ => _window.Close());
        }

        private async Task SaveAsync()
        {
            ErrorMessage = string.Empty;
            if (string.IsNullOrWhiteSpace(LicensePlate))
            {
                ErrorMessage = "Kennzeichen ist erforderlich.";
                return;
            }

            LicensePlate = LicensePlate.Trim().ToUpper();

            try
            {
                using var db = App.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>().CreateDbContext();
                
                // Check duplicate
                var exists = await db.Vehicles.AnyAsync(v => v.LicensePlate == LicensePlate && (_isNew || v.Id != _vehicle.Id));
                if (exists)
                {
                    ErrorMessage = "Ein Fahrzeug mit diesem Kennzeichen existiert bereits.";
                    return;
                }

                if (_isNew)
                {
                    db.Vehicles.Add(_vehicle);
                }
                else
                {
                    db.Vehicles.Update(_vehicle);
                }
                
                await db.SaveChangesAsync();
                WasSaved = true;
                _window.Close();
                _mainViewModel.ShowToast("Fahrzeug erfolgreich gespeichert.");
            }
            catch (Exception ex)
            {
                ErrorMessage = "Fehler beim Speichern: " + ex.Message;
            }
        }
    }
}
