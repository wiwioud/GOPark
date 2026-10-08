using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ParkingManagement.Data;
using ParkingManagement.Data.Models;
using ParkingManagement.UI.Services;
using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ParkingManagement.UI.ViewModels
{
    public class SettingsViewModel : ObservableObject
    {
        private readonly MainViewModel _mainViewModel;

        private ParkingRateSetting _settings = new();
        public ParkingRateSetting Settings
        {
            get => _settings;
            set => SetProperty(ref _settings, value);
        }

        private string? _validationError;
        public string? ValidationError
        {
            get => _validationError;
            set => SetProperty(ref _validationError, value);
        }

        public string ServiceStatusText => _mainViewModel.ServiceStatus;
        public string ServiceOnlineColor => _mainViewModel.IsServiceOnline ? "#10B981" : "#EF4444";

        public ICommand GoBackCommand { get; }
        public ICommand SaveCommand { get; }

        public SettingsViewModel(MainViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel;
            _mainViewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(MainViewModel.ServiceStatus) || e.PropertyName == nameof(MainViewModel.IsServiceOnline))
                {
                    OnPropertyChanged(nameof(ServiceStatusText));
                    OnPropertyChanged(nameof(ServiceOnlineColor));
                }
            };

            GoBackCommand = new RelayCommand(_ => _mainViewModel.NavigateTo(new DashboardViewModel(_mainViewModel, _mainViewModel.CurrentUser!)));
            SaveCommand = new RelayCommand(async _ => await SaveSettingsAsync());

            _ = LoadSettingsAsync();
        }

        private async Task LoadSettingsAsync()
        {
            using var db = App.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>().CreateDbContext();
            var set = await db.ParkingRateSettings.FirstOrDefaultAsync();
            if (set != null)
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() => Settings = set);
            }
        }

        private async Task SaveSettingsAsync()
        {
            ValidationError = null;

            if (Settings.HourlyRate < 0) { ValidationError = "Der Stundensatz darf nicht negativ sein."; return; }
            if (Settings.MinimumCharge < 0) { ValidationError = "Die Mindestgebuehr darf nicht negativ sein."; return; }
            if (Settings.GracePeriodMinutes < 0) { ValidationError = "Die Kulanzzeit darf nicht negativ sein."; return; }
            if (Settings.MaximumDailyRate <= 0) { ValidationError = "Das Tagesmaximum muss groesser als 0 sein."; return; }
            if (Settings.HourlyRate > Settings.MaximumDailyRate) { ValidationError = "Der Stundensatz darf das Tagesmaximum nicht ueberschreiten."; return; }

            try
            {
                using var db = App.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>().CreateDbContext();
                db.ParkingRateSettings.Update(Settings);
                await db.SaveChangesAsync();

                // Refresh fee calculator cache
                var calc = App.ServiceProvider.GetRequiredService<ParkingFeeCalculator>();
                await calc.RefreshSettingsAsync();

                _mainViewModel.ShowToast("Einstellungen wurden gespeichert.");
            }
            catch (Exception ex)
            {
                ValidationError = "Fehler beim Speichern: " + ex.Message;
            }
        }
    }
}
