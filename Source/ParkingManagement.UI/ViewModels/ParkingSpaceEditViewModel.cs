using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ParkingManagement.Data;
using ParkingManagement.Data.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace ParkingManagement.UI.ViewModels
{
    public class ParkingSpaceEditViewModel : ObservableObject
    {
        private readonly Window _window;
        private readonly MainViewModel _mainViewModel;
        private readonly ParkingSpace? _existingSpace;

        public string Title => _existingSpace == null ? "Neuen Parkplatz anlegen" : "Parkplatz bearbeiten";
        public bool WasSaved { get; private set; } = false;

        // ComboBox data sources
        public List<string> TypeOptions { get; } = new() { "Standard", "Behindert", "Elektro", "Motor" };
        public List<string> StatusOptions { get; } = new() { "Frei", "Außer Betrieb" };

        private string _spaceNumber = string.Empty;
        public string SpaceNumber { get => _spaceNumber; set => SetProperty(ref _spaceNumber, value); }

        private string _zone = string.Empty;
        public string Zone { get => _zone; set => SetProperty(ref _zone, value); }

        private string _selectedType = "Standard";
        public string SelectedType { get => _selectedType; set => SetProperty(ref _selectedType, value); }

        private string _selectedDisplayStatus = "Frei";
        public string SelectedDisplayStatus { get => _selectedDisplayStatus; set => SetProperty(ref _selectedDisplayStatus, value); }

        private string _errorMessage = string.Empty;
        public string ErrorMessage { get => _errorMessage; set { SetProperty(ref _errorMessage, value); OnPropertyChanged(nameof(HasError)); } }
        public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        public ParkingSpaceEditViewModel(Window window, MainViewModel mainViewModel, ParkingSpace? existing)
        {
            _window = window;
            _mainViewModel = mainViewModel;
            _existingSpace = existing;

            if (existing != null)
            {
                SpaceNumber = existing.SpaceNumber;
                Zone = existing.Zone ?? string.Empty;
                // If Type is corrupt (e.g. "System.Window..."), fall back to Standard
                SelectedType = TypeOptions.Contains(existing.Type ?? "") ? existing.Type! : "Standard";
                SelectedDisplayStatus = existing.Status == "Out of Order" ? "Außer Betrieb" : "Frei";
            }

            SaveCommand = new RelayCommand(async _ => await SaveAsync());
            CancelCommand = new RelayCommand(_ => _window.Close());
        }

        private async Task SaveAsync()
        {
            ErrorMessage = string.Empty;
            if (string.IsNullOrWhiteSpace(SpaceNumber)) { ErrorMessage = "Platznummer ist erforderlich."; return; }

            string dbStatus = SelectedDisplayStatus == "Außer Betrieb" ? "Out of Order" : "Free";

            try
            {
                using var db = App.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>().CreateDbContext();

                if (_existingSpace == null)
                {
                    bool exists = await db.ParkingSpaces.AnyAsync(s => s.SpaceNumber == SpaceNumber);
                    if (exists) { ErrorMessage = "Ein Parkplatz mit dieser Nummer existiert bereits."; return; }

                    db.ParkingSpaces.Add(new ParkingSpace
                    {
                        SpaceNumber = SpaceNumber,
                        Zone = Zone,
                        Type = SelectedType,
                        Status = dbStatus
                    });
                }
                else
                {
                    var space = await db.ParkingSpaces.FindAsync(_existingSpace.Id);
                    if (space == null) { ErrorMessage = "Parkplatz nicht gefunden."; return; }
                    space.SpaceNumber = SpaceNumber;
                    space.Zone = Zone;
                    space.Type = SelectedType;
                    // Only allow changing to Free or Out of Order (Occupied/Reserved is controlled by sessions)
                    if (space.Status != "Occupied" && space.Status != "Reserved")
                        space.Status = dbStatus;
                }

                await db.SaveChangesAsync();
                WasSaved = true;
                _window.Close();
                _mainViewModel.ShowToast(_existingSpace == null ? "Parkplatz wurde angelegt." : "Parkplatz wurde gespeichert.");
            }
            catch (Exception ex)
            {
                ErrorMessage = "Fehler: " + ex.Message;
            }
        }
    }
}
