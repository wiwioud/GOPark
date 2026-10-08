using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ParkingManagement.Data;
using ParkingManagement.Data.Models;
using ParkingManagement.UI.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ParkingManagement.UI.ViewModels
{
    public class VehicleEntryViewModel : ObservableObject
    {
        private readonly MainViewModel _mainViewModel;
        private readonly ParkingStatusService _parkingStatusService;

        public User CurrentUser { get; }

        // =========================================================
        // VEHICLE DATA
        // =========================================================

        private string _licensePlate = string.Empty;

        public string LicensePlate
        {
            get => _licensePlate;
            set => SetProperty(ref _licensePlate, value);
        }

        private string _vehicleType = string.Empty;

        public string VehicleType
        {
            get => _vehicleType;
            set => SetProperty(ref _vehicleType, value);
        }

        // =========================================================
        // FREE PARKING SPACES
        // =========================================================

        private ObservableCollection<ParkingSpace> _freeSpaces = new();

        public ObservableCollection<ParkingSpace> FreeSpaces
        {
            get => _freeSpaces;
            set => SetProperty(ref _freeSpaces, value);
        }

        private ParkingSpace? _selectedSpace;

        public ParkingSpace? SelectedSpace
        {
            get => _selectedSpace;
            set => SetProperty(ref _selectedSpace, value);
        }

        // =========================================================
        // COMMANDS
        // =========================================================

        public ICommand GoBackCommand { get; }
        public ICommand RegisterEntryCommand { get; }

        // =========================================================
        // CONSTRUCTOR
        // =========================================================

        public VehicleEntryViewModel(
            MainViewModel mainViewModel,
            User user)
        {
            _mainViewModel = mainViewModel;
            CurrentUser = user;

            _parkingStatusService = App.ServiceProvider
                .GetRequiredService<ParkingStatusService>();

            GoBackCommand = new RelayCommand(_ =>
                _mainViewModel.NavigateTo(
                    new DashboardViewModel(
                        _mainViewModel,
                        CurrentUser)));

            RegisterEntryCommand =
                new RelayCommand(async _ => await RegisterEntryAsync());

            _ = LoadFreeSpacesAsync();
        }

        // =========================================================
        // LOAD FREE PARKING SPACES
        // =========================================================

        private async Task LoadFreeSpacesAsync()
        {
            try
            {
                // Synchronize current parking-space status first.
                // This ensures active reservations and sessions
                // are reflected before showing available spaces.
                await _parkingStatusService.SyncAllSpacesAsync();

                using var db = App.ServiceProvider
                    .GetRequiredService<IDbContextFactory<AppDbContext>>()
                    .CreateDbContext();

                var spaces = await db.ParkingSpaces
                    .Where(s => s.Status == "Free")
                    .OrderBy(s => s.SpaceNumber)
                    .ToListAsync();

                FreeSpaces =
                    new ObservableCollection<ParkingSpace>(spaces);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    "LoadFreeSpacesAsync error: " + ex);

                _mainViewModel.ShowToast(
                    "Die verfügbaren Parkplätze konnten nicht geladen werden.");
            }
        }

        // =========================================================
        // REGISTER ENTRY
        // =========================================================

        private async Task RegisterEntryAsync()
        {
            // -----------------------------------------------------
            // Basic validation
            // -----------------------------------------------------

            if (string.IsNullOrWhiteSpace(LicensePlate))
            {
                _mainViewModel.ShowToast(
                    "Bitte geben Sie ein Kennzeichen ein.");

                return;
            }

            if (SelectedSpace == null)
            {
                _mainViewModel.ShowToast(
                    "Bitte wählen Sie einen Parkplatz aus.");

                return;
            }

            // Normalize license plate so that values such as
            // "abc123", " ABC123 " and "ABC123"
            // are treated consistently.
            var normalizedPlate =
                LicensePlate.Trim().ToUpperInvariant();

            try
            {
                // Refresh parking statuses immediately before entry.
                // The selected space may have changed since the page
                // was opened.
                await _parkingStatusService.SyncAllSpacesAsync();

                using var db = App.ServiceProvider
                    .GetRequiredService<IDbContextFactory<AppDbContext>>()
                    .CreateDbContext();

                using var transaction =
                    await db.Database.BeginTransactionAsync();

                // -------------------------------------------------
                // Re-read parking space from database
                // -------------------------------------------------

                var space = await db.ParkingSpaces
                    .FirstOrDefaultAsync(
                        s => s.Id == SelectedSpace.Id);

                // Entry is allowed ONLY for a currently free space.
                if (space == null || space.Status != "Free")
                {
                    _mainViewModel.ShowToast(
                        "Dieser Parkplatz ist aktuell nicht verfügbar.");

                    await LoadFreeSpacesAsync();

                    return;
                }

                // -------------------------------------------------
                // FIND OR CREATE VEHICLE
                // -------------------------------------------------

                var vehicle = await db.Vehicles
                    .FirstOrDefaultAsync(
                        v => v.LicensePlate == normalizedPlate);

                if (vehicle == null)
                {
                    vehicle = new Vehicle
                    {
                        LicensePlate = normalizedPlate,

                        VehicleType =
                            string.IsNullOrWhiteSpace(VehicleType)
                                ? "Car"
                                : VehicleType.Trim()
                    };

                    db.Vehicles.Add(vehicle);

                    // Save once so the new vehicle receives its ID.
                    await db.SaveChangesAsync();
                }

                

                bool isAlreadyParked =
                    await db.ParkingSessions.AnyAsync(
                        s => s.VehicleId == vehicle.Id &&
                             s.Status == "Active");

                if (isAlreadyParked)
                {
                    _mainViewModel.ShowToast(
                        "Dieses Fahrzeug befindet sich bereits im Parkhaus.");

                    return;
                }

                

                var session = new ParkingSession
                {
                    VehicleId = vehicle.Id,
                    ParkingSpaceId = space.Id,
                    EntryTime = DateTime.Now,
                    Status = "Active"
                };

                db.ParkingSessions.Add(session);

                // The selected parking space becomes occupied.
                space.Status = "Occupied";

                // First save creates the ParkingSession ID.
                await db.SaveChangesAsync();

                

                db.AuditLogs.Add(new AuditLog
                {
                    UserId = CurrentUser.Id,
                    Action = "Entry",
                    EntityType = "ParkingSession",

                    // Store the actual generated ParkingSession ID.
                    EntityId = session.Id.ToString(),

                    Details =
                        $"Fahrzeug {normalizedPlate} hat das Parkhaus befahren. " +
                        $"Parkplatz: {space.SpaceNumber}",

                    Timestamp = DateTime.Now
                });

                await db.SaveChangesAsync();

                await transaction.CommitAsync();

                

                _mainViewModel.ShowToast(
                    $"Einfahrt erfolgreich registriert. Parkplatz: {space.SpaceNumber}");

                LicensePlate = string.Empty;
                VehicleType = string.Empty;
                SelectedSpace = null;

                await LoadFreeSpacesAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    "RegisterEntryAsync error: " + ex);

                _mainViewModel.ShowToast(
                    "Die Einfahrt konnte nicht gespeichert werden.");
            }
        }
    }
}