using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ParkingManagement.Data;
using ParkingManagement.Data.Models;
using ParkingManagement.UI.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace ParkingManagement.UI.ViewModels
{
    public class VehicleExitViewModel : ObservableObject
    {
        private readonly MainViewModel _mainViewModel;
        private readonly ParkingFeeCalculator _feeCalculator;

        public User CurrentUser { get; }

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
                    _ = LoadActiveSessionsAsync();
                }
            }
        }

        // =========================================================
        // ACTIVE PARKING SESSIONS
        // =========================================================

        private ObservableCollection<ParkingSession> _activeSessionsList = new();

        public ObservableCollection<ParkingSession> ActiveSessionsList
        {
            get => _activeSessionsList;
            set
            {
                SetProperty(ref _activeSessionsList, value);
                OnPropertyChanged(nameof(HasNoSessions));
            }
        }

        public bool HasNoSessions =>
            _activeSessionsList == null ||
            _activeSessionsList.Count == 0;

        // =========================================================
        // SELECTED SESSION
        // =========================================================

        private ParkingSession? _selectedSession;

        public ParkingSession? SelectedSession
        {
            get => _selectedSession;
            set
            {
                if (SetProperty(ref _selectedSession, value))
                {
                    if (_selectedSession != null)
                    {
                        _ = CalculateFeeAsync(_selectedSession);
                    }
                }
            }
        }

        // =========================================================
        // CALCULATED FEE
        // =========================================================

        private decimal _calculatedFee;

        public decimal CalculatedFee
        {
            get => _calculatedFee;
            set => SetProperty(ref _calculatedFee, value);
        }

        // =========================================================
        // PAYMENT METHOD
        // =========================================================

        private bool _isPaymentBar = true;

        public bool IsPaymentBar
        {
            get => _isPaymentBar;
            set => SetProperty(ref _isPaymentBar, value);
        }

        private bool _isPaymentCard;

        public bool IsPaymentCard
        {
            get => _isPaymentCard;
            set => SetProperty(ref _isPaymentCard, value);
        }

        public string PaymentMethod =>
            _isPaymentCard ? "Karte" : "Bar";

        // =========================================================
        // COMMANDS
        // =========================================================

        public ICommand GoBackCommand { get; }
        public ICommand RegisterExitCommand { get; }
        public ICommand SelectSessionCommand { get; }

        // =========================================================
        // CONSTRUCTOR
        // =========================================================

        public VehicleExitViewModel(
            MainViewModel mainViewModel,
            User user)
        {
            _mainViewModel = mainViewModel;
            CurrentUser = user;

            _feeCalculator = App.ServiceProvider
                .GetRequiredService<ParkingFeeCalculator>();

            GoBackCommand = new RelayCommand(_ =>
                _mainViewModel.NavigateTo(
                    new DashboardViewModel(
                        _mainViewModel,
                        CurrentUser)));

            RegisterExitCommand = new RelayCommand(
                async _ => await RegisterExitAsync(),
                _ => SelectedSession != null);

            SelectSessionCommand = new RelayCommand(param =>
            {
                if (param is ParkingSession session)
                {
                    SelectedSession = session;
                }
            });

            _ = LoadActiveSessionsAsync();
        }

        // =========================================================
        // LOAD ACTIVE PARKING SESSIONS
        // =========================================================

        private async Task LoadActiveSessionsAsync()
        {
            try
            {
                using var db = App.ServiceProvider
                    .GetRequiredService<IDbContextFactory<AppDbContext>>()
                    .CreateDbContext();

                var query = db.ParkingSessions
                    .Include(s => s.Vehicle)
                    .Include(s => s.ParkingSpace)
                    .Where(s => s.Status == "Active");

                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    string search = SearchText.Trim();

                    query = query.Where(s =>
                        s.Vehicle.LicensePlate.Contains(search) ||
                        s.ParkingSpace.SpaceNumber.Contains(search));
                }

                var list = await query
                    .OrderByDescending(s => s.EntryTime)
                    .ToListAsync();

                Application.Current.Dispatcher.Invoke(() =>
                {
                    ActiveSessionsList =
                        new ObservableCollection<ParkingSession>(list);
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    "LoadActiveSessionsAsync error: " + ex);
            }
        }

        

        private async Task CalculateFeeAsync(
            ParkingSession session)
        {
            try
            {
                var duration = DateTime.Now - session.EntryTime;

                var fee =
                    await _feeCalculator.CalculateFeeAsync(duration);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    CalculatedFee = fee;
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    "CalculateFeeAsync error: " + ex);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    CalculatedFee = 0m;
                });
            }
        }

        // =========================================================
        // REGISTER EXIT
        // =========================================================

        private async Task RegisterExitAsync()
        {
            if (SelectedSession == null)
            {
                return;
            }

            var sessionRef = SelectedSession;

            try
            {
                using var db = App.ServiceProvider
                    .GetRequiredService<IDbContextFactory<AppDbContext>>()
                    .CreateDbContext();

                using var transaction =
                    await db.Database.BeginTransactionAsync();

                // Reload the session from the database.
                // This prevents completing an already finished session.
                var session = await db.ParkingSessions
                    .Include(s => s.Vehicle)
                    .Include(s => s.ParkingSpace)
                    .FirstOrDefaultAsync(
                        s => s.Id == sessionRef.Id);

                if (session == null)
                {
                    _mainViewModel.ShowToast(
                        "Parkvorgang nicht gefunden.");

                    return;
                }

                // Important:
                // The session may have been completed somewhere else
                // after it was displayed in the UI.
                if (session.Status != "Active")
                {
                    _mainViewModel.ShowToast(
                        "Dieser Parkvorgang ist nicht mehr aktiv.");

                    await LoadActiveSessionsAsync();

                    return;
                }

                // Use the CURRENT time when confirming the exit.
                var exitTime = DateTime.Now;

                var duration =
                    exitTime - session.EntryTime;

            
                
                var finalFee =
                    await _feeCalculator.CalculateFeeAsync(duration);

                session.ExitTime = exitTime;
                session.ParkingFee = finalFee;
                session.Status = "Completed";

                // Audit log
                db.AuditLogs.Add(new AuditLog
                {
                    UserId = CurrentUser.Id,
                    Action = "Exit",
                    EntityType = "ParkingSession",
                    EntityId = session.Id.ToString(),

                    Details =
                        "Ausfahrt: " +
                        session.Vehicle.LicensePlate +
                        " von Platz " +
                        session.ParkingSpace.SpaceNumber +
                        ". Gebühr: " +
                        finalFee.ToString("N2") +
                        " € (" +
                        PaymentMethod +
                        ")",

                    Timestamp = DateTime.Now
                });

                await db.SaveChangesAsync();

                await transaction.CommitAsync();

                
               
                var statusService = App.ServiceProvider
                    .GetRequiredService<ParkingStatusService>();

                await statusService.SyncAllSpacesAsync();

                Application.Current.Dispatcher.Invoke(() =>
                {
                    CalculatedFee = finalFee;
                    SelectedSession = null;
                });

                _mainViewModel.ShowToast(
                    $"Ausfahrt erfolgreich. Gebühr: {finalFee:N2} €");

                // Refresh active sessions.
                await LoadActiveSessionsAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    "RegisterExitAsync error: " + ex);

                _mainViewModel.ShowToast(
                    "Die Ausfahrt konnte nicht gespeichert werden.");
            }
        }
    }
}