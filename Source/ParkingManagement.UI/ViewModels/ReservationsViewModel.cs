using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ParkingManagement.Data;
using ParkingManagement.Data.Models;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ParkingManagement.UI.ViewModels
{
    public class ReservationsViewModel : ObservableObject
    {
        private readonly MainViewModel _mainViewModel;

        public User CurrentUser { get; }

        // =========================================================
        // RESERVATIONS
        // =========================================================

        private ObservableCollection<Reservation> _reservations = new();

        public ObservableCollection<Reservation> Reservations
        {
            get => _reservations;
            set => SetProperty(ref _reservations, value);
        }

        // =========================================================
        // SEARCH
        // =========================================================

        private string _searchText = string.Empty;

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    _ = LoadDataAsync();
                }
            }
        }

        // =========================================================
        // STATUS FILTER
        // =========================================================

        private string _statusFilter = "Alle";

        public string StatusFilter
        {
            get => _statusFilter;
            set
            {
                if (SetProperty(ref _statusFilter, value))
                {
                    _ = LoadDataAsync();
                }
            }
        }

        // =========================================================
        // COMMANDS
        // =========================================================

        public ICommand GoBackCommand { get; }
        public ICommand CancelReservationCommand { get; }
        public ICommand AddReservationCommand { get; }
        public ICommand FilterCommand { get; }

        // =========================================================
        // CONSTRUCTOR
        // =========================================================

        public ReservationsViewModel(
            MainViewModel mainViewModel,
            User user)
        {
            _mainViewModel = mainViewModel;
            CurrentUser = user;

            GoBackCommand = new RelayCommand(_ =>
                _mainViewModel.NavigateTo(
                    new DashboardViewModel(
                        _mainViewModel,
                        CurrentUser)));

            CancelReservationCommand =
                new RelayCommand(
                    async param =>
                        await CancelReservationAsync(
                            param as Reservation));

            AddReservationCommand =
                new RelayCommand(_ => AddReservation());

            FilterCommand =
                new RelayCommand(param =>
                {
                    if (param is string status)
                    {
                        StatusFilter = status;
                    }
                });

            _ = LoadDataAsync();
        }

        // =========================================================
        // LOAD RESERVATIONS
        // =========================================================

        private async Task LoadDataAsync()
        {
            using var db = App.ServiceProvider
                .GetRequiredService<IDbContextFactory<AppDbContext>>()
                .CreateDbContext();

            var query = db.Reservations
                .Include(r => r.Vehicle)
                .Include(r => r.ParkingSpace)
                .AsQueryable();

            // -----------------------------------------------------
            // STATUS FILTER
            // -----------------------------------------------------

            if (StatusFilter != "Alle")
            {
                string dbStatus = StatusFilter switch
                {
                    "Geplant" => "Planned",
                    "Aktiv" => "Active",
                    "Abgelaufen" => "Expired",
                    "Storniert" => "Cancelled",
                    _ => "Alle"
                };

                if (dbStatus != "Alle")
                {
                    query = query.Where(
                        r => r.Status == dbStatus);
                }
            }

            // -----------------------------------------------------
            // SEARCH
            // -----------------------------------------------------

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                string searchText = SearchText.Trim();

                query = query.Where(r =>
                    r.Vehicle.LicensePlate.Contains(searchText) ||
                    r.ParkingSpace.SpaceNumber.Contains(searchText) ||
                    r.Id.ToString().Contains(searchText));
            }

            // -----------------------------------------------------
            // RESULT
            // -----------------------------------------------------

            var reservationList = await query
                .OrderByDescending(r => r.ReservationDate)
                .ThenByDescending(r => r.StartTime)
                .ToListAsync();

            Reservations =
                new ObservableCollection<Reservation>(
                    reservationList);
        }

        // =========================================================
        // ADD RESERVATION
        // =========================================================

        private void AddReservation()
        {
            var window =
                new Views.ReservationEditWindow();

            var viewModel =
                new ReservationEditViewModel(
                    window,
                    _mainViewModel);

            window.DataContext = viewModel;
            window.Owner = App.Current.MainWindow;

            window.ShowDialog();

            if (viewModel.WasSaved)
            {
                _ = LoadDataAsync();
            }
        }

        // =========================================================
        // CANCEL RESERVATION
        // =========================================================

        private async Task CancelReservationAsync(
            Reservation? reservation)
        {
            if (reservation == null ||
                reservation.Status == "Cancelled")
            {
                return;
            }

            using var db = App.ServiceProvider
                .GetRequiredService<IDbContextFactory<AppDbContext>>()
                .CreateDbContext();

            var reservationFromDb =
                await db.Reservations.FindAsync(
                    reservation.Id);

            if (reservationFromDb != null)
            {
                reservationFromDb.Status =
                    "Cancelled";

                var space =
                    await db.ParkingSpaces.FindAsync(
                        reservationFromDb.ParkingSpaceId);

                if (space != null &&
                    space.Status == "Reserved")
                {
                    space.Status = "Free";
                }

                await db.SaveChangesAsync();

                await LoadDataAsync();
            }
        }
    }
}