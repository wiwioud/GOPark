using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ParkingManagement.Data;
using ParkingManagement.Data.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using ParkingManagement.UI.Models;
using ParkingManagement.UI.Services;

namespace ParkingManagement.UI.ViewModels
{
    public class DashboardViewModel : ObservableObject
    {
        private readonly MainViewModel _mainViewModel;

        public User CurrentUser { get; }

        // =========================================================
        // KPI VALUES
        // =========================================================

        private int _totalSpaces;

        public int TotalSpaces
        {
            get => _totalSpaces;
            set => SetProperty(ref _totalSpaces, value);
        }

        private int _freeSpaces;

        public int FreeSpaces
        {
            get => _freeSpaces;
            set => SetProperty(ref _freeSpaces, value);
        }

        private int _occupiedSpaces;

        public int OccupiedSpaces
        {
            get => _occupiedSpaces;
            set => SetProperty(ref _occupiedSpaces, value);
        }

        private int _reservedSpaces;

        public int ReservedSpaces
        {
            get => _reservedSpaces;
            set => SetProperty(ref _reservedSpaces, value);
        }

        private int _activeSessions;

        public int ActiveSessions
        {
            get => _activeSessions;
            set => SetProperty(ref _activeSessions, value);
        }

        private decimal _todayRevenue;

        public decimal TodayRevenue
        {
            get => _todayRevenue;
            set => SetProperty(ref _todayRevenue, value);
        }

        // =========================================================
        // OCCUPANCY
        // =========================================================

        public string OccupancyText =>
            $"{_occupiedSpaces} / {_totalSpaces} Plätze belegt";

        public double OccupancyPercentage =>
            _totalSpaces == 0
                ? 0
                : ((double)_occupiedSpaces / _totalSpaces) * 100;

        public string OccupancyPercentageText =>
            $"{(int)OccupancyPercentage} %";

        // =========================================================
        // PARKING SPACE OVERVIEW
        // =========================================================

        private ObservableCollection<ParkingSpaceDisplayModel>
            _spacesOverview = new();

        public ObservableCollection<ParkingSpaceDisplayModel> SpacesOverview
        {
            get => _spacesOverview;
            set => SetProperty(ref _spacesOverview, value);
        }

        // =========================================================
        // RECENT ACTIVITIES
        // =========================================================

        private ObservableCollection<string> _recentActivities = new();

        public ObservableCollection<string> RecentActivities
        {
            get => _recentActivities;
            set
            {
                SetProperty(ref _recentActivities, value);
                OnPropertyChanged(nameof(HasNoActivities));
            }
        }

        public bool HasNoActivities =>
            _recentActivities == null ||
            _recentActivities.Count == 0;

        // =========================================================
        // COMMANDS
        // =========================================================

        public ICommand GoToSpacesCommand { get; }
        public ICommand GoToVehiclesCommand { get; }
        public ICommand GoToEntryCommand { get; }
        public ICommand GoToExitCommand { get; }
        public ICommand GoToReservationsCommand { get; }
        public ICommand CreateReservationCommand { get; }
        public ICommand GoToReportsCommand { get; }
        public ICommand RefreshCommand { get; }

        // =========================================================
        // CONSTRUCTOR
        // =========================================================

        public DashboardViewModel(
            MainViewModel mainViewModel,
            User user)
        {
            _mainViewModel = mainViewModel;
            CurrentUser = user;

            GoToSpacesCommand = new RelayCommand(_ =>
                _mainViewModel.NavigateTo(
                    new ParkingSpacesViewModel(
                        _mainViewModel,
                        CurrentUser)));

            GoToVehiclesCommand = new RelayCommand(_ =>
                _mainViewModel.NavigateTo(
                    new VehiclesViewModel(
                        _mainViewModel,
                        CurrentUser)));

            GoToEntryCommand = new RelayCommand(_ =>
                _mainViewModel.NavigateTo(
                    new VehicleEntryViewModel(
                        _mainViewModel,
                        CurrentUser)));

            GoToExitCommand = new RelayCommand(_ =>
                _mainViewModel.NavigateTo(
                    new VehicleExitViewModel(
                        _mainViewModel,
                        CurrentUser)));

            GoToReservationsCommand = new RelayCommand(_ =>
                _mainViewModel.NavigateTo(
                    new ReservationsViewModel(
                        _mainViewModel,
                        CurrentUser)));

            GoToReportsCommand = new RelayCommand(_ =>
                _mainViewModel.NavigateTo(
                    new ReportsViewModel(
                        _mainViewModel,
                        CurrentUser)));

            CreateReservationCommand =
                new RelayCommand(_ => CreateReservation());

            RefreshCommand =
                new RelayCommand(
                    async _ => await LoadDataAsync());

            _ = LoadDataAsync();
        }

        // =========================================================
        // CREATE RESERVATION
        // =========================================================

        private void CreateReservation()
        {
            var window =
                new Views.ReservationEditWindow();

            var vm =
                new ReservationEditViewModel(
                    window,
                    _mainViewModel);

            window.DataContext = vm;
            window.Owner = App.Current.MainWindow;

            window.ShowDialog();

            if (vm.WasSaved)
            {
                _ = LoadDataAsync();
            }
        }

        // =========================================================
        // LOAD DASHBOARD DATA
        // =========================================================

        public async Task LoadDataAsync()
        {
            try
            {
                // -------------------------------------------------
                // SYNCHRONIZE PARKING SPACE STATUSES
                // -------------------------------------------------

                var statusService =
                    App.ServiceProvider
                        .GetRequiredService<ParkingStatusService>();

                await statusService.SyncAllSpacesAsync();

                using var dbContext =
                    App.ServiceProvider
                        .GetRequiredService<IDbContextFactory<AppDbContext>>()
                        .CreateDbContext();

                // -------------------------------------------------
                // KPI: PARKING SPACES
                // -------------------------------------------------

                TotalSpaces =
                    await dbContext.ParkingSpaces.CountAsync(
                        s =>
                            s.Status != "Out of Order" &&
                            s.Status != "Außer Betrieb");

                FreeSpaces =
                    await dbContext.ParkingSpaces.CountAsync(
                        s => s.Status == "Free");

                OccupiedSpaces =
                    await dbContext.ParkingSpaces.CountAsync(
                        s => s.Status == "Occupied");

                ActiveSessions =
                    await dbContext.ParkingSessions.CountAsync(
                        s => s.Status == "Active");

                // -------------------------------------------------
                // KPI: PLANNED RESERVATIONS
                // -------------------------------------------------
                //
                // Dashboard card:
                // "Geplante Reservierungen"
                //
                // Therefore only reservations with the database
                // status "Planned" are counted.
                //
                // Expired and Cancelled reservations must NOT
                // influence this value.
                // -------------------------------------------------

                ReservedSpaces =
                    await dbContext.Reservations.CountAsync(
                        r => r.Status == "Planned");

                // -------------------------------------------------
                // TODAY'S REVENUE
                // -------------------------------------------------

                var today = DateTime.Today;
                var tomorrow = today.AddDays(1);

                var revenue =
                    await dbContext.ParkingSessions
                        .Where(s =>
                            s.Status == "Completed" &&
                            s.ExitTime != null &&
                            s.ExitTime >= today &&
                            s.ExitTime < tomorrow)
                        .SumAsync(
                            s => (decimal?)(s.ParkingFee ?? 0))
                        ?? 0m;

                TodayRevenue = revenue;

                // -------------------------------------------------
                // UPDATE OCCUPANCY VALUES
                // -------------------------------------------------

                OnPropertyChanged(nameof(OccupancyText));
                OnPropertyChanged(nameof(OccupancyPercentage));
                OnPropertyChanged(nameof(OccupancyPercentageText));

                // -------------------------------------------------
                // BUILD PARKING SPACE OVERVIEW
                // -------------------------------------------------

                var allSpaces =
                    await dbContext.ParkingSpaces
                        .OrderBy(s => s.SpaceNumber)
                        .ToListAsync();

                var activeSessionsList =
                    await dbContext.ParkingSessions
                        .Include(s => s.Vehicle)
                        .Where(s => s.Status == "Active")
                        .ToListAsync();

                // Only reservations that can still be relevant
                // for the current parking situation are loaded.
                //
                // Expired and Cancelled reservations are excluded.
                var allReservations =
                    await dbContext.Reservations
                        .Include(r => r.Vehicle)
                        .Where(r =>
                            r.Status == "Planned" ||
                            r.Status == "Active")
                        .ToListAsync();

                var displayModels =
                    new List<ParkingSpaceDisplayModel>();

                var now = DateTime.Now;

                foreach (var space in allSpaces)
                {
                    var model =
                        new ParkingSpaceDisplayModel
                        {
                            Space = space
                        };

                    // ---------------------------------------------
                    // ACTIVE VEHICLE
                    // ---------------------------------------------

                    var activeSession =
                        activeSessionsList.FirstOrDefault(
                            s => s.ParkingSpaceId == space.Id);

                    if (activeSession?.Vehicle != null)
                    {
                        model.OccupantText =
                            activeSession.Vehicle.LicensePlate;
                    }

                    // ---------------------------------------------
                    // RESERVATION INFORMATION
                    // ---------------------------------------------

                    if (space.Status != "Occupied")
                    {
                        // Future planned reservation
                        var futureReservation =
                            allReservations
                                .Where(r =>
                                    r.Status == "Planned" &&
                                    r.ParkingSpaceId == space.Id)
                                .Where(r =>
                                    r.ReservationDate.Date > now.Date ||
                                    (
                                        r.ReservationDate.Date == now.Date &&
                                        r.StartTime > now.TimeOfDay
                                    ))
                                .OrderBy(r => r.ReservationDate)
                                .ThenBy(r => r.StartTime)
                                .FirstOrDefault();

                        if (futureReservation != null)
                        {
                            if (futureReservation.ReservationDate.Date ==
                                now.Date)
                            {
                                model.FutureReservationText =
                                    "Reserviert ab " +
                                    futureReservation.StartTime
                                        .ToString(@"hh\:mm");
                            }
                            else
                            {
                                model.FutureReservationText =
                                    "Reserviert ab " +
                                    futureReservation.ReservationDate
                                        .ToString("dd.MM.") +
                                    " " +
                                    futureReservation.StartTime
                                        .ToString(@"hh\:mm");
                            }
                        }
                        else if (space.Status == "Reserved")
                        {
                            // Current active reservation
                            var activeReservation =
                                allReservations.FirstOrDefault(r =>
                                    r.Status == "Active" &&
                                    r.ParkingSpaceId == space.Id &&
                                    r.ReservationDate.Date == now.Date &&
                                    r.StartTime <= now.TimeOfDay &&
                                    r.EndTime >= now.TimeOfDay);

                            if (activeReservation != null)
                            {
                                model.FutureReservationText =
                                    "bis " +
                                    activeReservation.EndTime
                                        .ToString(@"hh\:mm");
                            }
                        }
                    }

                    displayModels.Add(model);
                }

                SpacesOverview =
                    new ObservableCollection<ParkingSpaceDisplayModel>(
                        displayModels);

                // -------------------------------------------------
                // RECENT ACTIVITIES
                // -------------------------------------------------

                var logs =
                    await dbContext.AuditLogs
                        .OrderByDescending(a => a.Timestamp)
                        .Take(10)
                        .ToListAsync();

                var logStrings =
                    logs.Select(l =>
                    {
                        var actionDe =
                            l.Action switch
                            {
                                "Entry" => "Einfahrt",
                                "Exit" => "Ausfahrt",
                                "Create" => "Erstellt",
                                "Update" => "Geändert",
                                "Delete" => "Gelöscht",
                                "Login" => "Anmeldung",
                                _ => l.Action
                            };

                        return
                            $"{l.Timestamp:HH:mm}  " +
                            $"{actionDe,-12} " +
                            $"{l.Details}";
                    })
                    .ToList();

                RecentActivities =
                    new ObservableCollection<string>(
                        logStrings);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    "Dashboard error: " + ex);
            }
        }
    }
}